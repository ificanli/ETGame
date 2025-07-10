using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    /// <summary>
    /// ET包依赖分析器 - 检查包之间的访问权限和依赖关系
    /// 
    /// 【设计原理】：
    /// 每个分析器实例独立工作，通过ProjectReference机制可以直接访问所有依赖程序集的符号
    /// 分析器实例从当前编译上下文中获取所有符号（包括依赖程序集），构建自己的符号表
    /// 利用ISymbol.Locations获取源码位置，从文件路径中提取包名信息
    /// 
    /// 【核心优势】：
    /// - 每个分析器实例职责单一，避免全局状态复杂性
    /// - 直接利用编译器提供的符号信息，无需跨实例共享
    /// - 内存使用更高效，生命周期管理更简单
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PackageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor FieldAccessForbiddenDescriptor = new DiagnosticDescriptor(
            "ET0101",
            "Field access forbidden",
            "Package '{0}' cannot access field '{1}' from package '{2}'",
            "Package",
            DiagnosticSeverity.Error,
            true
        );

        private static readonly DiagnosticDescriptor MethodCallForbiddenDescriptor = new DiagnosticDescriptor(
            "ET0102",
            "Method call forbidden",
            "Package '{0}' cannot call method '{1}' from package '{2}'",
            "Package",
            DiagnosticSeverity.Error,
            true
        );

        private static readonly DiagnosticDescriptor TypeAccessForbiddenDescriptor = new DiagnosticDescriptor(
            "ET0105",
            "Type access forbidden",
            "Package '{0}' cannot access type '{1}' from package '{2}'",
            "Package",
            DiagnosticSeverity.Error,
            true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            FieldAccessForbiddenDescriptor,
            MethodCallForbiddenDescriptor,
            TypeAccessForbiddenDescriptor
        );

        // 每个分析器实例独立的符号表
        private readonly Dictionary<string, string> _symbolTable = new();
        private readonly Dictionary<string, PackageInfo> _packageInfos = new();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(InitializeCompilation);
        }

        private void InitializeCompilation(CompilationStartAnalysisContext context)
        {
            // 【关键过滤】：只处理AnalyzeAssembly.All中定义的程序集
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }
            
            LoadPackageInfos(context.Compilation);
            
            // 构建当前编译上下文的符号表
            BuildSymbolTable(context.Compilation);
            
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
        }

        private void BuildSymbolTable(Compilation compilation)
        {
            try
            {
                // 处理当前编译的语法树
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    if (syntaxTree.FilePath != null)
                    {
                        var packageName = ExtractPackageNameFromFilePath(syntaxTree.FilePath);
                        if (!string.IsNullOrEmpty(packageName))
                        {
                            WriteSymbolsFromSyntaxTree(syntaxTree, packageName!);
                        }
                    }
                }
                
                // 处理依赖程序集中的符号（通过ProjectReference可访问）
                BuildSymbolsFromReferencedAssemblies(compilation);
            }
            catch (Exception)
            {
                // 继续执行
            }
        }
        
        private void BuildSymbolsFromReferencedAssemblies(Compilation compilation)
        {
            try
            {
                // 获取所有引用的程序集
                foreach (var reference in compilation.References)
                {
                    if (reference is CompilationReference compRef)
                    {
                        // 这是ProjectReference，可以访问源码
                        var referencedCompilation = compRef.Compilation;
                        foreach (var syntaxTree in referencedCompilation.SyntaxTrees)
                        {
                            if (syntaxTree.FilePath != null)
                            {
                                var packageName = ExtractPackageNameFromFilePath(syntaxTree.FilePath);
                                if (!string.IsNullOrEmpty(packageName))
                                {
                                    WriteSymbolsFromSyntaxTree(syntaxTree, packageName!);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 继续执行
            }
        }

        private string? ExtractPackageNameFromFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            
            var normalizedPath = filePath.Replace("\\", "/");
            
            if (normalizedPath.Contains("Packages"))
            {
                var packageStart = normalizedPath.IndexOf("Packages") + 9;
                var packageEnd = normalizedPath.IndexOf("/", packageStart);
                if (packageEnd > packageStart)
                {
                    var packageName = normalizedPath.Substring(packageStart, packageEnd - packageStart);
                    if (packageName.StartsWith("cn.etetet."))
                    {
                        return packageName;
                    }
                }
            }
            
            return null;
        }

        private void WriteSymbolsFromSyntaxTree(SyntaxTree syntaxTree, string packageName)
        {
            try
            {
                var root = syntaxTree.GetRoot();
                
                
                // 【关键修复】：只记录在当前文件中**定义**的符号，不记录使用的符号
                
                // 处理所有类型声明
                foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (IsInETNamespace(typeDecl))
                    {
                        var typeName = typeDecl.Identifier.ValueText;
                        var namespaceName = GetNamespaceName(typeDecl);
                        var fullTypeName = string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";
                        
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            // 记录完整类型名
                            _symbolTable[fullTypeName] = packageName;
                            
                            // 处理成员
                            foreach (var member in typeDecl.Members)
                            {
                                switch (member)
                                {
                                    case FieldDeclarationSyntax fieldDecl:
                                        foreach (var variable in fieldDecl.Declaration.Variables)
                                        {
                                            var fieldName = variable.Identifier.ValueText;
                                            if (!string.IsNullOrEmpty(fieldName))
                                            {
                                                var fieldKey = $"{fullTypeName}.{fieldName}";
                                                _symbolTable[fieldKey] = packageName;
                                            }
                                        }
                                        break;
                                    case MethodDeclarationSyntax methodDecl:
                                        var methodName = methodDecl.Identifier.ValueText;
                                        if (!string.IsNullOrEmpty(methodName))
                                        {
                                            // 构建完整的方法签名，包含泛型参数和方法参数
                                            var methodSignature = GetMethodSignature(fullTypeName, methodDecl);
                                            _symbolTable[methodSignature] = packageName;
                                        }
                                        break;
                                    case PropertyDeclarationSyntax propertyDecl:
                                        var propertyName = propertyDecl.Identifier.ValueText;
                                        if (!string.IsNullOrEmpty(propertyName))
                                        {
                                            var propertyKey = $"{fullTypeName}.{propertyName}";
                                            _symbolTable[propertyKey] = packageName;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                
                // 处理枚举
                foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
                {
                    if (IsInETNamespace(enumDecl))
                    {
                        var enumName = enumDecl.Identifier.ValueText;
                        var namespaceName = GetNamespaceName(enumDecl);
                        var fullEnumName = string.IsNullOrEmpty(namespaceName) ? enumName : $"{namespaceName}.{enumName}";
                        
                        if (!string.IsNullOrEmpty(enumName))
                        {
                            _symbolTable[fullEnumName] = packageName;
                            
                            foreach (var enumMember in enumDecl.Members)
                            {
                                var memberName = enumMember.Identifier.ValueText;
                                if (!string.IsNullOrEmpty(memberName))
                                {
                                    // 使用完整的枚举成员名（Namespace.EnumName.MemberName）
                                    var fullMemberName = $"{fullEnumName}.{memberName}";
                                    _symbolTable[fullMemberName] = packageName;
                                }
                            }
                        }
                    }
                }
                
                // 处理委托
                foreach (var delegateDecl in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
                {
                    if (IsInETNamespace(delegateDecl))
                    {
                        var delegateName = delegateDecl.Identifier.ValueText;
                        var namespaceName = GetNamespaceName(delegateDecl);
                        var fullDelegateName = string.IsNullOrEmpty(namespaceName) ? delegateName : $"{namespaceName}.{delegateName}";
                        
                        if (!string.IsNullOrEmpty(delegateName))
                        {
                            _symbolTable[fullDelegateName] = packageName;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 继续执行
            }
        }


        private bool IsInETNamespace(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax ns)
                {
                    var namespaceName = ns.Name.ToString();
                    return namespaceName == "ET" || namespaceName.StartsWith("ET.");
                }
                parent = parent.Parent;
            }
            return false;
        }

        private string GetNamespaceName(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax ns)
                {
                    return ns.Name.ToString();
                }
                parent = parent.Parent;
            }
            return string.Empty;
        }

        private string GetMethodSignature(string fullTypeName, MethodDeclarationSyntax methodDecl)
        {
            var methodName = methodDecl.Identifier.ValueText;
            var signature = $"{fullTypeName}.{methodName}";
            
            // 添加泛型参数
            if (methodDecl.TypeParameterList != null && methodDecl.TypeParameterList.Parameters.Count > 0)
            {
                var genericParams = string.Join(",", methodDecl.TypeParameterList.Parameters.Select(p => p.Identifier.ValueText));
                signature += $"<{genericParams}>";
            }
            
            // 添加方法参数
            var parameters = methodDecl.ParameterList.Parameters;
            if (parameters.Count > 0)
            {
                var paramTypes = parameters.Select(p => {
                    var typeName = p.Type?.ToString() ?? "unknown";
                    return typeName;
                });
                signature += $"({string.Join(",", paramTypes)})";
            }
            else
            {
                signature += "()";
            }
            
            return signature;
        }

        private string GetMethodSignatureFromSymbol(IMethodSymbol methodSymbol)
        {
            var fullTypeName = methodSymbol.ContainingType.ToDisplayString();
            var methodName = methodSymbol.Name;
            var signature = $"{fullTypeName}.{methodName}";
            
            // 添加泛型参数
            if (methodSymbol.TypeParameters.Length > 0)
            {
                var genericParams = string.Join(",", methodSymbol.TypeParameters.Select(tp => tp.Name));
                signature += $"<{genericParams}>";
            }
            
            // 添加方法参数
            if (methodSymbol.Parameters.Length > 0)
            {
                var paramTypes = methodSymbol.Parameters.Select(p => p.Type.ToDisplayString());
                signature += $"({string.Join(",", paramTypes)})";
            }
            else
            {
                signature += "()";
            }
            
            return signature;
        }

        private void LoadPackageInfos(Compilation compilation)
        {
            try
            {
                // 从编译上下文的文件路径中找到项目根目录
                var packagesPath = FindPackagesPathFromCompilation(compilation);
                if (string.IsNullOrEmpty(packagesPath)) 
                {
                    return;
                }

                var packageDirs = Directory.GetDirectories(packagesPath, "cn.etetet.*");
                
                foreach (var packageDir in packageDirs)
                {
                    var packageName = Path.GetFileName(packageDir);
                    var packageJsonPath = Path.Combine(packageDir, "package.json");
                    var packageGitJsonPath = Path.Combine(packageDir, "packagegit.json");

                    if (File.Exists(packageJsonPath))
                    {
                        var packageInfo = LoadPackageInfo(packageJsonPath, packageGitJsonPath);
                        if (packageInfo != null)
                        {
                            _packageInfos[packageName] = packageInfo;
                        }
                    }
                }
                
                // 【性能优化】：预计算所有包的完整依赖集合
                PrecomputeAllDependencies();
            }
            catch (Exception)
            {
                // 继续执行
            }
        }

        private string FindPackagesPathFromCompilation(Compilation compilation)
        {
            try
            {
                // 从编译中的语法树文件路径中寻找包含Packages目录的项目根目录
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    if (syntaxTree.FilePath != null)
                    {
                        var filePath = syntaxTree.FilePath.Replace("\\", "/");
                        
                        // 寻找包含Packages的路径结构
                        if (filePath.Contains("/Packages/"))
                        {
                            var packagesIndex = filePath.IndexOf("/Packages/");
                            var projectRoot = filePath.Substring(0, packagesIndex);
                            var packagesPath = Path.Combine(projectRoot, "Packages");
                            
                            if (Directory.Exists(packagesPath))
                            {
                                return packagesPath;
                            }
                        }
                    }
                }
                
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private PackageInfo? LoadPackageInfo(string packageJsonPath, string packageGitJsonPath)
        {
            try
            {
                var packageJsonContent = File.ReadAllText(packageJsonPath);
                var packageName = ExtractJsonStringValue(packageJsonContent, "name");
                if (string.IsNullOrEmpty(packageName))
                    return null;

                var packageInfo = new PackageInfo
                {
                    Name = packageName,
                    Dependencies = new List<string>(),
                    Level = 0,
                    AllowSameLevelAccess = false
                };

                var dependencies = ExtractJsonObjectKeys(packageJsonContent);
                packageInfo.Dependencies.AddRange(dependencies);

                // 读取 packagegit.json 文件获取层级和同层访问配置
                if (File.Exists(packageGitJsonPath))
                {
                    var packageGitContent = File.ReadAllText(packageGitJsonPath);
                    var levelStr = ExtractJsonStringValue(packageGitContent, "Level");
                    if (int.TryParse(levelStr, out int level))
                    {
                        packageInfo.Level = level;
                    }
                    
                    var allowSameLevelStr = ExtractJsonStringValue(packageGitContent, "AllowSameLevelAccess");
                    if (bool.TryParse(allowSameLevelStr, out bool allowSameLevel))
                    {
                        packageInfo.AllowSameLevelAccess = allowSameLevel;
                    }
                    
                }

                return packageInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string ExtractJsonStringValue(string json, string key)
        {
            var pattern = $@"""{key}""\s*:\s*""([^""]+)""|""{key}""\s*:\s*([0-9]+)|""{key}""\s*:\s*(true|false)";
            var match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Success ? match.Groups[1].Value : 
                       match.Groups[2].Success ? match.Groups[2].Value : 
                       match.Groups[3].Value;
            }
            return string.Empty;
        }

        private List<string> ExtractJsonObjectKeys(string json)
        {
            var keys = new List<string>();
            
            try
            {
                // 【关键修复】：只从dependencies字段中提取依赖包
                var dependenciesPattern = @"""dependencies""\s*:\s*\{([^}]*)\}";
                var dependenciesMatch = Regex.Match(json, dependenciesPattern);
                
                if (dependenciesMatch.Success)
                {
                    var dependenciesContent = dependenciesMatch.Groups[1].Value;
                    var packagePattern = @"""(cn\.etetet\.[^""]+)""\s*:\s*""[^""]*""";
                    var matches = Regex.Matches(dependenciesContent, packagePattern);
                    
                    foreach (Match match in matches)
                    {
                        var packageName = match.Groups[1].Value.Trim();
                        keys.Add(packageName);
                    }
                }
            }
            catch (Exception)
            {
                // 继续执行
            }
            
            return keys;
        }

        // 分析方法
        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            
            if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
            {
                CheckFieldAccess(context, memberAccess, fieldSymbol);
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                CheckMethodCall(context, invocation, methodSymbol);
            }
        }

        private void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifier = (IdentifierNameSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
            
            if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
            {
                CheckFieldAccess(context, identifier, fieldSymbol);
            }
        }

        private void AnalyzeObjectCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation);
            
            if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
            {
                CheckTypeAccess(context, objectCreation, constructorSymbol.ContainingType);
            }
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var variableDeclaration = (VariableDeclarationSyntax)context.Node;
            var typeInfo = context.SemanticModel.GetTypeInfo(variableDeclaration.Type);
            
            if (typeInfo.Type != null)
            {
                CheckTypeAccess(context, variableDeclaration.Type, typeInfo.Type);
            }
        }

        private void CheckFieldAccess(SyntaxNodeAnalysisContext context, SyntaxNode node, IFieldSymbol fieldSymbol)
        {
            var currentPackage = GetPackageFromNamespace(node);
            var targetPackage = GetPackageFromSymbol(fieldSymbol);
            
            if (fieldSymbol.ContainingType?.TypeKind == TypeKind.Enum)
            {
                targetPackage = GetPackageFromSymbol(fieldSymbol.ContainingType);
            }
            
            if (currentPackage != null && targetPackage != null && !CanAccessSymbol(currentPackage, targetPackage))
            {
                var diagnostic = Diagnostic.Create(
                    FieldAccessForbiddenDescriptor,
                    node.GetLocation(),
                    currentPackage,
                    fieldSymbol.Name,
                    targetPackage
                );
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void CheckMethodCall(SyntaxNodeAnalysisContext context, SyntaxNode node, IMethodSymbol methodSymbol)
        {
            var currentPackage = GetPackageFromNamespace(node);
            var targetPackage = GetPackageFromSymbol(methodSymbol);
            
            
            if (currentPackage != null && targetPackage != null && !CanAccessSymbol(currentPackage, targetPackage))
            {
                
                var diagnostic = Diagnostic.Create(
                    MethodCallForbiddenDescriptor,
                    node.GetLocation(),
                    currentPackage,
                    methodSymbol.Name,
                    targetPackage
                );
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void CheckTypeAccess(SyntaxNodeAnalysisContext context, SyntaxNode node, ITypeSymbol typeSymbol)
        {
            var currentPackage = GetPackageFromNamespace(node);
            var targetPackage = GetPackageFromSymbol(typeSymbol);
            
            
            if (currentPackage != null && targetPackage != null && currentPackage != targetPackage)
            {
                if (!CanAccessSymbol(currentPackage, targetPackage))
                {
                    
                    var diagnostic = Diagnostic.Create(
                        TypeAccessForbiddenDescriptor,
                        node.GetLocation(),
                        currentPackage,
                        typeSymbol.Name,
                        targetPackage
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private string? GetPackageFromNamespace(SyntaxNode node)
        {
            // 【关键修复】：对于partial类，应该根据当前语法树文件的位置来确定包归属
            // 而不是根据符号表中的类型名，因为partial类可能分布在多个包中
            
            if (node.SyntaxTree?.FilePath != null)
            {
                var packageFromPath = ExtractPackageNameFromFilePath(node.SyntaxTree.FilePath);
                if (!string.IsNullOrEmpty(packageFromPath))
                {
                    // 确保这确实是在ET命名空间中
                    var typeDeclaration = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                    if (typeDeclaration != null && IsInETNamespace(typeDeclaration))
                    {
                        return packageFromPath;
                    }
                }
            }
            
            // 没有找到类型声明，可能是全局代码，不需要包检查
            return null;
        }

        private string? GetPackageFromSymbol(ISymbol symbol)
        {
            if (symbol == null) return null;
            
            // 检查是否是ET类型
            var namespaceName = symbol.ContainingNamespace?.ToDisplayString();
            if (namespaceName == null || (namespaceName != "ET" && !namespaceName.StartsWith("ET.")))
            {
                return null; // 非ET类型允许自由访问
            }
            
            // 【跳过检查】：泛型类型参数、委托类型、局部函数等不需要包检查
            if (symbol is ITypeParameterSymbol || 
                symbol.Kind == SymbolKind.TypeParameter ||
                symbol.ContainingType?.TypeKind == TypeKind.Delegate)
            {
                return null;
            }
            
            // 【跳过检查】：局部函数不需要包检查
            if (symbol is IMethodSymbol methodSym && methodSym.MethodKind == MethodKind.LocalFunction)
            {
                return null;
            }
            
            // 使用FullName从符号表中查找
            if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.ContainingType != null)
            {
                var fullTypeName = fieldSymbol.ContainingType.ToDisplayString();
                var fieldKey = $"{fullTypeName}.{fieldSymbol.Name}";
                if (_symbolTable.TryGetValue(fieldKey, out string? fieldPackage))
                {
                    return fieldPackage;
                }
            }
            
            if (symbol is IMethodSymbol methodSymbol && methodSymbol.ContainingType != null)
            {
                // 构建完整的方法签名进行查找
                var methodSignature = GetMethodSignatureFromSymbol(methodSymbol);
                if (_symbolTable.TryGetValue(methodSignature, out string? methodPackage))
                {
                    return methodPackage;
                }
            }
            
            if (symbol is IPropertySymbol propertySymbol && propertySymbol.ContainingType != null)
            {
                var fullTypeName = propertySymbol.ContainingType.ToDisplayString();
                var propertyKey = $"{fullTypeName}.{propertySymbol.Name}";
                if (_symbolTable.TryGetValue(propertyKey, out string? propertyPackage))
                {
                    return propertyPackage;
                }
            }
            
            // 查询类型（使用FullName）
            var symbolFullName = symbol.ToDisplayString();
            if (_symbolTable.TryGetValue(symbolFullName, out string? typePackage))
            {
                return typePackage;
            }
            
            // 【特殊情况】：如果是系统定义的委托、方法等，允许自由访问
            if (symbol.ContainingNamespace?.ToDisplayString() == "System" || 
                symbol.ContainingAssembly?.Name?.StartsWith("System") == true ||
                symbol.ContainingAssembly?.Name?.StartsWith("Microsoft") == true ||
                symbol.ContainingAssembly?.Name?.StartsWith("mscorlib") == true ||
                symbol.ContainingAssembly?.Name?.StartsWith("netstandard") == true)
            {
                return null;
            }
            
            // 【关键判断】：如果符号表中找不到，需要判断这个符号是否真的是ET框架中声明的
            // 检查符号的定义位置是否在ET框架的源文件中
            var locations = symbol.Locations;
            foreach (var location in locations)
            {
                if (location.IsInSource && location.SourceTree?.FilePath != null)
                {
                    var packageFromPath = ExtractPackageNameFromFilePath(location.SourceTree.FilePath);
                    if (!string.IsNullOrEmpty(packageFromPath))
                    {
                        // 找到符号的包归属，直接返回
                        return packageFromPath;
                    }
                }
            }
            
            // 符号不是在ET框架中声明的，允许自由访问
            return null;
        }

        private bool CanAccessSymbol(string currentPackage, string targetPackage)
        {
            // 【包依赖规范】：包中只能访问自己包或者依赖包的符号
            if (currentPackage == targetPackage) return true;
            
            if (!_packageInfos.TryGetValue(currentPackage, out var currentInfo))
            {
                return false;
            }
            
            if (!_packageInfos.TryGetValue(targetPackage, out var targetInfo))
            {
                return false;
            }
            
            // 【传统依赖链检查】：先检查是否在依赖链中
            if (IsInDependencyChain(currentInfo, targetPackage))
            {
                return true;
            }
            
            // 【新增同层访问规则】：检查同层访问权限
            if (currentInfo.Level == targetInfo.Level && targetInfo.AllowSameLevelAccess)
            {
                // 如果目标包允许同层访问，还需要确保当前包没有被目标包依赖
                // 即：如果A包引用了同层包B，那么B包无论如何都不能访问A包
                bool targetDependsOnCurrent = IsInDependencyChain(targetInfo, currentPackage);
                
                if (!targetDependsOnCurrent)
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool IsInDependencyChain(PackageInfo currentPackage, string targetPackage)
        {
            if (string.IsNullOrEmpty(targetPackage))
            {
                return false;
            }
            
            // 【性能优化】：使用预计算的完整依赖集合，O(1) 查找
            if (currentPackage.AllDependencies.Count > 0)
            {
                return currentPackage.AllDependencies.Contains(targetPackage);
            }
            
            // 【回退方案】：如果预计算失败，使用原有的实时计算方式
            if (currentPackage.Dependencies.Contains(targetPackage))
            {
                return true;
            }
            
            var visited = new HashSet<string>();
            var toVisit = new Queue<string>();
            
            foreach (var dependency in currentPackage.Dependencies)
            {
                toVisit.Enqueue(dependency);
            }
            
            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                
                if (visited.Contains(current)) continue;
                visited.Add(current);
                
                if (current == targetPackage) 
                {
                    return true;
                }
                
                if (_packageInfos.TryGetValue(current, out var packageInfo))
                {
                    foreach (var dependency in packageInfo.Dependencies)
                    {
                        if (!visited.Contains(dependency))
                        {
                            toVisit.Enqueue(dependency);
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 预计算所有包的完整依赖集合，包括传递依赖
        /// </summary>
        private void PrecomputeAllDependencies()
        {
            try
            {
                // 使用拓扑排序确保依赖计算顺序正确
                var computed = new HashSet<string>();
                var computing = new HashSet<string>();
                
                foreach (var packageName in _packageInfos.Keys)
                {
                    ComputePackageDependencies(packageName, computed, computing);
                }
            }
            catch (Exception)
            {
                // 继续执行，如果预计算失败，会回退到原有的实时计算方式
            }
        }

        /// <summary>
        /// 递归计算单个包的所有依赖
        /// </summary>
        private void ComputePackageDependencies(string packageName, HashSet<string> computed, HashSet<string> computing)
        {
            if (computed.Contains(packageName) || !_packageInfos.TryGetValue(packageName, out var packageInfo))
            {
                return;
            }
            
            // 检测循环依赖
            if (computing.Contains(packageName))
            {
                return; // 循环依赖，跳过
            }
            
            computing.Add(packageName);
            
            // 先计算所有直接依赖的完整依赖集合
            foreach (var dependency in packageInfo.Dependencies)
            {
                ComputePackageDependencies(dependency, computed, computing);
                
                // 添加直接依赖
                packageInfo.AllDependencies.Add(dependency);
                
                // 添加传递依赖
                if (_packageInfos.TryGetValue(dependency, out var depInfo))
                {
                    foreach (var transitiveDep in depInfo.AllDependencies)
                    {
                        packageInfo.AllDependencies.Add(transitiveDep);
                    }
                }
            }
            
            computing.Remove(packageName);
            computed.Add(packageName);
        }

        private class PackageInfo
        {
            public string Name { get; set; } = string.Empty;
            public List<string> Dependencies { get; set; } = new List<string>();
            public int Level { get; set; } = 0;
            public bool AllowSameLevelAccess { get; set; } = false;
            
            /// <summary>
            /// 预计算的完整依赖集合（包括传递依赖）
            /// </summary>
            public HashSet<string> AllDependencies { get; set; } = new HashSet<string>();
        }
    }
}