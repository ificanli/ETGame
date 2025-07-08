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
                }
            }));
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
                
            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol == null)
                return;
                
            var targetSymbol = symbolInfo.Symbol;
            var targetType = GetContainingType(targetSymbol);
            if (targetType == null)
                return;
                
            var targetPackage = GetPackageNameFromType(targetType);
            if (string.IsNullOrEmpty(targetPackage))
                return; // partial class或无法确定包名，允许访问
                
            // 同一个包内的访问允许
            if (sourcePackage == targetPackage)
                return;
                
            // 特殊处理：对于partial class（如Opcode），需要检查成员是否在当前包中定义
            if (IsPartialClassMember(targetType, targetSymbol, sourcePackage))
                return; // 成员在当前包中定义，允许访问
                
            // 检查字段访问：包与包之间不允许访问字段
            if (targetSymbol is IFieldSymbol)
            {
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
            
            // 对于partial class（如PackageType），需要特殊处理
            // 分析器应该根据访问的具体成员来判断包名，而不是类型本身
            
            // 优先查找主要定义（通常是第一个非partial的定义）
            string primaryPackage = null;
            var allPackages = new List<string>();
            
            foreach (var location in locations)
            {
                if (location.IsInSource)
                {
                    var packageName = GetPackageNameFromPath(location.SourceTree.FilePath);
                    if (!string.IsNullOrEmpty(packageName))
                    {
                        // 如果找到了cn.etetet包，优先返回
                        if (packageName.StartsWith("cn.etetet."))
                        {
                            allPackages.Add(packageName);
                            
                            // 对于Unit类型，应该优先选择cn.etetet.unit包
                            if (typeSymbol.Name == "Unit" && packageName == "cn.etetet.unit")
                            {
                                return packageName;
                            }
                            if (primaryPackage == null)
                            {
                                primaryPackage = packageName;
                            }
                        }
                    }
                }
            }
            
            // 对于partial class，如果有多个定义位置，返回null让调用者特殊处理
            if (allPackages.Count > 1 && (typeSymbol.Name == "PackageType" || typeSymbol.Name == "NumericType" || typeSymbol.Name == "SceneType"))
            {
                return null; // 表示这是个partial class，需要特殊处理
            }
            
            return primaryPackage;
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
            // 检查成员是否在当前包中定义
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
    }
}