#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PackageAccessAnalyzer : DiagnosticAnalyzer
    {
        public const string FieldAccessDiagnosticId = "ET0120";
        public const string UnauthorizedAccessDiagnosticId = "ET0121";
        
        private static readonly LocalizableString FieldAccessTitle = "Package字段访问违规";
        private static readonly LocalizableString FieldAccessMessageFormat = "包 '{0}' 不能访问包 '{1}' 的字段 '{2}'";
        private static readonly LocalizableString FieldAccessDescription = "包与包之间不允许访问字段，字段只能被包内的方法访问.";
        
        private static readonly LocalizableString UnauthorizedAccessTitle = "Package未授权访问";
        private static readonly LocalizableString UnauthorizedAccessMessageFormat = "包 '{0}' 未在dependencies中声明对包 '{1}' 的依赖，不能访问其类型或方法";
        private static readonly LocalizableString UnauthorizedAccessDescription = "包与包之间的访问需要在package.json的dependencies中明确声明.";
        
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor FieldAccessRule = new DiagnosticDescriptor(
            FieldAccessDiagnosticId,
            FieldAccessTitle,
            FieldAccessMessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: FieldAccessDescription);
            
        private static readonly DiagnosticDescriptor UnauthorizedAccessRule = new DiagnosticDescriptor(
            UnauthorizedAccessDiagnosticId,
            UnauthorizedAccessTitle,
            UnauthorizedAccessMessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: UnauthorizedAccessDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(FieldAccessRule, UnauthorizedAccessRule);

        // 缓存package信息
        private static readonly Dictionary<string, Dictionary<string, string>> _packageDependenciesCache = new Dictionary<string, Dictionary<string, string>>();

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction((analysisContext =>
            {
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                {
                    analysisContext.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
                    analysisContext.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
                    analysisContext.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.ObjectCreationExpression);
                    analysisContext.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.TypeOfExpression);
                    analysisContext.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
                    analysisContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
                    
                    // 移除强制检测
                    // analysisContext.RegisterSyntaxNodeAction(ForceAnalyzeTargetComponent, SyntaxKind.IdentifierName);
                }
            }));
        }

        private static void ForceAnalyzeTargetComponent(SyntaxNodeAnalysisContext context)
        {
            var identifier = (IdentifierNameSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            
            if (identifier.Identifier.ValueText == "TargetComponent" && 
                !string.IsNullOrEmpty(filePath) && IsInPackageDirectory(filePath))
            {
                var sourcePackage = GetPackageNameFromPath(filePath);
                if (sourcePackage == "cn.etetet.map")
                {
                    // 强制报告：map包中的TargetComponent使用
                    var diagnostic = Diagnostic.Create(UnauthorizedAccessRule, identifier.GetLocation(),
                        "cn.etetet.map", "cn.etetet.spell");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeAllTypeReferences(SyntaxNodeAnalysisContext context)
        {
            var compilationUnit = (CompilationUnitSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            
            // 如果是M2M_UnitTransferRequestHandler文件，强制报错以测试分析器是否工作
            if (filePath.Contains("M2M_UnitTransferRequestHandler"))
            {
                var diagnostic = Diagnostic.Create(UnauthorizedAccessRule, compilationUnit.GetLocation(),
                    "ANALYZER_TEST", "M2M_UnitTransferRequestHandler file detected");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            
            if (string.IsNullOrEmpty(filePath) || !IsInPackageDirectory(filePath))
                return;
                
            var sourcePackage = GetPackageNameFromPath(filePath);
            if (string.IsNullOrEmpty(sourcePackage))
                return;
                
            // 检查泛型方法调用中的类型参数
            if (memberAccess.Name is GenericNameSyntax genericName)
            {
                foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
                {
                    AnalyzeTypeArgument(context, typeArgument, sourcePackage);
                }
            }
                
            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol == null)
                return;
                
            var targetSymbol = symbolInfo.Symbol;
            var targetType = GetContainingType(targetSymbol);
            if (targetType == null)
                return;
                
            var targetPackage = GetPackageNameFromType(targetType);
            if (string.IsNullOrEmpty(targetPackage))
                return; // 无法确定包名，允许访问
                
            // 特殊处理：对于partial class，检查成员是否在当前包中定义
            if (IsPartialClassMember(targetType, targetSymbol, sourcePackage))
                return; // 成员在当前包中定义，允许访问
                
            // 同一个包内的访问允许
            if (sourcePackage == targetPackage)
                return;
                
            // 检查字段访问：包与包之间不允许访问字段
            if (targetSymbol is IFieldSymbol fieldSymbol)
            {
                // 常量字段不受访问限制，任何包都可以访问
                if (fieldSymbol.IsConst)
                {
                    return;
                }
                
                // 检查目标包是否允许其他包访问字段
                if (!CheckOtherPackageAccessField(targetPackage))
                {
                    var diagnostic = Diagnostic.Create(FieldAccessRule, memberAccess.GetLocation(),
                        sourcePackage, targetPackage, targetSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                return;
            }
            
            // 检查包间依赖：必须在dependencies中声明才能访问类型或方法
            if (!CheckPackageJsonDependencies(sourcePackage, targetPackage))
            {
                var diagnostic = Diagnostic.Create(UnauthorizedAccessRule, memberAccess.GetLocation(),
                    sourcePackage, targetPackage);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifierName = (IdentifierNameSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            
            if (string.IsNullOrEmpty(filePath) || !IsInPackageDirectory(filePath))
                return;
                
            var sourcePackage = GetPackageNameFromPath(filePath);
            if (string.IsNullOrEmpty(sourcePackage))
                return;
                
            var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName);
            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                var targetPackage = GetPackageNameFromType(typeSymbol);
                if (string.IsNullOrEmpty(targetPackage) || targetPackage == sourcePackage)
                    return; // partial class、同包或无法确定包名，允许访问
                    
                // 检查包间依赖：必须在dependencies中声明才能访问类型
                if (!CheckPackageJsonDependencies(sourcePackage, targetPackage))
                {
                    var diagnostic = Diagnostic.Create(UnauthorizedAccessRule, identifierName.GetLocation(),
                        sourcePackage, targetPackage);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeTypeUsage(SyntaxNodeAnalysisContext context)
        {
            var filePath = context.Node.SyntaxTree.FilePath;
            
            if (string.IsNullOrEmpty(filePath) || !IsInPackageDirectory(filePath))
                return;
                
            var sourcePackage = GetPackageNameFromPath(filePath);
            if (string.IsNullOrEmpty(sourcePackage))
                return;
                
            INamedTypeSymbol typeSymbol = null;
            
            if (context.Node is ObjectCreationExpressionSyntax objectCreation)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation.Type);
                typeSymbol = symbolInfo.Symbol as INamedTypeSymbol;
            }
            else if (context.Node is TypeOfExpressionSyntax typeOf)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(typeOf.Type);
                typeSymbol = symbolInfo.Symbol as INamedTypeSymbol;
            }
            
            if (typeSymbol == null)
                return;
                
            var targetPackage = GetPackageNameFromType(typeSymbol);
            if (string.IsNullOrEmpty(targetPackage) || targetPackage == sourcePackage)
                return; // partial class、同包或无法确定包名，允许访问
                
            // 检查包间依赖：必须在dependencies中声明才能访问类型
            if (!CheckPackageJsonDependencies(sourcePackage, targetPackage))
            {
                var diagnostic = Diagnostic.Create(UnauthorizedAccessRule, context.Node.GetLocation(),
                    sourcePackage, targetPackage);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
        {
            var genericName = (GenericNameSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            
            if (string.IsNullOrEmpty(filePath) || !IsInPackageDirectory(filePath))
                return;
                
            var sourcePackage = GetPackageNameFromPath(filePath);
            if (string.IsNullOrEmpty(sourcePackage))
                return;
                
            // 检查泛型类型参数
            foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
            {
                AnalyzeTypeArgument(context, typeArgument, sourcePackage);
            }
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            
            if (string.IsNullOrEmpty(filePath) || !IsInPackageDirectory(filePath))
                return;
                
            var sourcePackage = GetPackageNameFromPath(filePath);
            if (string.IsNullOrEmpty(sourcePackage))
                return;
                
            // 检查泛型方法调用中的类型参数
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName)
            {
                foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
                {
                    AnalyzeTypeArgument(context, typeArgument, sourcePackage);
                }
            }
            
            // 也检查直接的泛型调用 (如果有的话)
            if (invocation.Expression is GenericNameSyntax directGeneric)
            {
                foreach (var typeArgument in directGeneric.TypeArgumentList.Arguments)
                {
                    AnalyzeTypeArgument(context, typeArgument, sourcePackage);
                }
            }
        }
        
        private static void AnalyzeTypeArgument(SyntaxNodeAnalysisContext context, TypeSyntax typeArgument, string sourcePackage)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(typeArgument);
            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                var targetPackage = GetPackageNameFromType(typeSymbol);
                
                // 对于跨程序集的类型，尝试从命名空间推断包名
                if (string.IsNullOrEmpty(targetPackage))
                {
                    targetPackage = InferPackageFromNamespace(typeSymbol);
                }
                
                if (string.IsNullOrEmpty(targetPackage) || targetPackage == sourcePackage)
                    return; // partial class、同包或无法确定包名，允许访问
                    
                // 检查包间依赖：必须在dependencies中声明才能访问类型
                if (!CheckPackageJsonDependencies(sourcePackage, targetPackage))
                {
                    var diagnostic = Diagnostic.Create(UnauthorizedAccessRule, typeArgument.GetLocation(),
                        sourcePackage, targetPackage);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }




        private static string GetPackageNameFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;
                
            var normalizedPath = filePath.Replace('\\', '/');
            var packageNameMatch = System.Text.RegularExpressions.Regex.Match(normalizedPath, @"/Packages/(cn\.etetet\.[^/]+)/");
            
            if (!packageNameMatch.Success)
                return null;
                
            return packageNameMatch.Groups[1].Value;
        }

        private static string GetPackageNameFromType(INamedTypeSymbol typeSymbol)
        {
            // 通过类型的源文件路径获取包名
            var locations = typeSymbol.Locations;
            
            // 检查是否是partial class（在多个源文件中有定义）
            var sourceLocations = locations.Where(loc => loc.IsInSource).ToArray();
            var allPackages = new List<string>();
            
            foreach (var location in sourceLocations)
            {
                var packageName = GetPackageNameFromPath(location.SourceTree.FilePath);
                if (!string.IsNullOrEmpty(packageName))
                {
                    allPackages.Add(packageName);
                }
            }
            
            // 如果没有从源文件找到包信息，尝试从命名空间或其他方式推断
            if (allPackages.Count == 0)
            {
                // 对于跨程序集引用的类型，尝试从类型信息推断包名
                // 这是ET框架特有的逻辑，基于已知的类型-包映射
                return GetPackageFromKnownTypes(typeSymbol);
            }
            
            // 如果类型在多个包中都有定义，说明这是partial class，返回null
            var uniquePackages = allPackages.Distinct().ToArray();
            if (uniquePackages.Length > 1)
            {
                return null; // partial class，无法确定单一包名
            }
            
            // 如果只在一个包中定义，返回该包名
            return uniquePackages[0];
        }
        
        private static string GetPackageFromKnownTypes(INamedTypeSymbol typeSymbol)
        {
            // 尝试从编译时可用的信息推断包名
            
            // 方法1：检查当前编译上下文中是否有相同类型的源码定义
            var compilation = typeSymbol.ContainingAssembly?.GlobalNamespace?.ContainingCompilation;
            if (compilation != null)
            {
                // 在当前编译上下文中查找相同全名的类型
                var fullName = GetFullTypeName(typeSymbol);
                var sourceType = FindTypeInCompilationByFullName(compilation, fullName);
                if (sourceType != null && !SymbolEqualityComparer.Default.Equals(sourceType, typeSymbol))
                {
                    // 找到了源码中的相同类型，使用其包信息
                    foreach (var location in sourceType.Locations)
                    {
                        if (location.IsInSource)
                        {
                            var packageName = GetPackageNameFromPath(location.SourceTree.FilePath);
                            if (!string.IsNullOrEmpty(packageName))
                            {
                                return packageName;
                            }
                        }
                    }
                }
            }
            
            // 方法2：尝试从类型的程序集引用信息推断包名
            return InferPackageFromAssemblyContext(typeSymbol);
        }
        
        private static string GetFullTypeName(INamedTypeSymbol typeSymbol)
        {
            var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
            if (string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>")
            {
                return typeSymbol.Name;
            }
            return $"{namespaceName}.{typeSymbol.Name}";
        }
        
        private static INamedTypeSymbol FindTypeInCompilationByFullName(Compilation compilation, string fullName)
        {
            var parts = fullName.Split('.');
            var typeName = parts.Last();
            var namespaceParts = parts.Take(parts.Length - 1).ToArray();
            
            INamespaceSymbol currentNamespace = compilation.GlobalNamespace;
            
            // 导航到目标命名空间
            foreach (var part in namespaceParts)
            {
                var found = false;
                foreach (var member in currentNamespace.GetNamespaceMembers())
                {
                    if (member.Name == part)
                    {
                        currentNamespace = member;
                        found = true;
                        break;
                    }
                }
                if (!found) return null;
            }
            
            // 在目标命名空间中查找类型
            foreach (var member in currentNamespace.GetTypeMembers())
            {
                if (member.Name == typeName)
                {
                    return member;
                }
            }
            
            return null;
        }
        
        private static string InferPackageFromAssemblyContext(INamedTypeSymbol typeSymbol)
        {
            // 尝试通过程序集上下文和编译信息推断包名
            
            // 方法1：从包含命名空间的所有已知类型中查找
            var compilation = typeSymbol.ContainingAssembly?.GlobalNamespace?.ContainingCompilation;
            if (compilation != null)
            {
                // 在当前编译的所有源文件中查找相同命名空间下的其他类型
                var targetNamespace = typeSymbol.ContainingNamespace;
                if (targetNamespace != null)
                {
                    var sameNamespaceTypes = GetTypesInNamespace(compilation, targetNamespace);
                    foreach (var type in sameNamespaceTypes)
                    {
                        foreach (var location in type.Locations)
                        {
                            if (location.IsInSource)
                            {
                                var packageName = GetPackageNameFromPath(location.SourceTree.FilePath);
                                if (!string.IsNullOrEmpty(packageName))
                                {
                                    return packageName; // 使用同命名空间中已知类型的包名
                                }
                            }
                        }
                    }
                }
            }
            
            // 对于引用程序集中的类型，我们暂时无法确定其包归属
            // 这是分析器设计的一个限制：跨程序集的包依赖检查需要额外的元数据支持
            return null;
        }
        
        private static IEnumerable<INamedTypeSymbol> GetTypesInNamespace(Compilation compilation, INamespaceSymbol namespaceSymbol)
        {
            var types = new List<INamedTypeSymbol>();
            
            // 递归收集命名空间中的所有类型
            CollectTypesFromNamespace(namespaceSymbol, types);
            
            return types;
        }
        
        private static void CollectTypesFromNamespace(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> types)
        {
            // 添加当前命名空间中的类型
            types.AddRange(namespaceSymbol.GetTypeMembers());
            
            // 递归处理子命名空间
            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                CollectTypesFromNamespace(childNamespace, types);
            }
        }

        private static string InferPackageFromNamespace(INamedTypeSymbol typeSymbol)
        {
            // 尝试从程序集中查找同名类型的包信息
            // 这是一个通用方法，不依赖硬编码的类型映射
            
            if (_projectRootPath == null)
            {
                _projectRootPath = FindProjectRoot();
            }
            
            if (_projectRootPath != null)
            {
                // 搜索所有包目录中的类型定义
                var packagesPath = Path.Combine(_projectRootPath, "Packages");
                if (Directory.Exists(packagesPath))
                {
                    var packageDirs = Directory.GetDirectories(packagesPath, "cn.etetet.*");
                    foreach (var packageDir in packageDirs)
                    {
                        var packageName = Path.GetFileName(packageDir);
                        var scriptsPath = Path.Combine(packageDir, "Scripts");
                        
                        if (Directory.Exists(scriptsPath))
                        {
                            // 在包中查找同名的.cs文件
                            var files = Directory.GetFiles(scriptsPath, $"{typeSymbol.Name}.cs", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                return packageName;
                            }
                        }
                    }
                }
            }
            
            return null; // 无法找到对应的包
        }



        private static bool IsInPackageDirectory(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
                
            var normalizedPath = filePath.Replace('\\', '/');
            return normalizedPath.Contains("/Packages/cn.etetet.");
        }

        private static INamedTypeSymbol GetContainingType(ISymbol symbol)
        {
            while (symbol != null)
            {
                if (symbol is INamedTypeSymbol typeSymbol)
                    return typeSymbol;
                symbol = symbol.ContainingSymbol;
            }
            return null;
        }







        private static string _projectRootPath = null;
        
        private static bool CheckPackageJsonDependencies(string sourcePackage, string targetPackage)
        {
            if (string.IsNullOrEmpty(sourcePackage) || string.IsNullOrEmpty(targetPackage))
                return false;
            
            try
            {
                // 如果还没有找到项目根路径，尝试确定
                if (_projectRootPath == null)
                {
                    _projectRootPath = FindProjectRoot();
                }
                
                if (_projectRootPath != null)
                {
                    var packageJsonPath = Path.Combine(_projectRootPath, "Packages", sourcePackage, "package.json");
                    
                    if (File.Exists(packageJsonPath))
                    {
                        var json = File.ReadAllText(packageJsonPath);
                        
                        // 简单的字符串检查
                        var dependenciesSection = json.IndexOf("\"dependencies\":");
                        if (dependenciesSection != -1)
                        {
                            var dependenciesText = json.Substring(dependenciesSection);
                            var nextSection = dependenciesText.IndexOf("},");
                            if (nextSection == -1)
                                nextSection = dependenciesText.IndexOf("}");
                                
                            if (nextSection != -1)
                            {
                                var actualDependencies = dependenciesText.Substring(0, nextSection);
                                return actualDependencies.Contains($"\"{targetPackage}\"");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略读取错误
            }
            
            return false;
        }
        
        private static string FindProjectRoot()
        {
            try
            {
                // 尝试多个可能的根路径位置
                var possiblePaths = new[]
                {
                    Directory.GetCurrentDirectory(),
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "/Users/tanghai/Documents/WOW"  // 作为后备方案
                };
                
                foreach (var path in possiblePaths)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        var packagesDir = Path.Combine(path, "Packages");
                        if (Directory.Exists(packagesDir))
                        {
                            return path;
                        }
                        
                        // 向上搜索，最多5级
                        var currentPath = path;
                        for (int i = 0; i < 5; i++)
                        {
                            var parentPath = Directory.GetParent(currentPath)?.FullName;
                            if (parentPath == null) break;
                            
                            var parentPackagesDir = Path.Combine(parentPath, "Packages");
                            if (Directory.Exists(parentPackagesDir))
                            {
                                return parentPath;
                            }
                            
                            currentPath = parentPath;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 搜索失败
            }
            
            return null;
        }
        
        private static bool IsPartialClassMember(INamedTypeSymbol typeSymbol, ISymbol memberSymbol, string sourcePackage)
        {
            // 首先检查成员是否在当前包中定义
            var memberLocations = memberSymbol.Locations;
            foreach (var location in memberLocations)
            {
                if (location.IsInSource)
                {
                    var memberPackage = GetPackageNameFromPath(location.SourceTree.FilePath);
                    if (memberPackage == sourcePackage)
                    {
                        return true; // 成员在当前包中定义
                    }
                }
            }
            
            // 对于常量字段，进一步检查是否是partial class的成员
            if (memberSymbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
            {
                // 检查类型是否是partial class（通过检查是否有多个源文件位置）
                var typeLocations = typeSymbol.Locations.Where(loc => loc.IsInSource).ToArray();
                if (typeLocations.Length > 1)
                {
                    // 这是一个partial class，直接调用常量字段归属检查
                    return IsConstantFieldBelongsToPackage(fieldSymbol, sourcePackage);
                }
            }
            
            return false;
        }
        
        private static bool CheckOtherPackageAccessField(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return false;
                
            try
            {
                // 如果还没有找到项目根路径，尝试确定
                if (_projectRootPath == null)
                {
                    _projectRootPath = FindProjectRoot();
                }
                
                if (_projectRootPath != null)
                {
                    var packageGitJsonPath = Path.Combine(_projectRootPath, "Packages", packageName, "packagegit.json");
                    
                    if (File.Exists(packageGitJsonPath))
                    {
                        var json = File.ReadAllText(packageGitJsonPath);
                        
                        // 检查AllowAccessField字段
                        if (json.Contains("\"AllowAccessField\""))
                        {
                            // 简单的字符串匹配检查
                            var allowAccessFieldIndex = json.IndexOf("\"AllowAccessField\":");
                            if (allowAccessFieldIndex != -1)
                            {
                                var remainingJson = json.Substring(allowAccessFieldIndex);
                                var nextCommaOrBrace = Math.Min(
                                    remainingJson.IndexOf(',') == -1 ? int.MaxValue : remainingJson.IndexOf(','),
                                    remainingJson.IndexOf('}') == -1 ? int.MaxValue : remainingJson.IndexOf('}')
                                );
                                
                                if (nextCommaOrBrace != int.MaxValue)
                                {
                                    var fieldValue = remainingJson.Substring(0, nextCommaOrBrace);
                                    return fieldValue.Contains("true");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略读取错误
            }
            
            return false; // 默认不允许其他包访问字段
        }


        private static Dictionary<string, string> ParsePackageJsonDependencies(string json)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                var dependenciesStart = json.IndexOf("\"dependencies\":");
                if (dependenciesStart == -1) return result;
                
                var openBrace = json.IndexOf('{', dependenciesStart);
                if (openBrace == -1) return result;
                
                var closeBrace = FindMatchingBrace(json, openBrace);
                if (closeBrace == -1) return result;
                
                var dependenciesContent = json.Substring(openBrace + 1, closeBrace - openBrace - 1);
                var lines = dependenciesContent.Split('\n');
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("\"") && trimmed.Contains(":"))
                    {
                        var colonIndex = trimmed.IndexOf(':');
                        var packageName = trimmed.Substring(0, colonIndex).Trim().Trim('"');
                        var version = trimmed.Substring(colonIndex + 1).Trim().TrimEnd(',').Trim('"');
                        
                        if (!string.IsNullOrEmpty(packageName) && !string.IsNullOrEmpty(version))
                        {
                            result[packageName] = version;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 解析失败，返回空字典
            }
            
            return result;
        }

        private static int FindMatchingBrace(string text, int openBraceIndex)
        {
            int braceCount = 1;
            for (int i = openBraceIndex + 1; i < text.Length; i++)
            {
                if (text[i] == '{') braceCount++;
                else if (text[i] == '}') braceCount--;
                
                if (braceCount == 0) return i;
            }
            return -1;
        }

        private static bool IsConstantFieldBelongsToPackage(IFieldSymbol fieldSymbol, string sourcePackage)
        {
            // 对于常量字段，直接检查字段的定义位置
            // 如果字段在当前访问的包中有定义，就认为属于当前包
            
            foreach (var location in fieldSymbol.Locations)
            {
                if (location.IsInSource)
                {
                    var fieldPackage = GetPackageNameFromPath(location.SourceTree.FilePath);
                    if (fieldPackage == sourcePackage)
                    {
                        return true; // 字段在当前包中定义
                    }
                }
            }
            
            return false;
        }
        
        private static string FirstLetterToUpper(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}