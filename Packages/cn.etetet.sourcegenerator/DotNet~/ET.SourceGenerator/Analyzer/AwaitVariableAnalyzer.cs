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
                if (!AnalyzerHelper.IsAssemblyNeedAnalyze(ctx.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                    return;

                if (awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } method)
                    return;

                if (!method.DescendantNodes().OfType<AwaitExpressionSyntax>().Any())
                    return;

                AnalyzeAwaitUsage(ctx, awaitExpr, method);
            }, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeAwaitUsage(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax awaitExpr, MethodDeclarationSyntax method)
        {
            var semanticModel = context.SemanticModel;
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null || HasSkipAwaitEntityCheckAttribute(methodSymbol))
                return;

            if (IsAwaitedMethodHasSkipAttribute(semanticModel, awaitExpr))
                return;

            var varsBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var varsFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var loopDeclared = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var preAwaitIgnore = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var p in method.ParameterList.Parameters)
            {
                var ps = semanticModel.GetDeclaredSymbol(p);
                if (ps is { } && IsEntityOrSubclass(ps.Type))
                    varsBeforeAwait.Add(ps);
            }

            if (awaitExpr.FirstAncestorOrSelf<ForEachStatementSyntax>() is { } foreachStmt)
            {
                foreach (var id in foreachStmt.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                {
                    var sym = semanticModel.GetSymbolInfo(id).Symbol;
                    if (sym != null && IsEntityOrSubclass(sym.GetSymbolTypeSafe()))
                        preAwaitIgnore.Add(sym);
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
                            var sym = semanticModel.GetDeclaredSymbol(v);
                            if (sym is ILocalSymbol ls && IsEntityOrSubclass(ls.Type))
                                varsBeforeAwait.Add(ls);
                        }
                    }

                    if (stmt is ForEachStatementSyntax foreachStmt2)
                    {
                        foreach (var id in foreachStmt2.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                        {
                            var sym = semanticModel.GetSymbolInfo(id).Symbol;
                            if (sym != null && IsEntityOrSubclass(sym.GetSymbolTypeSafe()))
                                preAwaitIgnore.Add(sym);
                        }
                    }
                }
            }

            var parent = awaitExpr.Parent;
            if (parent is AssignmentExpressionSyntax a)
            {
                var left = semanticModel.GetSymbolInfo(a.Left).Symbol;
                if (left != null) varsFromAwait.Add(left);
            }
            else if (parent is VariableDeclaratorSyntax vd)
            {
                var sym = semanticModel.GetDeclaredSymbol(vd);
                if (sym != null) varsFromAwait.Add(sym);
            }

            var awaitStmt = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();
            if (awaitStmt == null) return;

            var impacted = GetImpactedStatements(awaitStmt, semanticModel, loopDeclared);
            var assigned = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var stmt in impacted)
            {
                foreach (var assign in stmt.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var sym = semanticModel.GetSymbolInfo(assign.Left).Symbol;
                    var rhsType = semanticModel.GetTypeInfo(assign.Right).Type;
                    if (sym != null && varsBeforeAwait.Contains(sym))
                    {
                        assigned.Add(sym);
                        if (!IsEntityOrSubclass(rhsType))
                            varsFromAwait.Add(sym);
                    }
                }

                foreach (var id in stmt.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var sym = semanticModel.GetSymbolInfo(id).Symbol;
                    if (sym != null &&
                        varsBeforeAwait.Contains(sym) &&
                        !varsFromAwait.Contains(sym) &&
                        !assigned.Contains(sym) &&
                        !loopDeclared.Contains(sym) &&
                        !preAwaitIgnore.Contains(sym) &&
                        !IsInUsingScope(context, sym, awaitExpr))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, id.GetLocation(), id.Identifier.Text));
                    }
                }
            }
        }

        private static List<StatementSyntax> GetImpactedStatements(
            StatementSyntax awaitStmt,
            SemanticModel semanticModel,
            HashSet<ISymbol> loopDeclared)
        {
            var list = new List<StatementSyntax>();

            if (awaitStmt.Parent is BlockSyntax blk)
            {
                int idx = blk.Statements.IndexOf(awaitStmt);
                for (int i = idx + 1; i < blk.Statements.Count; i++)
                    list.Add(blk.Statements[i]);
            }

            SyntaxNode parent = awaitStmt;
            while ((parent = parent.Parent) != null)
            {
                if (parent is LocalFunctionStatementSyntax lfs &&
                    lfs.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(lfs) as IMethodSymbol;
                    if (methodSymbol != null)
                    {
                        var returnType = methodSymbol.ReturnType;
                        if (returnType.SpecialType == SpecialType.System_Void)
                            return list;

                        var getAwaiterMethods = returnType.GetMembers("GetAwaiter")
                            .OfType<IMethodSymbol>()
                            .Where(m => m.Parameters.Length == 0);

                        if (getAwaiterMethods.Any())
                            return list;
                    }
                }

                if (parent is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                {
                    if (parenthesizedLambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                        return list;
                }
                else if (parent is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    if (simpleLambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                        return list;
                }
                else if (parent is AnonymousMethodExpressionSyntax anonymousMethod)
                {
                    if (anonymousMethod.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                        return list;
                }

                if (parent is IfStatementSyntax ifStmt && parent.Parent is BlockSyntax outer)
                {
                    bool ifAwait = ContainsAwait(ifStmt.Statement);
                    bool elseAwait = ContainsAwait(ifStmt.Else?.Statement);
                    bool ifExit = Terminates(ifStmt.Statement);
                    bool elseExit = ifStmt.Else != null && Terminates(ifStmt.Else.Statement);
                    if ((ifAwait && !ifExit) || (elseAwait && !elseExit))
                    {
                        int idx = outer.Statements.IndexOf(ifStmt);
                        for (int i = idx + 1; i < outer.Statements.Count; i++)
                            list.Add(outer.Statements[i]);
                    }
                }
                else if (parent is StatementSyntax stmt &&
                         ContainsAwait(stmt) &&
                         (stmt is WhileStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or DoStatementSyntax))
                {
                    List<StatementSyntax> body;
                    if (stmt is BlockSyntax b)
                    {
                        body = b.Statements.ToList();
                    }
                    else
                    {
                        body = new List<StatementSyntax> { stmt };
                    }
                    list.AddRange(body);

                    foreach (var decl in stmt.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    {
                        var sym = semanticModel.GetDeclaredSymbol(decl);
                        if (sym is ILocalSymbol ls && IsEntityOrSubclass(ls.Type))
                            loopDeclared.Add(ls);
                    }
                }
            }

            return list;
        }

        private static bool ContainsAwait(StatementSyntax stmt)
        {
            return stmt?.DescendantNodes().OfType<AwaitExpressionSyntax>().Any() == true;
        }

        private static bool Terminates(StatementSyntax stmt)
        {
            if (stmt == null) return false;
            if (stmt is BlockSyntax b && b.Statements.Any())
                stmt = b.Statements.Last();

            return stmt is ReturnStatementSyntax or ThrowStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax;
        }

        private static bool IsInUsingScope(SyntaxNodeAnalysisContext context, ISymbol sym, AwaitExpressionSyntax awaitExpr)
        {
            var syntax = sym.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            var usingDecl = syntax?.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
            if (usingDecl == null || !usingDecl.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                return false;

            var block = usingDecl.FirstAncestorOrSelf<BlockSyntax>();
            var ablock = awaitExpr.FirstAncestorOrSelf<BlockSyntax>();
            return block != null && ablock != null && block.Span.Contains(awaitExpr.SpanStart);
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
