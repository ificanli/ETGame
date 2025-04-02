using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitEntityAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "禁止在 await 之后访问 Entity 及其子类变量";

        private const string MessageFormat = "变量 '{0}' 是 Entity 或其子类类型，不允许在 await 之后访问。请使用EntityRef包装传递!";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticIds.ETAwaitEntityAnalyzerRuleId,
            Title,
            MessageFormat,
            DiagnosticCategories.Hotfix,
            DiagnosticSeverity.Error,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(analysisContext =>
            {
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.All))
                {
                    AnalyzeAwaitUsage(analysisContext);
                }
            }, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeAwaitUsage(SyntaxNodeAnalysisContext context)
        {
            AwaitExpressionSyntax awaitExpression = (AwaitExpressionSyntax)context.Node;
            BlockSyntax? containingBlock = awaitExpression.FirstAncestorOrSelf<BlockSyntax>();
            if (containingBlock == null)
            {
                return;
            }

            SemanticModel semanticModel = context.SemanticModel;
            var variablesBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            // 找到方法参数中属于 Entity 或其子类的变量
            MethodDeclarationSyntax? methodDeclaration = awaitExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodDeclaration != null)
            {
                foreach (ParameterSyntax parameter in methodDeclaration.ParameterList.Parameters)
                {
                    IParameterSymbol? symbol = semanticModel.GetDeclaredSymbol(parameter);
                    if (symbol != null && IsEntityOrSubclass(symbol.Type, semanticModel))
                    {
                        variablesBeforeAwait.Add(symbol);
                    }
                }
            }

            // 找到 await 之前声明的所有 Entity 或其子类变量
            foreach (StatementSyntax statement in containingBlock.Statements)
            {
                if (statement.Span.Start >= awaitExpression.Span.Start)
                {
                    break;
                }

                if (statement is LocalDeclarationStatementSyntax localDeclaration)
                {
                    foreach (VariableDeclaratorSyntax variable in localDeclaration.Declaration.Variables)
                    {
                        ILocalSymbol? symbol = semanticModel.GetDeclaredSymbol(variable) as ILocalSymbol;
                        if (symbol != null && IsEntityOrSubclass(symbol.Type, semanticModel))
                        {
                            variablesBeforeAwait.Add(symbol);
                        }
                    }
                }
            }

            // 记录 await 返回的变量
            if (awaitExpression.Parent is AssignmentExpressionSyntax assignment)
            {
                ILocalSymbol? assignedSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol as ILocalSymbol;
                if (assignedSymbol != null)
                {
                    variablesFromAwait.Add(assignedSymbol);
                }
            }
            else if (awaitExpression.Parent is VariableDeclaratorSyntax variableDeclarator)
            {
                ILocalSymbol? declaredSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator) as ILocalSymbol;
                if (declaredSymbol != null)
                {
                    variablesFromAwait.Add(declaredSymbol);
                }
            }

            // 检查 await 之后是否访问了这些 Entity 或其子类变量（排除 await 返回的变量）
            var statementsAfterAwait = containingBlock.Statements.Where(s => s.Span.Start > awaitExpression.Span.End);
            foreach (StatementSyntax? statement in statementsAfterAwait)
            {
                var identifiers = statement.DescendantNodes().OfType<IdentifierNameSyntax>();
                foreach (IdentifierNameSyntax? identifier in identifiers)
                {
                    ISymbol? symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                    if (symbol != null && variablesBeforeAwait.Contains(symbol) && !variablesFromAwait.Contains(symbol))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), identifier.Identifier.Text);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsEntityOrSubclass(ITypeSymbol type, SemanticModel semanticModel)
        {
            while (type != null)
            {
                if (type.Name == "Entity")
                {
                    return true;
                }

                if (type.BaseType == null)
                {
                    break;
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}