#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        private static readonly ConcurrentDictionary<string, bool> SkipTypeAttributeCache = new();
        private static readonly ConcurrentDictionary<string, bool> AsyncReturnTypeCache = new();

        private static bool HasSkipAttributeCached(ITypeSymbol type)
        {
            if (type == null) return false;
            var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return SkipTypeAttributeCache.GetOrAdd(key, _ =>
            {
                return type.GetAttributes().Any(attr =>
                {
                    var name = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return name?.EndsWith(".SkipAwaitEntityCheck") == true
                           || name?.EndsWith(".SkipAwaitEntityCheckAttribute") == true;
                });
            });
        }

        private static bool IsAsyncReturnType(ITypeSymbol type)
        {
            if (type == null) return false;
            var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return AsyncReturnTypeCache.GetOrAdd(key, _ =>
            {
                if (type.SpecialType == SpecialType.System_Void)
                    return true;

                var members = type.GetMembers("GetAwaiter");
                foreach (var member in members)
                {
                    if (member is IMethodSymbol ms && ms.Parameters.Length == 0)
                        return true;
                }
                return false;
            });
        }

        public override void Initialize(AnalysisContext context)
        {
            if (AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx =>
            {
                var awaitExpr = (AwaitExpressionSyntax)ctx.Node;
                if (!AnalyzerHelper.IsAssemblyNeedAnalyze(ctx.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                    return;

                if (awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } method)
                    return;

                // 这里注释掉早期跳过逻辑，强制执行完整分析，方便调试
                // if (!MethodHasEntityVariables(ctx.SemanticModel, method))
                //     return;

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

            var declaredSymbolCache = new Dictionary<SyntaxNode, ISymbol>();
            ISymbol GetCachedDeclaredSymbol(SyntaxNode node)
            {
                if (declaredSymbolCache.TryGetValue(node, out var s)) return s;
                var sym = semanticModel.GetDeclaredSymbol(node);
                declaredSymbolCache[node] = sym;
                return sym;
            }

            var symbolInfoCache = new Dictionary<SyntaxNode, ISymbol>();
            ISymbol GetCachedSymbolInfo(SyntaxNode node)
            {
                if (symbolInfoCache.TryGetValue(node, out var s)) return s;
                var sym = semanticModel.GetSymbolInfo(node).Symbol;
                symbolInfoCache[node] = sym;
                return sym;
            }

            var varsBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var varsFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var loopDeclared = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var preAwaitIgnore = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var p in method.ParameterList.Parameters)
            {
                var ps = GetCachedDeclaredSymbol(p);
                if (ps != null && IsEntityOrSubclass(ps.GetSymbolTypeSafe()))
                {
                    varsBeforeAwait.Add(ps);
                    Debug.WriteLine($"Parameter entity detected: {ps.Name}");
                }
            }

            if (awaitExpr.FirstAncestorOrSelf<ForEachStatementSyntax>() is { } foreachStmt)
            {
                foreach (var id in EnumerateIdentifiersUnlimited(foreachStmt.Expression))
                {
                    var sym = GetCachedSymbolInfo(id);
                    if (sym != null && IsEntityOrSubclass(sym.GetSymbolTypeSafe()))
                    {
                        preAwaitIgnore.Add(sym);
                        Debug.WriteLine($"Foreach expression entity detected: {sym.Name}");
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
                            var sym = GetCachedDeclaredSymbol(v);
                            if (sym is ILocalSymbol ls && IsEntityOrSubclass(ls.Type))
                            {
                                varsBeforeAwait.Add(ls);
                                Debug.WriteLine($"Local entity detected: {ls.Name}");
                            }
                        }
                    }

                    if (stmt is ForEachStatementSyntax foreachStmt2)
                    {
                        foreach (var id in EnumerateIdentifiersUnlimited(foreachStmt2.Expression))
                        {
                            var sym = GetCachedSymbolInfo(id);
                            if (sym != null && IsEntityOrSubclass(sym.GetSymbolTypeSafe()))
                            {
                                preAwaitIgnore.Add(sym);
                                Debug.WriteLine($"Foreach2 expression entity detected: {sym.Name}");
                            }
                        }
                    }
                }
            }

            var parent = awaitExpr.Parent;
            if (parent is AssignmentExpressionSyntax a)
            {
                var left = GetCachedSymbolInfo(a.Left);
                if (left != null) varsFromAwait.Add(left);
            }
            else if (parent is VariableDeclaratorSyntax vd)
            {
                var sym = GetCachedDeclaredSymbol(vd);
                if (sym != null) varsFromAwait.Add(sym);
            }

            var awaitStmt = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();
            if (awaitStmt == null) return;

            var impacted = GetImpactedStatements(awaitStmt, semanticModel, loopDeclared, GetCachedDeclaredSymbol);
            var assigned = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var stmt in impacted)
            {
                foreach (var assign in stmt.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var sym = GetCachedSymbolInfo(assign.Left);
                    var rhsType = semanticModel.GetTypeInfo(assign.Right).Type;
                    if (sym != null && varsBeforeAwait.Contains(sym))
                    {
                        assigned.Add(sym);
                        if (!IsEntityOrSubclass(rhsType))
                            varsFromAwait.Add(sym);
                    }
                }

                foreach (var id in EnumerateIdentifiersUnlimited(stmt))
                {
                    var sym = GetCachedSymbolInfo(id);
                    if (sym != null &&
                        varsBeforeAwait.Contains(sym) &&
                        !varsFromAwait.Contains(sym) &&
                        !assigned.Contains(sym) &&
                        !loopDeclared.Contains(sym) &&
                        !preAwaitIgnore.Contains(sym) &&
                        !IsInUsingScope(context, sym, awaitExpr) &&
                        !IsVariableFromSkipClass(sym))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, id.GetLocation(), id.Identifier.Text));
                        Debug.WriteLine($"Diagnostic reported on {id.Identifier.Text}");
                    }
                }
            }
        }

        private static IEnumerable<IdentifierNameSyntax> EnumerateIdentifiersUnlimited(SyntaxNode node)
        {
            if (node == null) yield break;
            foreach (var id in node.DescendantNodes().OfType<IdentifierNameSyntax>())
                yield return id;
        }

        private static List<StatementSyntax> GetImpactedStatements(
            StatementSyntax awaitStmt,
            SemanticModel semanticModel,
            HashSet<ISymbol> loopDeclared,
            Func<SyntaxNode, ISymbol> getDeclaredSymbol)
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
                    if (methodSymbol != null && IsAsyncReturnType(methodSymbol.ReturnType))
                        return list;
                }

                if (parent is ParenthesizedLambdaExpressionSyntax pls && pls.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                    return list;
                if (parent is SimpleLambdaExpressionSyntax sls && sls.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                    return list;
                if (parent is AnonymousMethodExpressionSyntax ams && ams.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                    return list;

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
                    List<StatementSyntax> body = stmt is BlockSyntax b ? b.Statements.ToList() : new List<StatementSyntax> { stmt };
                    list.AddRange(body);

                    foreach (var decl in stmt.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    {
                        var sym = getDeclaredSymbol(decl);
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
            if (usingDecl == null || !usingDecl.UsingKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.UsingKeyword))
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

        private static bool IsVariableFromSkipClass(ISymbol variableSymbol)
        {
            if (variableSymbol == null)
                return false;

            var varType = variableSymbol.GetSymbolTypeSafe();
            if (HasSkipAttributeCached(varType))
                return true;

            var containingType = variableSymbol.ContainingType;
            if (HasSkipAttributeCached(containingType))
                return true;

            return false;
        }
    }
}
