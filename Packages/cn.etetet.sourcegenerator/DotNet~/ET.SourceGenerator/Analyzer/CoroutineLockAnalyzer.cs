#nullable disable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET
{
    /// <summary>
    /// 检查禁止使用 using 语句和手动调用 Dispose 的分析器
    ///
    /// CoroutineLock 是一个自管理的锁，会在超时时自动释放。
    /// 如果使用 using 语句或手动调用 Dispose，可能导致重复释放异常。
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CoroutineLockAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                CoroutineLockUsingAnalyzerRule.Rule,
                CoroutineLockDisposeAnalyzerRule.Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLocalDeclarationStatement, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        /// <summary>
        /// 检查 using 语句
        /// </summary>
        private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax)context.Node;

            // 检查 using 声明的变量类型
            if (usingStatement.Declaration != null)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(usingStatement.Declaration.Type);
                if (IsCoroutineLock(typeInfo.Type))
                {
                    var diagnostic = Diagnostic.Create(
                        CoroutineLockUsingAnalyzerRule.Rule,
                        usingStatement.UsingKeyword.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // 检查 using 的资源表达式
            if (usingStatement.Expression != null)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(usingStatement.Expression);
                if (IsCoroutineLock(typeInfo.Type))
                {
                    var diagnostic = Diagnostic.Create(
                        CoroutineLockUsingAnalyzerRule.Rule,
                        usingStatement.UsingKeyword.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 检查 using 声明（C# 8.0+ 中的 using variable 语法）
        /// 例如：using CoroutineLock lockEntity = ...;
        /// </summary>
        private void AnalyzeLocalDeclarationStatement(SyntaxNodeAnalysisContext context)
        {
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

            // 检查是否有 using 修饰符
            if (!localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
            {
                return;
            }

            // 检查声明的变量类型
            var typeInfo = context.SemanticModel.GetTypeInfo(localDeclaration.Declaration.Type);
            if (IsCoroutineLock(typeInfo.Type))
            {
                var diagnostic = Diagnostic.Create(
                    CoroutineLockUsingAnalyzerRule.Rule,
                    localDeclaration.UsingKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 检查方法调用，特别是 Dispose 方法
        /// </summary>
        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // 检查是否是 Dispose 方法调用
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null || memberAccess.Name.Identifier.ValueText != "Dispose")
            {
                return;
            }

            // 获取被调用对象的类型
            var memberType = context.SemanticModel.GetTypeInfo(memberAccess.Expression);

            if (IsCoroutineLock(memberType.Type))
            {
                var diagnostic = Diagnostic.Create(
                    CoroutineLockDisposeAnalyzerRule.Rule,
                    invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 判断是否是 CoroutineLock 类型
        /// </summary>
        private bool IsCoroutineLock(ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            // 检查类型名称和命名空间
            return typeSymbol.Name == "CoroutineLock" &&
                   (typeSymbol.ContainingNamespace?.ToDisplayString() == "ET" ||
                    typeSymbol.ContainingNamespace?.ToDisplayString() == "ET.Core");
        }
    }
}
