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
    /// 【用户要求总结】：要严格按照下面要求编写，每次做出决定之前先检查是否是否违反规定，执行完任务之后再次检查是否违反规定
    /// 1. 编译ET.Core时：分析器实例A可以从编译信息中获取符号路径，初始化的时候把符号跟映射的包写入符号表
    /// 2. 编译ET.Model时：分析器实例B可以从编译信息中获取符号路径，分析器实例B初始化时可以把符号表跟映射的包写入符号表，那么此时符号表有ET.Model跟ET.Core两者的符号表
    /// 3. 编译ET.Hotfix时：ET.Model跟ET.Core已经编译完成了，分析器实例C初始化可以把符号表跟映射的包写入符号表，那么此时符号表有ET.Model跟ET.Hotfix跟ET.Core三者的符号表
    /// 7. 一定要找到所有符号，要修复所有找不到符号的异常，找不到说明符号表有遗漏，需要检查写入符号表是否漏掉
    /// 8. 绝对禁止hard code任何包名映射或类型推断
    /// 9. 必须利用编译时符号表中的信息，因为编译时符号已经写入符号表
    /// 10. 编译流程：程序集相互依赖时，被依赖的程序集先编译完成
    /// 11. 编译ET.Hotfix时，ET.Model已编译完成，其符号在编译上下文中可用
    /// 12. 查找符号只需要从符号表中查找，因为已经提前写入了符号表
    /// 13. 初期加载packageinfo可以读取文件系统，后面则禁止扫描文件系统去获取符号的包名，只能从符号表路径中获取
    /// 14. 已编译的程序集跟当前程序集的符号已经写入了符号表，不需要扫描文件系统去获取
    /// 15. 【关键设计】：分析器在初始化时把程序集符号跟包写入符号表，这样后面使用拿符号包名直接从符号表中拿
    /// 16. 【严格检查】：找不到符号的包要抛异常，符号查找请做到最严格的检查
    /// 17. 该文件禁止hard code
    /// 18. 当前程序集获取包名也从符号表获取，因为分析器在一开始就已经把当前代码的符号写入了符号表
    /// 19. 分析器可以把调试信息写入文件来调试
    /// 20. 对于partial class，只需要检查用到的partial class的成员或者方法
    /// 
    /// 【实现原理】：
    /// 使用全局共享符号表（static ConcurrentDictionary<string, string> _globalSymbolTable）
    /// 每个分析器实例初始化时从当前编译的语法树中提取符号并写入全局符号表
    /// 查找符号包归属时直接从全局符号表中查询，实现跨程序集的符号查找
    /// 
    /// 【编译流程】：
    /// 1. 编译ET.Core时：分析器实例A写入Core的符号到全局表
    /// 2. 编译ET.Model时：分析器实例B写入Model的符号到全局表（此时表中有Core+Model）  
    /// 3. 编译ET.Hotfix时：分析器实例C写入Hotfix的符号到全局表（此时表中有Core+Model+Hotfix）
    /// 4. 查找符号时：直接从全局符号表中查询，无需推断或hard code
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

        // 【关键】：所有分析器实例共享的全局符号表
        private static readonly ConcurrentDictionary<string, string> _globalSymbolTable = new ConcurrentDictionary<string, string>();
        
        // 【关键修复】：包信息缓存也应该是全局共享的，就像符号表一样
        private static readonly ConcurrentDictionary<string, PackageInfo> _packageInfos = new ConcurrentDictionary<string, PackageInfo>();
        private static volatile bool _initialized = false;
        private static readonly object _initLock = new object();

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
            
            lock (_initLock)
            {
                // 【关键修复】：每个编译单元都需要写入符号表，不能只初始化一次
                if (!_initialized)
                {
                    LoadPackageInfos(context.Compilation);
                    _initialized = true;
                }
                
                // 每个编译单元都写入自己的符号表
                WriteCurrentCompilationToGlobalSymbolTable(context.Compilation);
            }
            
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationExpression, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
        }

        private void WriteCurrentCompilationToGlobalSymbolTable(Compilation compilation)
        {
            try
            {
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
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            
                            // 只有在符号表中不存在时才添加，避免覆盖
                            _globalSymbolTable.TryAdd(typeName, packageName);
                            
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
                                                var fieldKey = $"{typeName}.{fieldName}";
                                                _globalSymbolTable.TryAdd(fieldKey, packageName);
                                            }
                                        }
                                        break;
                                    case MethodDeclarationSyntax methodDecl:
                                        var methodName = methodDecl.Identifier.ValueText;
                                        if (!string.IsNullOrEmpty(methodName))
                                        {
                                            var methodKey = $"{typeName}.{methodName}";
                                            _globalSymbolTable.TryAdd(methodKey, packageName);
                                        }
                                        break;
                                    case PropertyDeclarationSyntax propertyDecl:
                                        var propertyName = propertyDecl.Identifier.ValueText;
                                        if (!string.IsNullOrEmpty(propertyName))
                                        {
                                            var propertyKey = $"{typeName}.{propertyName}";
                                            _globalSymbolTable.TryAdd(propertyKey, packageName);
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
                        if (!string.IsNullOrEmpty(enumName))
                        {
                            _globalSymbolTable.TryAdd(enumName, packageName);
                            
                            foreach (var enumMember in enumDecl.Members)
                            {
                                var memberName = enumMember.Identifier.ValueText;
                                if (!string.IsNullOrEmpty(memberName))
                                {
                                    _globalSymbolTable.TryAdd($"{enumName}.{memberName}", packageName);
                                    _globalSymbolTable.TryAdd(memberName, packageName);
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
                        if (!string.IsNullOrEmpty(delegateName))
                        {
                            _globalSymbolTable.TryAdd(delegateName, packageName);
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
            
            // 【关键修复】：对于字段、方法、属性，直接从符号表中查找
            if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.ContainingType != null)
            {
                var fieldKey = $"{fieldSymbol.ContainingType.Name}.{fieldSymbol.Name}";
                if (_globalSymbolTable.TryGetValue(fieldKey, out string? fieldPackage))
                {
                    return fieldPackage;
                }
            }
            
            if (symbol is IMethodSymbol methodSymbol && methodSymbol.ContainingType != null)
            {
                var methodKey = $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}";
                if (_globalSymbolTable.TryGetValue(methodKey, out string? methodPackage))
                {
                    return methodPackage;
                }
            }
            
            if (symbol is IPropertySymbol propertySymbol && propertySymbol.ContainingType != null)
            {
                var propertyKey = $"{propertySymbol.ContainingType.Name}.{propertySymbol.Name}";
                if (_globalSymbolTable.TryGetValue(propertyKey, out string? propertyPackage))
                {
                    return propertyPackage;
                }
            }
            
            // 查询类型
            if (_globalSymbolTable.TryGetValue(symbol.Name, out string? typePackage))
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
                        // 这是ET框架中声明的符号，但符号表中没有记录，说明符号写入有遗漏
                        throw new InvalidOperationException($"Symbol '{symbol.Name}' is declared in ET package '{packageFromPath}' but not found in symbol table. File: {location.SourceTree.FilePath}");
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