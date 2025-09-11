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
            TypeAccessForbiddenDescriptor,
            DebugInfoDescriptor,
            CircularDependencyDescriptor
        );

        // 每个分析器实例独立的符号表
        private readonly Dictionary<string, string> _symbolTable = new();
        private readonly Dictionary<string, PackageInfo> _packageInfos = new();
        
        // 初始化锁，确保符号表构建完成后再开始分析
        private readonly object _initLock = new object();
        private volatile bool _isInitialized = false;
        
        // 【性能优化】：预计算的扁平化依赖关系，O(1)查找
        private readonly Dictionary<string, HashSet<string>> _flatDependencies = new();
        
        // 【性能优化】：预编译正则表达式
        private static readonly Regex DependenciesRegex = new(
            @"""dependencies""\s*:\s*\{([^}]*)\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
            
        private static readonly Regex PackageNameRegex = new(
            @"""(cn\.etetet\.[^""]+)""\s*:\s*""[^""]*""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 【分析器日志】：用于调试的诊断报告
        private static readonly DiagnosticDescriptor DebugInfoDescriptor = new DiagnosticDescriptor(
            "ET9999",
            "PackageAnalyzer Debug Info",
            "Debug: {0}",
            "Debug",
            DiagnosticSeverity.Info,
            true
        );

        // 【循环依赖错误】：检测到循环依赖时立即报错
        private static readonly DiagnosticDescriptor CircularDependencyDescriptor = new DiagnosticDescriptor(
            "ET0103",
            "Circular dependency detected",
            "Package '{0}' has circular dependency. This violates ET framework architecture rules.",
            "Package",
            DiagnosticSeverity.Error,
            true
        );

        /// <summary>
        /// 【改进】：在分析器中输出调试信息的正确方式
        /// </summary>
        private void LogDebugInfo(SyntaxNodeAnalysisContext context, string message)
        {
#if DEBUG
            // 方案1：通过诊断报告输出（会在编译输出中显示）
            var diagnostic = Diagnostic.Create(
                DebugInfoDescriptor,
                Location.None,
                message
            );
            context.ReportDiagnostic(diagnostic);
#endif
        }

        /// <summary>
        /// 【简化】：只使用临时目录写入日志
        /// </summary>
        private static void LogToFile(string message)
        {
#if DEBUG
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                var tempLogPath = Path.Combine(Path.GetTempPath(), "PackageAnalyzer.log");
                File.AppendAllText(tempLogPath, logMessage);
            }
            catch
            {
                // 静默失败，避免影响分析器正常工作
            }
#endif
        }


        /// <summary>
        /// 【循环依赖检查】：检查循环依赖，如果发现则抛出异常
        /// </summary>
        private void CheckAndReportCircularDependencies(CompilationStartAnalysisContext context)
        {
            try
            {
                var circularPackages = new HashSet<string>();
                DetectCircularDependencies(_packageInfos, circularPackages);
                
                // 如果有循环依赖，记录错误信息并注册检查action
                if (circularPackages.Count > 0)
                {
                    foreach (var packageName in circularPackages)
                    {
                        string errorMessage = packageName;
                        
                        // 如果有循环路径信息，包含在错误消息中
                        if (_packageInfos.TryGetValue(packageName, out var packageInfo) && packageInfo.CircularPath != null)
                        {
                            var pathStr = string.Join(" -> ", packageInfo.CircularPath);
                            errorMessage = $"{packageName}. Circular dependency path: {pathStr}";
                            LogToFile($"CIRCULAR DEPENDENCY DETECTED: {errorMessage}");
                        }
                    }
                    
                    // 必须注册一个非CompilationEndAction的操作来满足分析器框架要求
                    // 同时注册CompilationEndAction来报告错误
                    context.RegisterSyntaxNodeAction(_ => { }, SyntaxKind.CompilationUnit);
                    
                    var circularPackagesList = circularPackages.ToList();
                    context.RegisterCompilationEndAction(compilationContext =>
                    {
                        foreach (var packageName in circularPackagesList)
                        {
                            string errorMessage = packageName;
                            
                            if (_packageInfos.TryGetValue(packageName, out var packageInfo) && packageInfo.CircularPath != null)
                            {
                                var pathStr = string.Join(" -> ", packageInfo.CircularPath);
                                errorMessage = $"{packageName}. Circular dependency path: {pathStr}";
                            }
                            
                            var diagnostic = Diagnostic.Create(
                                CircularDependencyDescriptor,
                                Location.None,
                                errorMessage
                            );
                            compilationContext.ReportDiagnostic(diagnostic);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogToFile($"CheckAndReportCircularDependencies error: {ex.Message}");
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            //context.RegisterCompilationStartAction(InitializeCompilation);
        }

        private void InitializeCompilation(CompilationStartAnalysisContext context)
        {
            // 【关键过滤】：只处理AnalyzeAssembly.All中定义的程序集
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }
            
            
            // 确保符号表完全构建完成后再注册分析动作
            lock (_initLock)
            {
                if (!_isInitialized)
                {
                    LoadPackageInfos(context.Compilation, context);
                    
                    // 构建当前编译上下文的符号表
                    BuildSymbolTable(context.Compilation);
                    
                    _isInitialized = true;
                }
            }
            
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
        }

        // 【修复重复处理】：记录已处理的文件路径，避免重复处理
        private readonly HashSet<string> _processedFiles = new();
        
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
            catch (Exception ex)
            {
                // 【改进错误处理】：使用分析器专用日志方式
                LogToFile($"BuildSymbolTable error: {ex.Message}");
#if DEBUG
                LogToFile($"Stack trace: {ex.StackTrace}");
#endif
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
            catch (Exception ex)
            {
                // 【改进错误处理】：记录错误但继续执行
                LogToFile($"BuildSymbolsFromReferencedAssemblies error: {ex.Message}");
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
                            // 【修复partial类问题】：对于partial类，只有主要定义才记录到符号表
                            // 检查是否为partial类
                            bool isPartial = typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                            
                            if (isPartial)
                            {
                                // 【特殊处理partial static class】：如PackageType, MailBoxType等
                                // 这些类的成员（常量字段）应该归属到定义它们的包，而不是某个"主要"包
                                bool isStaticClass = typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
                                
                                if (isStaticClass && (typeName == "PackageType" || typeName == "MailBoxType" || 
                                                     typeName == "SceneType" || typeName == "TimerInvokeType" ||
                                                     typeName == "ErrorCode"))
                                {
                                    // 对于partial static class，每个包都可以定义自己的成员
                                    // 不记录类型本身到符号表，但记录成员到当前包
                                    ProcessPartialStaticClassMembers(typeDecl, fullTypeName, packageName);
                                    continue;
                                }
                                
                                // 对于普通partial类，只有主要包才能拥有该类型
                                // 判断标准：如果该类型已经在符号表中存在，且不是当前包，则跳过类型记录
                                if (_symbolTable.TryGetValue(fullTypeName, out string existingPackage) && existingPackage != packageName)
                                {
                                    // 已经有其他包定义了这个partial类，跳过类型记录
                                    // 但仍需要处理成员，且成员归属到当前包（定义成员的包）
                                    ProcessPartialClassMembers(typeDecl, fullTypeName, packageName);
                                    continue;
                                }
                            }
                            
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
            catch (Exception ex)
            {
                // 【改进错误处理】：记录错误但继续执行
                LogToFile($"WriteSymbolsFromSyntaxTree error in file {syntaxTree.FilePath}: {ex.Message}");
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

        /// <summary>
        /// 处理partial static类的成员，将成员归属到当前包
        /// </summary>
        private void ProcessPartialStaticClassMembers(TypeDeclarationSyntax typeDecl, string fullTypeName, string currentPackage)
        {
            // 对于partial static类（如PackageType），每个包的成员都归属到当前包
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
                                _symbolTable[fieldKey] = currentPackage;
                            }
                        }
                        break;
                    case PropertyDeclarationSyntax propertyDecl:
                        var propertyName = propertyDecl.Identifier.ValueText;
                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            var propertyKey = $"{fullTypeName}.{propertyName}";
                            _symbolTable[propertyKey] = currentPackage;
                        }
                        break;
                    case MethodDeclarationSyntax methodDecl:
                        var methodName = methodDecl.Identifier.ValueText;
                        if (!string.IsNullOrEmpty(methodName))
                        {
                            var methodSignature = GetMethodSignature(fullTypeName, methodDecl);
                            _symbolTable[methodSignature] = currentPackage;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 处理partial类的成员，将成员归属到定义它们的包
        /// </summary>
        private void ProcessPartialClassMembers(TypeDeclarationSyntax typeDecl, string fullTypeName, string memberPackage)
        {
            // 处理partial类的成员，将它们归属到定义成员的包
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
                                _symbolTable[fieldKey] = memberPackage;
                            }
                        }
                        break;
                    case MethodDeclarationSyntax methodDecl:
                        var methodName = methodDecl.Identifier.ValueText;
                        if (!string.IsNullOrEmpty(methodName))
                        {
                            var methodSignature = GetMethodSignature(fullTypeName, methodDecl);
                            _symbolTable[methodSignature] = memberPackage;
                        }
                        break;
                    case PropertyDeclarationSyntax propertyDecl:
                        var propertyName = propertyDecl.Identifier.ValueText;
                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            var propertyKey = $"{fullTypeName}.{propertyName}";
                            _symbolTable[propertyKey] = memberPackage;
                        }
                        break;
                }
            }
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

        private void LoadPackageInfos(Compilation compilation, CompilationStartAnalysisContext context)
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
                
                // 【循环依赖检查】：在包信息加载完成后立即检查循环依赖
                CheckAndReportCircularDependencies(context);
                
                // 【性能优化】：预计算所有包的完整依赖集合（循环依赖检查通过后才进行）
                PrecomputeAllDependenciesFlat();
            }
            catch (Exception ex)
            {
                // 【改进错误处理】：记录错误但继续执行
                LogToFile($"LoadPackageInfos error: {ex.Message}");
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
                    
                    var allowAnyAccessStr = ExtractJsonStringValue(packageGitContent, "AllowAnyPackageAccess");
                    if (bool.TryParse(allowAnyAccessStr, out bool allowAnyAccess))
                    {
                        packageInfo.AllowAnyPackageAccess = allowAnyAccess;
                    }
                    
                }

                return packageInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 【优化】：改进JSON值提取的错误处理
        /// </summary>
        private string ExtractJsonStringValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return string.Empty;
                
            try
            {
                var pattern = $@"""{key}""\s*:\s*""([^""]+)""|""{key}""\s*:\s*([0-9]+)|""{key}""\s*:\s*(true|false)";
                var match = Regex.Match(json, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Success ? match.Groups[1].Value : 
                           match.Groups[2].Success ? match.Groups[2].Value : 
                           match.Groups[3].Value;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"ExtractJsonStringValue error for key '{key}': {ex.Message}");
            }
            
            return string.Empty;
        }

        /// <summary>
        /// 【优化】：使用预编译正则表达式提取依赖包列表
        /// </summary>
        private List<string> ExtractJsonObjectKeys(string json)
        {
            var keys = new List<string>();
            
            if (string.IsNullOrEmpty(json))
                return keys;
            
            try
            {
                // 使用预编译的正则表达式
                var dependenciesMatch = DependenciesRegex.Match(json);
                
                if (dependenciesMatch.Success)
                {
                    var dependenciesContent = dependenciesMatch.Groups[1].Value;
                    var matches = PackageNameRegex.Matches(dependenciesContent);
                    
                    foreach (Match match in matches)
                    {
                        var packageName = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(packageName))
                        {
                            keys.Add(packageName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"ExtractJsonObjectKeys error: {ex.Message}");
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
            
            // 【性能优化】：使用预计算的扁平化依赖关系，O(1)查找
            if (_flatDependencies.TryGetValue(currentPackage, out var dependencies))
            {
                if (dependencies.Contains(targetPackage))
                {
                    return true;
                }
            }

            // 新增：如果目标包设置了 AllowAnyPackageAccess，则允许任何包访问（但仍禁止循环依赖）
            if (targetInfo.AllowAnyPackageAccess)
            {
                return true;
            }
            
            // 【新增同层访问规则】：检查同层访问权限
            if (currentInfo.Level == targetInfo.Level && targetInfo.AllowSameLevelAccess)
            {
                // 修复：只检查直接依赖，而不是扁平化依赖
                // 扁平化依赖可能包含传递依赖，导致误判
                if (_packageInfos.TryGetValue(targetPackage, out var targetInfo2) && 
                    targetInfo2.Dependencies != null)
                {
                    // 只检查直接依赖，不检查传递依赖
                    bool hasDirectDependency = targetInfo2.Dependencies.Contains(currentPackage);
                    return !hasDirectDependency;
                }
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 【废弃方法】：已由PrecomputeAllDependenciesFlat和扁平化依赖关系替代
        /// </summary>
        [Obsolete("Use _flatDependencies for O(1) lookup instead")]
        private bool IsInDependencyChain(PackageInfo currentPackage, string targetPackage)
        {
            // 兼容性保留，但建议使用_flatDependencies
            if (_flatDependencies.TryGetValue(currentPackage.Name, out var dependencies))
            {
                return dependencies.Contains(targetPackage);
            }
            
            // 回退到原有逻辑
            return currentPackage.AllDependencies.Contains(targetPackage);
        }

        /// <summary>
        /// 【性能优化】：预计算所有包的完整依赖集合，使用扁平化HashSet实现O(1)查找
        /// </summary>
        private void PrecomputeAllDependenciesFlat()
        {
            try
            {
                // 先初始化所有包的依赖集合
                foreach (var packageName in _packageInfos.Keys)
                {
                    _flatDependencies[packageName] = new HashSet<string>();
                }
                
                // 使用拓扑排序计算传递依赖（不检测循环依赖）
                var computed = new HashSet<string>();
                var computing = new HashSet<string>();
                
                foreach (var packageName in _packageInfos.Keys)
                {
                    ComputePackageDependenciesFlatSafe(packageName, computed, computing);
                }
            }
            catch (Exception ex)
            {
                // 【改进错误处理】：记录错误但继续执行
                LogToFile($"PrecomputeAllDependenciesFlat failed: {ex.Message}");
#if DEBUG
                // Debug模式下输出更详细信息
                LogToFile($"Stack trace: {ex.StackTrace}");
#endif
            }
        }

        /// <summary>
        /// 递归计算单个包的所有依赖（安全版本，忽略循环依赖）
        /// </summary>
        private void ComputePackageDependenciesFlatSafe(string packageName, HashSet<string> computed, HashSet<string> computing)
        {
            if (computed.Contains(packageName) || !_packageInfos.TryGetValue(packageName, out var packageInfo))
                return;
            
            if (computing.Contains(packageName))
            {
                // 静默跳过循环依赖，不影响性能计算
                return;
            }
            
            computing.Add(packageName);
            
            var dependencies = _flatDependencies[packageName];
            
            foreach (var dependency in packageInfo.Dependencies)
            {
                // 递归计算依赖的依赖
                ComputePackageDependenciesFlatSafe(dependency, computed, computing);
                
                // 添加直接依赖
                dependencies.Add(dependency);
                
                // 添加传递依赖
                if (_flatDependencies.TryGetValue(dependency, out var transitiveDeps))
                {
                    dependencies.UnionWith(transitiveDeps);
                }
            }
            
            computing.Remove(packageName);
            computed.Add(packageName);
        }


        /// <summary>
        /// 【循环依赖检测】：检测所有包的循环依赖
        /// </summary>
        private void DetectCircularDependencies(Dictionary<string, PackageInfo> packageInfos, HashSet<string> circularPackages)
        {
            try
            {
                var computed = new HashSet<string>();
                var circularPaths = new Dictionary<string, List<string>>();
                
                foreach (var packageName in packageInfos.Keys)
                {
                    var path = new List<string>();
                    DetectCircularDependenciesRecursive(packageName, packageInfos, computed, path, circularPaths);
                }
                
                // 将检测到的循环依赖路径信息添加到circularPackages
                foreach (var kvp in circularPaths)
                {
                    circularPackages.Add(kvp.Key);
                    // 将循环路径信息存储到PackageInfo中，以便后续报告时使用
                    if (packageInfos.TryGetValue(kvp.Key, out var packageInfo))
                    {
                        packageInfo.CircularPath = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"DetectCircularDependencies error: {ex.Message}");
            }
        }

        /// <summary>
        /// 【循环依赖检测】：递归检测单个包的循环依赖
        /// </summary>
        private void DetectCircularDependenciesRecursive(string packageName, Dictionary<string, PackageInfo> packageInfos, 
            HashSet<string> computed, List<string> currentPath, Dictionary<string, List<string>> circularPaths)
        {
            if (computed.Contains(packageName) || !packageInfos.TryGetValue(packageName, out var packageInfo))
                return;
            
            // 检查当前路径中是否已经包含此包（形成循环）
            var cycleStartIndex = currentPath.IndexOf(packageName);
            if (cycleStartIndex >= 0)
            {
                // 发现循环依赖，提取循环路径
                var cyclePath = new List<string>();
                for (int i = cycleStartIndex; i < currentPath.Count; i++)
                {
                    cyclePath.Add(currentPath[i]);
                }
                cyclePath.Add(packageName); // 添加回到起点的包名，形成完整循环
                
                circularPaths[packageName] = cyclePath;
                LogToFile($"DETECTED: Circular dependency path: {string.Join(" -> ", cyclePath)}");
                return;
            }
            
            currentPath.Add(packageName);
            
            foreach (var dependency in packageInfo.Dependencies)
            {
                DetectCircularDependenciesRecursive(dependency, packageInfos, computed, currentPath, circularPaths);
            }
            
            currentPath.RemoveAt(currentPath.Count - 1);
            computed.Add(packageName);
        }

        private class PackageInfo
        {
            public string Name { get; set; } = string.Empty;
            public List<string> Dependencies { get; set; } = new List<string>();
            public int Level { get; set; } = 0;
            public bool AllowSameLevelAccess { get; set; } = false;
            public bool AllowAnyPackageAccess { get; set; } = false;
            
            /// <summary>
            /// 预计算的完整依赖集合（包括传递依赖）
            /// </summary>
            public HashSet<string> AllDependencies { get; set; } = new HashSet<string>();
            
            /// <summary>
            /// 循环依赖路径（如果存在循环依赖）
            /// </summary>
            public List<string>? CircularPath { get; set; }
        }
    }
}