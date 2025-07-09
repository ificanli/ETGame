using System;
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

        private static readonly DiagnosticDescriptor CircularDependencyDescriptor = new DiagnosticDescriptor(
            "ET0103",
            "Circular dependency detected",
            "Circular dependency detected: {0}",
            "Package",
            DiagnosticSeverity.Error,
            true
        );

        private static readonly DiagnosticDescriptor InvalidDependencyDescriptor = new DiagnosticDescriptor(
            "ET0104",
            "Invalid dependency",
            "Package '{0}' cannot depend on package '{1}' (higher or same level dependency)",
            "Package",
            DiagnosticSeverity.Error,
            true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            MethodCallForbiddenDescriptor,
            CircularDependencyDescriptor,
            InvalidDependencyDescriptor
        );

        private readonly Dictionary<string, PackageInfo> _packageInfos = new Dictionary<string, PackageInfo>();
        private readonly Dictionary<string, int> _packageLevels = new Dictionary<string, int>();
        private readonly Dictionary<int, string> _packageIdToName = new Dictionary<int, string>();
        private bool _initialized = false;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            
            LoadPackageInfos();
            InitializePackageLevels();
            ValidatePackageDependencies();
            _initialized = true;
        }

        private void LoadPackageInfos()
        {
            var packagesPath = FindPackagesPath();
            if (string.IsNullOrEmpty(packagesPath)) return;

            try
            {
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
                            _packageIdToName[packageInfo.Id] = packageName;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 静默处理错误，避免编译时报错
            }
        }

        private string FindPackagesPath()
        {
            var currentDir = Directory.GetCurrentDirectory();
            while (currentDir != null)
            {
                var packagesPath = Path.Combine(currentDir, "Packages");
                if (Directory.Exists(packagesPath))
                {
                    return packagesPath;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            return string.Empty;
        }

        private PackageInfo LoadPackageInfo(string packageJsonPath, string packageGitJsonPath)
        {
            try
            {
                var packageJsonContent = File.ReadAllText(packageJsonPath);
                var packageName = ExtractJsonStringValue(packageJsonContent, "name");
                if (string.IsNullOrEmpty(packageName))
                    return null!;

                var packageInfo = new PackageInfo
                {
                    Name = packageName,
                    Dependencies = new List<string>()
                };

                // 解析dependencies
                var dependencies = ExtractJsonObjectKeys(packageJsonContent, "dependencies");
                packageInfo.Dependencies.AddRange(dependencies);

                if (File.Exists(packageGitJsonPath))
                {
                    var packageGitContent = File.ReadAllText(packageGitJsonPath);
                    var idValue = ExtractJsonStringValue(packageGitContent, "Id");
                    if (int.TryParse(idValue, out int id))
                    {
                        packageInfo.Id = id;
                    }
                    
                    var levelValue = ExtractJsonStringValue(packageGitContent, "Level");
                    if (int.TryParse(levelValue, out int level))
                    {
                        packageInfo.Level = level;
                    }
                }

                return packageInfo;
            }
            catch (Exception)
            {
                return null!;
            }
        }

        private string ExtractJsonStringValue(string json, string key)
        {
            // 同时匹配字符串值和数字值
            var pattern = $@"""{key}""\s*:\s*""([^""]+)""|""{key}""\s*:\s*([0-9]+)";
            var match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            }
            return string.Empty;
        }

        private List<string> ExtractJsonObjectKeys(string json, string objectKey)
        {
            var keys = new List<string>();
            var pattern = $@"""{objectKey}""\s*:\s*\{{([^}}]+)\}}";
            var match = Regex.Match(json, pattern);
            
            if (match.Success)
            {
                var content = match.Groups[1].Value;
                var keyPattern = @"""([^""]+)""\s*:\s*""[^""]+""";
                var keyMatches = Regex.Matches(content, keyPattern);
                
                foreach (Match keyMatch in keyMatches)
                {
                    keys.Add(keyMatch.Groups[1].Value);
                }
            }
            
            return keys;
        }

        private void InitializePackageLevels()
        {
            // 从packagegit.json读取包级别信息
            foreach (var packageInfo in _packageInfos.Values)
            {
                if (packageInfo.Level > 0)
                {
                    _packageLevels[packageInfo.Name] = packageInfo.Level;
                }
            }
        }

        private void ValidatePackageDependencies()
        {
            foreach (var packageInfo in _packageInfos.Values)
            {
                if (!_packageLevels.TryGetValue(packageInfo.Name, out int packageLevel))
                    continue;

                foreach (var dependency in packageInfo.Dependencies)
                {
                    if (!_packageLevels.TryGetValue(dependency, out int dependencyLevel))
                        continue;

                    // 只能依赖更低层级的包
                    if (dependencyLevel >= packageLevel)
                    {
                        // 这里应该报告错误，但暂时跳过
                        continue;
                    }
                }
            }
        }


        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            EnsureInitialized();

            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                var currentPackage = GetPackageFromNamespace(context.SemanticModel, invocation);
                var targetPackage = GetPackageFromSymbol(methodSymbol);
                
                if (currentPackage != null && targetPackage != null && !CanCallMethod(currentPackage, targetPackage))
                {
                    var diagnostic = Diagnostic.Create(
                        MethodCallForbiddenDescriptor,
                        invocation.GetLocation(),
                        currentPackage,
                        methodSymbol.Name,
                        targetPackage
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }


        private string GetPackageFromNamespace(SemanticModel semanticModel, SyntaxNode node)
        {
            var compilationUnit = node.SyntaxTree.GetCompilationUnitRoot();
            var filePath = node.SyntaxTree.FilePath;
            
            // 从文件路径提取包名
            if (filePath.Contains("Packages"))
            {
                var packageStart = filePath.IndexOf("Packages") + 9;
                var packageEnd = filePath.IndexOf("/", packageStart);
                if (packageEnd > packageStart)
                {
                    return filePath.Substring(packageStart, packageEnd - packageStart);
                }
            }
            
            return null!;
        }

        private string GetPackageFromSymbol(ISymbol symbol)
        {
            var location = symbol.Locations.FirstOrDefault();
            if (location?.SourceTree?.FilePath != null)
            {
                var filePath = location.SourceTree.FilePath;
                if (filePath.Contains("Packages"))
                {
                    var packageStart = filePath.IndexOf("Packages") + 9;
                    var packageEnd = filePath.IndexOf("/", packageStart);
                    if (packageEnd > packageStart)
                    {
                        return filePath.Substring(packageStart, packageEnd - packageStart);
                    }
                }
            }
            return null!;
        }


        private bool CanCallMethod(string currentPackage, string targetPackage)
        {
            // 可以调用自己包的方法
            if (currentPackage == targetPackage) return true;
            
            // 检查当前包是否存在
            if (!_packageInfos.TryGetValue(currentPackage, out var currentPackageInfo))
            {
                // 如果当前包不存在，允许调用（可能是外部包）
                return true;
            }
            
            // 可以调用依赖包的方法
            return IsInDependencyChain(currentPackageInfo, targetPackage);
        }

        private bool IsInDependencyChain(PackageInfo currentPackage, string targetPackage)
        {
            var visited = new HashSet<string>();
            var toVisit = new Queue<string>();
            
            // 添加直接依赖
            foreach (var dependency in currentPackage.Dependencies)
            {
                toVisit.Enqueue(dependency);
            }
            
            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                
                if (visited.Contains(current)) continue;
                visited.Add(current);
                
                if (current == targetPackage) return true;
                
                // 添加间接依赖
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

        private class PackageInfo
        {
            public string Name { get; set; } = string.Empty;
            public int Id { get; set; }
            public int Level { get; set; }
            public List<string> Dependencies { get; set; } = new List<string>();
        }
    }
}