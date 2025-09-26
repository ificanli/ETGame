using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ETTaskAnalyzer:DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(ETTaskInSyncMethodAnalyzerRule.Rule,ETTaskInAsyncMethodAnalyzerRule.Rule,AsyncMethodReturnTypeAnalyzerRule.Rule);
        
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
                    analysisContext.RegisterSemanticModelAction((this.AnalyzeSemanticModel));
                }
            } ));
            // 检测async void方法和Lambda
            context.RegisterSyntaxNodeAction(this.AnalyzeAsyncVoidMethod, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
            context.RegisterSyntaxNodeAction(this.AnalyzeAsyncVoidLambda, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression);
        }
        
        private void AnalyzeSemanticModel(SemanticModelAnalysisContext analysisContext)
        {
            foreach (var memberAccessExpressionSyntax in analysisContext.SemanticModel.SyntaxTree.GetRoot().DescendantNodes<MemberAccessExpressionSyntax>())
            {
                AnalyzeMemberAccessExpression(analysisContext, memberAccessExpressionSyntax);
            }
        }

        private void AnalyzeMemberAccessExpression(SemanticModelAnalysisContext context, MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            // 获取方法调用Syntax 对应的methodSymbol
            if (!(memberAccessExpressionSyntax?.Parent is InvocationExpressionSyntax invocationExpressionSyntax) ||
                !(context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            //忽略void返回值函数
            if (methodSymbol.ReturnsVoid)
            {
                return;
            }
            
            if (!(methodSymbol.ReturnType is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }
            
            // 筛选出返回值为ETTask 和ETTask<T>的函数
            if (namedTypeSymbol.Name!=Definition.ETTask)
            {
                return;
            }
            
            // 获取ETTask函数调用处所在的函数体
            var containingMethodDeclarationSyntax = memberAccessExpressionSyntax?.GetNeareastAncestor<MethodDeclarationSyntax>();
            if (containingMethodDeclarationSyntax==null)
            {
                return;
            }
            
            IMethodSymbol? containingMethodSymbol = context.SemanticModel.GetDeclaredSymbol(containingMethodDeclarationSyntax);
            if (containingMethodSymbol==null)
            {
                return;
            }
            
            // ETTask函数在Lambda表达式中时
            var lambdaExpression = invocationExpressionSyntax.GetNeareastAncestor<LambdaExpressionSyntax>();
            if (lambdaExpression != null)
            {
                // 检查是否是async lambda
                bool isAsyncLambda = false;
                if (lambdaExpression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                {
                    isAsyncLambda = parenthesizedLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
                }
                else if (lambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    isAsyncLambda = simpleLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
                }

                // 对于async lambda中的ETTask调用，检查是否有await
                if (isAsyncLambda)
                {
                    // 检查当前调用是否被await修饰
                    var lambdaAwaitExpression = invocationExpressionSyntax.GetNeareastAncestor<AwaitExpressionSyntax>();
                    if (lambdaAwaitExpression == null)
                    {
                        // 检查是否调用了.NoContext()或WithContext()
                        if (!IsValidETTaskCall(invocationExpressionSyntax))
                        {
                            Diagnostic diagnostic = Diagnostic.Create(ETTaskInAsyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                                memberAccessExpressionSyntax?.Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                else
                {
                    // 同步lambda中的ETTask调用，需要.NoContext()或WithContext()
                    if (!IsValidETTaskCall(invocationExpressionSyntax))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(ETTaskInSyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                            memberAccessExpressionSyntax?.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                return;
            }
            
            
            // 检查普通方法中的ETTask调用
            // 先检查是否被await修饰
            var awaitExpression = invocationExpressionSyntax.GetNeareastAncestor<AwaitExpressionSyntax>();
            if (awaitExpression != null)
            {
                // 已经有await，是正确的用法
                return;
            }

            // 检查是否有正确的后缀调用
            if (IsValidETTaskCall(invocationExpressionSyntax))
            {
                // 已经有.NoContext()或WithContext()，是正确的用法
                return;
            }

            // 方法体内ETTask单独调用时
            if (invocationExpressionSyntax.Parent is ExpressionStatementSyntax)
            {
                if (containingMethodSymbol.IsAsync)
                {
                    Diagnostic diagnostic = Diagnostic.Create(ETTaskInAsyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                        memberAccessExpressionSyntax?.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    Diagnostic diagnostic = Diagnostic.Create(ETTaskInSyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                        memberAccessExpressionSyntax?.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 检查ETTask调用是否已经正确处理（有.NoContext()或WithContext()后缀）
        /// </summary>
        private static bool IsValidETTaskCall(InvocationExpressionSyntax invocationExpression)
        {
            // 检查父级是否是成员访问表达式（如 .NoContext() 或 .WithContext()）
            if (invocationExpression.Parent is MemberAccessExpressionSyntax parentMemberAccess)
            {
                var memberName = parentMemberAccess.Name.Identifier.ValueText;
                if (memberName == "NoContext" || memberName == "WithContext")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检测async void方法
        /// </summary>
        private void AnalyzeAsyncVoidMethod(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }

            IMethodSymbol? methodSymbol = null;

            if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
            }
            else if (context.Node is LocalFunctionStatementSyntax localFunctionStatementSyntax)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunctionStatementSyntax) as IMethodSymbol;
            }

            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.IsAsync && methodSymbol.ReturnsVoid)
            {
                Diagnostic diagnostic = Diagnostic.Create(AsyncMethodReturnTypeAnalyzerRule.Rule, context.Node.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 检测async void Lambda表达式
        /// </summary>
        private void AnalyzeAsyncVoidLambda(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }

            bool isAsync = false;
            if (context.Node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                isAsync = parenthesizedLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
            }
            else if (context.Node is SimpleLambdaExpressionSyntax simpleLambda)
            {
                isAsync = simpleLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
            }

            if (isAsync)
            {
                // 检查lambda表达式的转换目标类型
                var typeInfo = context.SemanticModel.GetTypeInfo(context.Node);
                if (typeInfo.ConvertedType != null)
                {
                    // 检查是否是Action委托（对应async void）
                    if (typeInfo.ConvertedType.Name == "Action" ||
                        (typeInfo.ConvertedType.ContainingNamespace?.Name == "System" && typeInfo.ConvertedType.Name.StartsWith("Action")))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(AsyncMethodReturnTypeAnalyzerRule.Rule, context.Node.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }

                // 检查lambda表达式的符号信息
                var symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    // 检查是否是async void（返回void）
                    if (methodSymbol.ReturnsVoid)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(AsyncMethodReturnTypeAnalyzerRule.Rule, context.Node.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}