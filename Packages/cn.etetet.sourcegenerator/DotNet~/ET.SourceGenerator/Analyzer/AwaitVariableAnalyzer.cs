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
        private static readonly DiagnosticDescriptor Rule = new(DiagnosticIds.ETAwaitEntityAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Hotfix, DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
                return;

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(analysisContext =>
            {
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                    AnalyzeAwaitUsage(analysisContext);
            }, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeAwaitUsage(SyntaxNodeAnalysisContext context)
        {
            var awaitExpr = (AwaitExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (IsAwaitedMethodHasSkipAttribute(semanticModel, awaitExpr))
                return;

            var variablesBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesAssignedAfterAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesAfterAssignment = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            var method = awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return;

            if (HasSkipAwaitEntityCheckAttribute(semanticModel.GetDeclaredSymbol(method)))
                return;

            foreach (var p in method.ParameterList.Parameters)
            {
                var ps = semanticModel.GetDeclaredSymbol(p);
                if (ps is { } && IsEntityOrSubclass(ps.Type))
                    variablesBeforeAwait.Add(ps);
            }

            foreach (var block in awaitExpr.Ancestors().OfType<BlockSyntax>())
            {
                foreach (var stmt in block.Statements)
                {
                    if (stmt.Span.End >= awaitExpr.SpanStart)
                        break;

                    if (stmt is LocalDeclarationStatementSyntax decl)
                    {
                        foreach (var v in decl.Declaration.Variables)
                        {
                            var symbol = semanticModel.GetDeclaredSymbol(v);
                            if (symbol is ILocalSymbol ls && IsEntityOrSubclass(ls.Type))
                                variablesBeforeAwait.Add(ls);
                        }
                    }
                }
            }

            if (awaitExpr.Parent is AssignmentExpressionSyntax assign)
            {
                var leftSymbol = semanticModel.GetSymbolInfo(assign.Left).Symbol;
                if (leftSymbol != null)
                    variablesFromAwait.Add(leftSymbol);
            }
            else if (awaitExpr.Parent is VariableDeclaratorSyntax declVar)
            {
                var sym = semanticModel.GetDeclaredSymbol(declVar);
                if (sym != null)
                    variablesFromAwait.Add(sym);
            }

            var methodBlock = awaitExpr.FirstAncestorOrSelf<BlockSyntax>();
            if (methodBlock != null)
            {
                AnalyzeStatementsAfterAwait(awaitExpr, methodBlock, variablesBeforeAwait, variablesFromAwait, variablesAssignedAfterAwait, variablesAfterAssignment, context);
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
            var semanticModel = context.SemanticModel;
            var statement = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();
            if (statement == null)
                return;

            var statements = block.Statements;
            int index = statements.IndexOf(statement);
            if (index == -1)
                return;

            for (int i = index + 1; i < statements.Count; i++)
            {
                var stmt = statements[i];

                if (stmt is WhileStatementSyntax ws)
                {
                    AnalyzeLoopBody(ws.Statement);
                    continue;
                }
                if (stmt is ForStatementSyntax fs)
                {
                    AnalyzeLoopBody(fs.Statement);
                    continue;
                }
                if (stmt is DoStatementSyntax ds)
                {
                    AnalyzeLoopBody(ds.Statement);
                    continue;
                }
                if (stmt is ForEachStatementSyntax fes)
                {
                    AnalyzeLoopBody(fes.Statement);
                    continue;
                }

                AnalyzeStatement(stmt);
            }

            void AnalyzeLoopBody(StatementSyntax body)
            {
                foreach (var innerAwait in body.DescendantNodes().OfType<AwaitExpressionSyntax>())
                {
                    var innerBlock = innerAwait.FirstAncestorOrSelf<BlockSyntax>();
                    if (innerBlock != null)
                    {
                        AnalyzeStatementsAfterAwait(innerAwait, innerBlock,
                            variablesBeforeAwait,
                            variablesFromAwait,
                            new HashSet<ISymbol>(SymbolEqualityComparer.Default),
                            new HashSet<ISymbol>(SymbolEqualityComparer.Default),
                            context);
                    }
                }

                if (!body.DescendantNodes().OfType<AwaitExpressionSyntax>().Any())
                    AnalyzeStatement(body);
            }

            void AnalyzeStatement(StatementSyntax stmt)
            {
                foreach (var assign in stmt.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var sym = semanticModel.GetSymbolInfo(assign.Left).Symbol;
                    var rhsType = semanticModel.GetTypeInfo(assign.Right).Type;
                    if (sym != null && variablesBeforeAwait.Contains(sym))
                    {
                        variablesAssignedAfterAwait.Add(sym);
                        if (!IsEntityOrSubclass(rhsType))
                            variablesAfterAssignment.Add(sym);
                    }
                }

                foreach (var id in stmt.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(id).Symbol;
                    if (symbol != null &&
                        variablesBeforeAwait.Contains(symbol) &&
                        !variablesFromAwait.Contains(symbol) &&
                        !variablesAssignedAfterAwait.Contains(symbol) &&
                        !variablesAfterAssignment.Contains(symbol) &&
                        !IsInUsingScope(context, symbol, awaitExpr)) // 使用语法作用域判断
                    {
                        var diag = Diagnostic.Create(Rule, id.GetLocation(), id.Identifier.Text);
                        context.ReportDiagnostic(diag);
                    }
                }
            }
        }

        private static bool IsInUsingScope(SyntaxNodeAnalysisContext context, ISymbol symbol, AwaitExpressionSyntax awaitExpr)
        {
            var semanticModel = context.SemanticModel;
            var declaringSyntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (declaringSyntax == null)
                return false;

            var usingDeclStmt = declaringSyntax.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
            if (usingDeclStmt == null || !usingDeclStmt.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                return false;

            var declaringBlock = usingDeclStmt.FirstAncestorOrSelf<BlockSyntax>();
            if (declaringBlock == null)
                return false;

            var awaitBlock = awaitExpr.FirstAncestorOrSelf<BlockSyntax>();
            if (awaitBlock == null)
                return false;

            return declaringBlock.Span.Contains(awaitExpr.SpanStart) || IsBlockOrNestedIn(awaitBlock, declaringBlock);
        }

        private static bool IsBlockOrNestedIn(BlockSyntax block, BlockSyntax possibleParentBlock)
        {
            if (block == possibleParentBlock)
                return true;

            var parentBlock = block.Parent?.FirstAncestorOrSelf<BlockSyntax>();
            if (parentBlock == null)
                return false;

            return IsBlockOrNestedIn(parentBlock, possibleParentBlock);
        }

        private static bool HasSkipAwaitEntityCheckAttribute(IMethodSymbol? methodSymbol)
        {
            if (methodSymbol == null)
                return false;

            foreach (var attr in methodSymbol.GetAttributes())
            {
                if (attr.AttributeClass == null)
                    continue;

                var fullName = attr.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (fullName.EndsWith(".SkipAwaitEntityCheck") || fullName.EndsWith(".SkipAwaitEntityCheckAttribute"))
                    return true;
            }

            return false;
        }

        private static bool IsAwaitedMethodHasSkipAttribute(SemanticModel semanticModel, AwaitExpressionSyntax awaitExpr)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(awaitExpr.Expression);
            ISymbol? symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (symbol == null)
                return false;

            if (symbol is IMethodSymbol methodSymbol)
                return HasSkipAwaitEntityCheckAttribute(methodSymbol);

            if (symbol is IPropertySymbol propertySymbol)
                return propertySymbol.GetMethod != null && HasSkipAwaitEntityCheckAttribute(propertySymbol.GetMethod);

            return false;
        }

        private static bool IsEntityOrSubclass(ITypeSymbol? type)
        {
            while (type != null)
            {
                if (type.Name == "Entity")
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}
