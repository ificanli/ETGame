using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    /// <summary>
    /// 测试用例命名规范分析器
    /// 规则：
    /// 1. 继承ATestHandler的类必须是测试用例
    /// 2. 测试用例类名必须符合格式：{PackageType}_{TestName}_Test
    /// 3. 测试用例必须放在包的Scripts/Hotfix/Test目录
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestCaseNamingAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Test case naming violation";
        private const string MessageFormat = "Test case class '{0}' must follow naming pattern '{{PackageType}}_{{TestName}}_Test' and be placed in 'Scripts/Hotfix/Test' directory: {1}";
        private const string Description = "Test case classes inheriting from ATestHandler must follow naming conventions.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.TestCaseNamingAnalyzerRuleId,
            Title,
            MessageFormat,
            DiagnosticCategories.Hotfix,
            DiagnosticSeverity.Error,
            true,
            Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterCompilationStartAction(analysisContext =>
            {
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllHotfix))
                {
                    analysisContext.RegisterSemanticModelAction(AnalyzeSemanticModel);
                }
            });
        }

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            foreach (var classDeclarationSyntax in context.SemanticModel.SyntaxTree.GetRoot()
                .DescendantNodes<ClassDeclarationSyntax>())
            {
                var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                if (classTypeSymbol != null)
                {
                    AnalyzeTestCase(context, classDeclarationSyntax, classTypeSymbol);
                }
            }
        }

        private void AnalyzeTestCase(SemanticModelAnalysisContext context, ClassDeclarationSyntax classDeclaration, 
            INamedTypeSymbol classSymbol)
        {
            // 检查是否继承自ATestHandler
            if (!InheritsFromATestHandler(classSymbol))
            {
                return;
            }

            var className = classSymbol.Name;
            var filePath = classDeclaration.SyntaxTree.FilePath;

            // 检查文件路径
            if (!IsInCorrectDirectory(filePath, out string errorMessage))
            {
                var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), 
                    className, errorMessage);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // 检查命名格式
            if (!IsValidTestCaseNaming(className, filePath, out errorMessage))
            {
                var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), 
                    className, errorMessage);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool InheritsFromATestHandler(INamedTypeSymbol classSymbol)
        {
            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                // 检查完整类型名
                if (baseType.ToDisplayString() == "ET.Test.ATestHandler")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        private bool IsInCorrectDirectory(string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (string.IsNullOrEmpty(filePath))
            {
                errorMessage = "File path is empty";
                return false;
            }

            // 规范化路径
            var normalizedPath = filePath.Replace("\\", "/");
            
            // 检查是否在Scripts/Hotfix/Test目录
            if (!normalizedPath.Contains("/Scripts/Hotfix/Test/"))
            {
                errorMessage = "Test case must be placed in 'Scripts/Hotfix/Test' directory";
                return false;
            }

            return true;
        }

        private bool IsValidTestCaseNaming(string className, string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 类名必须以_Test结尾
            if (!className.EndsWith("_Test"))
            {
                errorMessage = "Class name must end with '_Test'";
                return false;
            }

            // 提取PackageType部分（类名中_Test之前的第一个部分）
            var parts = className.Split('_');
            if (parts.Length < 3)
            {
                errorMessage = "Class name must follow pattern '{PackageType}_{TestName}_Test' (at least 3 parts separated by '_')";
                return false;
            }

            // parts[0] 应该是PackageType
            // parts[1..^2] 是TestName（可能包含多个下划线）
            // parts[^1] 是"Test"
            var packageTypeName = parts[0];
            
            // 从文件路径中提取包名
            var packageName = ExtractPackageNameFromFilePath(filePath);
            if (string.IsNullOrEmpty(packageName))
            {
                errorMessage = "Cannot extract package name from file path";
                return false;
            }

            // 验证PackageType是否与包名匹配
            // 例如：cn.etetet.test -> Test, cn.etetet.robot -> Robot
            var expectedPackageTypeName = GetExpectedPackageTypeName(packageName);
            
            if (!string.Equals(packageTypeName, expectedPackageTypeName, StringComparison.Ordinal))
            {
                errorMessage = $"PackageType should be '{expectedPackageTypeName}' for package '{packageName}', but got '{packageTypeName}'";
                return false;
            }

            return true;
        }

        private string ExtractPackageNameFromFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return string.Empty;
            }

            var normalizedPath = filePath.Replace("\\", "/");
            
            if (normalizedPath.Contains("/Packages/"))
            {
                var packageStart = normalizedPath.IndexOf("/Packages/") + 10;
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
            
            return string.Empty;
        }

        private string GetExpectedPackageTypeName(string packageName)
        {
            // cn.etetet.test -> Test
            // cn.etetet.robot -> Robot
            // cn.etetet.yiui -> YIUI
            if (string.IsNullOrEmpty(packageName))
            {
                return string.Empty;
            }

            // 提取最后一个部分
            var parts = packageName.Split('.');
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            var lastPart = parts[parts.Length - 1];
            
            // 首字母大写
            if (string.IsNullOrEmpty(lastPart))
            {
                return string.Empty;
            }

            // 特殊处理：yiui相关包
            if (lastPart.StartsWith("yiui"))
            {
                // yiui -> YIUI
                // yiuiframework -> YIUIFramework
                return "YIUI" + (lastPart.Length > 4 ? 
                    char.ToUpper(lastPart[4]) + lastPart.Substring(5) : "");
            }

            // 普通包：首字母大写
            return char.ToUpper(lastPart[0]) + lastPart.Substring(1);
        }
    }
}