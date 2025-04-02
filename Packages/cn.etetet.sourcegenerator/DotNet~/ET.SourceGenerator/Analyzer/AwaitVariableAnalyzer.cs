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
            AwaitExpressionSyntax awaitExpr = (AwaitExpressionSyntax)context.Node;
            SemanticModel semanticModel = context.SemanticModel;

            var variablesBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesAssignedAfterAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesAfterAssignment = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            // 方法参数
            MethodDeclarationSyntax? method = awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method != null)
            {
                foreach (ParameterSyntax p in method.ParameterList.Parameters)
                {
                    IParameterSymbol? symbol = semanticModel.GetDeclaredSymbol(p);
                    if (symbol is IParameterSymbol ps && IsEntityOrSubclass(ps.Type))
                    {
                        variablesBeforeAwait.Add(ps);
                    }
                }
            }

            // 包含此 await 的所有 Block（向上追溯）中声明的 Entity 变量
            var ancestorBlocks = awaitExpr.Ancestors().OfType<BlockSyntax>();
            foreach (BlockSyntax? block in ancestorBlocks)
            {
                foreach (StatementSyntax stmt in block.Statements)
                {
                    if (stmt.Span.End >= awaitExpr.SpanStart)
                    {
                        break;
                    }

                    if (stmt is LocalDeclarationStatementSyntax decl)
                    {
                        foreach (VariableDeclaratorSyntax v in decl.Declaration.Variables)
                        {
                            ISymbol? symbol = semanticModel.GetDeclaredSymbol(v);
                            if (symbol is ILocalSymbol ls && IsEntityOrSubclass(ls.Type))
                            {
                                variablesBeforeAwait.Add(ls);
                            }
                        }
                    }
                }
            }

            // await 的返回变量
            if (awaitExpr.Parent is AssignmentExpressionSyntax assign)
            {
                ISymbol? leftSymbol = semanticModel.GetSymbolInfo(assign.Left).Symbol;
                if (leftSymbol != null)
                {
                    variablesFromAwait.Add(leftSymbol);
                }
            }
            else if (awaitExpr.Parent is VariableDeclaratorSyntax declVar)
            {
                ISymbol? sym = semanticModel.GetDeclaredSymbol(declVar);
                if (sym != null)
                {
                    variablesFromAwait.Add(sym);
                }
            }

            BlockSyntax? currentBlock = awaitExpr.FirstAncestorOrSelf<BlockSyntax>();
            if (currentBlock != null)
            {
                AnalyzeStatementsAfterAwait(awaitExpr,
                    currentBlock,
                    variablesBeforeAwait,
                    variablesFromAwait,
                    variablesAssignedAfterAwait,
                    variablesAfterAssignment,
                    context);
            }
        }

        private static void AnalyzeStatementsAfterAwait(
        AwaitExpressionSyntax awaitExpr,
        BlockSyntax block,
        HashSet<ISymbol> variablesBeforeAwait,
        HashSet<ISymbol> variablesFromAwait,
        HashSet<ISymbol> variablesAssignedAfterAwait,
        HashSet<ISymbol> variablesAfterAssignment,
        SyntaxNodeAnalysisContext context)
        {
            SemanticModel semanticModel = context.SemanticModel;
            StatementSyntax? statement = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
            {
                return;
            }

            var statements = block.Statements;
            int index = statements.IndexOf(statement);
            if (index == -1)
            {
                return;
            }

            for (int i = index + 1; i < statements.Count; i++)
            {
                StatementSyntax stmt = statements[i];

                if (stmt is WhileStatementSyntax ws)
                {
                    AnalyzeLoop(ws.Statement);
                }
                else if (stmt is ForStatementSyntax fs)
                {
                    AnalyzeLoop(fs.Statement);
                }
                else if (stmt is DoStatementSyntax ds)
                {
                    AnalyzeLoop(ds.Statement);
                }
                else if (stmt is ForEachStatementSyntax fes)
                {
                    AnalyzeLoop(fes.Statement);
                }

                foreach (AssignmentExpressionSyntax? assign in stmt.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    ISymbol? sym = semanticModel.GetSymbolInfo(assign.Left).Symbol;
                    ITypeSymbol? rhsType = semanticModel.GetTypeInfo(assign.Right).Type;
                    if (sym != null && variablesBeforeAwait.Contains(sym))
                    {
                        variablesAssignedAfterAwait.Add(sym);
                        if (!IsEntityOrSubclass(rhsType))
                        {
                            variablesAfterAssignment.Add(sym);
                        }
                    }
                }

                foreach (IdentifierNameSyntax? id in stmt.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    ISymbol? symbol = semanticModel.GetSymbolInfo(id).Symbol;
                    if (symbol != null &&
                        variablesBeforeAwait.Contains(symbol) &&
                        !variablesFromAwait.Contains(symbol) &&
                        !variablesAssignedAfterAwait.Contains(symbol) &&
                        !variablesAfterAssignment.Contains(symbol))
                    {
                        Diagnostic diag = Diagnostic.Create(Rule, id.GetLocation(), id.Identifier.Text);
                        context.ReportDiagnostic(diag);
                    }
                }
            }

            void AnalyzeLoop(StatementSyntax body)
            {
                foreach (AwaitExpressionSyntax? innerAwait in body.DescendantNodes().OfType<AwaitExpressionSyntax>())
                {
                    BlockSyntax? innerBlock = innerAwait.FirstAncestorOrSelf<BlockSyntax>();
                    if (innerBlock != null)
                    {
                        AnalyzeStatementsAfterAwait(innerAwait, innerBlock,
                            variablesBeforeAwait,
                            variablesFromAwait,
                            variablesAssignedAfterAwait,
                            variablesAfterAssignment,
                            context);
                    }
                }
            }
        }

        private static bool IsEntityOrSubclass(ITypeSymbol? type)
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