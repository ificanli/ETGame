#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    public static class SymbolExtensions
    {
        public static ITypeSymbol GetSymbolTypeSafe(this ISymbol symbol)
        {
            return symbol switch
            {
                ILocalSymbol ls => ls.Type,
                IFieldSymbol fs => fs.Type,
                IPropertySymbol ps => ps.Type,
                IParameterSymbol ps => ps.Type,
                _ => null
            };
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitEntityAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new(
            "ETAE001",
            "禁止在 await 之后访问 Entity 及其子类变量",
            "变量 '{0}' 是 Entity 或其子类类型，不允许在 await 之后访问。请使用EntityRef包装传递!",
            "ET.Hotfix",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx =>
            {
                var awaitExpr = (AwaitExpressionSyntax)ctx.Node;
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(ctx.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                    AnalyzeAwaitUsage(ctx, awaitExpr);
            }, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeAwaitUsage(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax awaitExpr)
        {
            var semanticModel = context.SemanticModel;

            if (awaitExpr.FirstAncestorOrSelf<LocalFunctionStatementSyntax>() != null ||
                awaitExpr.FirstAncestorOrSelf<LambdaExpressionSyntax>() != null ||
                awaitExpr.FirstAncestorOrSelf<AnonymousMethodExpressionSyntax>() != null)
                return;

            if (IsAwaitedMethodHasSkipAttribute(semanticModel, awaitExpr))
                return;

            var method = awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null)
                return;

            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null)
                return;

            var variablesBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var variablesFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var loopDeclaredVariables = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var preAwaitIgnoreSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var p in method.ParameterList.Parameters)
            {
                var ps = semanticModel.GetDeclaredSymbol(p);
                if (ps != null && IsEntityOrSubclass(ps.Type))
                    variablesBeforeAwait.Add(ps);
            }

            // ✅ 修复 Test19：豁免 foreach 表达式对 unit 的访问
            if (awaitExpr.FirstAncestorOrSelf<ForEachStatementSyntax>() is { } foreachStmt)
            {
                foreach (var id in foreachStmt.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                {
                    var sym = semanticModel.GetSymbolInfo(id).Symbol;
                    if (sym is { } && IsEntityOrSubclass(sym.GetSymbolTypeSafe()))
                    {
                        preAwaitIgnoreSymbols.Add(sym);
                    }
                }
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

                    if (stmt is ForEachStatementSyntax foreachStmt2)
                    {
                        foreach (var id in foreachStmt2.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                        {
                            var sym = semanticModel.GetSymbolInfo(id).Symbol;
                            if (sym is { } && IsEntityOrSubclass(sym.GetSymbolTypeSafe()))
                                preAwaitIgnoreSymbols.Add(sym);
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

            var awaitStatement = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();
            if (awaitStatement == null)
                return;

            var impactedStatements = GetStatementsImpactedByAwait(awaitStatement, semanticModel, loopDeclaredVariables);
            var assigned = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var stmt in impactedStatements)
            {
                foreach (var assignExpr in stmt.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var target = semanticModel.GetSymbolInfo(assignExpr.Left).Symbol;
                    var rhsType = semanticModel.GetTypeInfo(assignExpr.Right).Type;

                    if (target != null && variablesBeforeAwait.Contains(target))
                    {
                        assigned.Add(target);
                        if (!IsEntityOrSubclass(rhsType))
                            variablesFromAwait.Add(target);
                    }
                }

                foreach (var id in stmt.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(id).Symbol;
                    if (symbol != null &&
                        variablesBeforeAwait.Contains(symbol) &&
                        !variablesFromAwait.Contains(symbol) &&
                        !assigned.Contains(symbol) &&
                        !loopDeclaredVariables.Contains(symbol) &&
                        !preAwaitIgnoreSymbols.Contains(symbol) &&
                        !IsInUsingScope(context, symbol, awaitExpr))
                    {
                        var diag = Diagnostic.Create(Rule, id.GetLocation(), id.Identifier.Text);
                        context.ReportDiagnostic(diag);
                    }
                }
            }
        }

        private static List<StatementSyntax> GetStatementsImpactedByAwait(
            StatementSyntax awaitStatement,
            SemanticModel semanticModel,
            HashSet<ISymbol> loopDeclaredVars)
        {
            var impacted = new List<StatementSyntax>();

            if (awaitStatement.Parent is BlockSyntax block)
            {
                var idx = block.Statements.IndexOf(awaitStatement);
                for (int i = idx + 1; i < block.Statements.Count; i++)
                    impacted.Add(block.Statements[i]);
            }

            SyntaxNode parent = awaitStatement;
            while ((parent = parent.Parent) != null)
            {
                if (parent is IfStatementSyntax ifStmt && parent.Parent is BlockSyntax outer)
                {
                    bool ifHasAwait = ContainsAwait(ifStmt.Statement);
                    bool elseHasAwait = ContainsAwait(ifStmt.Else?.Statement);
                    bool ifTerminates = Terminates(ifStmt.Statement);
                    bool elseTerminates = ifStmt.Else != null && Terminates(ifStmt.Else.Statement);

                    if ((ifHasAwait && !ifTerminates) || (elseHasAwait && !elseTerminates))
                    {
                        int idx = outer.Statements.IndexOf(ifStmt);
                        for (int i = idx + 1; i < outer.Statements.Count; i++)
                            impacted.Add(outer.Statements[i]);
                    }
                }
                else if (parent is StatementSyntax stmt &&
                         ContainsAwait(stmt) &&
                         (stmt is WhileStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or DoStatementSyntax))
                {
                    var loopStatements = GetAllStatementsIn(stmt);
                    impacted.AddRange(loopStatements);

                    foreach (var decl in stmt.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(decl);
                        if (symbol is ILocalSymbol local && IsEntityOrSubclass(local.Type))
                            loopDeclaredVars.Add(local);
                    }
                }
            }

            return impacted;
        }

        private static List<StatementSyntax> GetAllStatementsIn(StatementSyntax stmt)
        {
            return stmt is BlockSyntax block ? block.Statements.ToList() : new List<StatementSyntax> { stmt };
        }

        private static bool ContainsAwait(StatementSyntax stmt)
        {
            return stmt?.DescendantNodes().OfType<AwaitExpressionSyntax>().Any() == true;
        }

        private static bool Terminates(StatementSyntax stmt)
        {
            if (stmt == null) return false;
            if (stmt is BlockSyntax block && block.Statements.Any())
                stmt = block.Statements.Last();

            return stmt is ReturnStatementSyntax or ThrowStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax;
        }

        private static bool IsInUsingScope(SyntaxNodeAnalysisContext context, ISymbol symbol, AwaitExpressionSyntax awaitExpr)
        {
            var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (syntax == null) return false;

            var usingDecl = syntax.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
            if (usingDecl == null || !usingDecl.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                return false;

            var block = usingDecl.FirstAncestorOrSelf<BlockSyntax>();
            var awaitBlock = awaitExpr.FirstAncestorOrSelf<BlockSyntax>();
            if (block == null || awaitBlock == null) return false;

            return block.Span.Contains(awaitExpr.SpanStart);
        }

        private static bool IsEntityOrSubclass(ITypeSymbol type)
        {
            while (type != null)
            {
                if (type.Name == "Entity")
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        private static bool HasSkipAwaitEntityCheckAttribute(IMethodSymbol method)
        {
            return method.GetAttributes().Any(attr =>
            {
                var name = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return name?.EndsWith(".SkipAwaitEntityCheck") == true || name?.EndsWith(".SkipAwaitEntityCheckAttribute") == true;
            });
        }

        private static bool IsAwaitedMethodHasSkipAttribute(SemanticModel model, AwaitExpressionSyntax expr)
        {
            var symbol = model.GetSymbolInfo(expr.Expression).Symbol ??
                         model.GetSymbolInfo(expr.Expression).CandidateSymbols.FirstOrDefault();

            return symbol switch
            {
                IMethodSymbol ms => HasSkipAwaitEntityCheckAttribute(ms),
                IPropertySymbol ps => ps.GetMethod is { } g && HasSkipAwaitEntityCheckAttribute(g),
                _ => false
            };
        }
    }
}
