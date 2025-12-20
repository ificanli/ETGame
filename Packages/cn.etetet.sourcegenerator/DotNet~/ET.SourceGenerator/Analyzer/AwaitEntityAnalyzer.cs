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
        public static ITypeSymbol GetSymbolTypeSafe(this ISymbol symbol) => symbol switch
        {
            ILocalSymbol ls      => ls.Type,
            IFieldSymbol fs      => fs.Type,
            IPropertySymbol ps   => ps.Type,
            IParameterSymbol ps2 => ps2.Type,
            _                    => null
        };
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitEntityAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new(
            "ETAE001",
            "禁止在 await 之后访问 Entity 及其子类变量",
            "变量 '{0}' 是 Entity 或其子类类型，不允许在 await 之后访问。请使用 EntityRef 包装传递！",
            "ET.Hotfix",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compCtx =>
            {
                // 全局缓存：ET.Entity 与 SkipAwaitEntityCheckAttribute
                var entityBaseSym    = compCtx.Compilation.GetTypeByMetadataName("ET.Entity");
                var skipAttrSym      = compCtx.Compilation.GetTypeByMetadataName("ET.SkipAwaitEntityCheckAttribute");

                // 方法级缓存：该方法是否含有任何 Entity 变量
                var methodHasEntity  = new ConcurrentDictionary<IMethodSymbol, bool>(SymbolEqualityComparer.Default);
                // 类型继承 & SkipAttribute 缓存
                var inheritsCache    = new ConcurrentDictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);
                var skipTypeAttrCache= new ConcurrentDictionary<ITypeSymbol, bool>(SymbolEqualityComparer.Default);

                // 判断类型是否继承自 ET.Entity（带缓存）
                bool InheritsFromEntity(ITypeSymbol type)
                {
                    if (type == null) return false;
                    if (inheritsCache.TryGetValue(type, out var ok)) return ok;
                    bool result = false;
                    var cur = type;
                    while (cur != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(cur, entityBaseSym))
                        {
                            result = true;
                            break;
                        }
                        cur = cur.BaseType;
                    }
                    inheritsCache[type] = result;
                    return result;
                }

                // 判断类型或其成员是否标记 [SkipAwaitEntityCheck]（带缓存）
                bool HasSkipAttributeCached(ITypeSymbol type)
                {
                    if (type == null) return false;
                    if (skipTypeAttrCache.TryGetValue(type, out var ok)) return ok;
                    bool result = type.GetAttributes().Any(attr =>
                    {
                        var cls = attr.AttributeClass;
                        return SymbolEqualityComparer.Default.Equals(cls, skipAttrSym)
                            || SymbolEqualityComparer.Default.Equals(cls?.BaseType, skipAttrSym);
                    });
                    skipTypeAttrCache[type] = result;
                    return result;
                }

                // 判断方法上或其所在类型上是否贴了 SkipAwaitEntityCheck
                bool HasSkipAwaitEntityCheckAttribute(IMethodSymbol method)
                    => HasSkipAttributeCached(method.ContainingType)
                       || method.GetAttributes().Any(attr =>
                           {
                               var cls = attr.AttributeClass;
                               return SymbolEqualityComparer.Default.Equals(cls, skipAttrSym)
                                   || SymbolEqualityComparer.Default.Equals(cls?.BaseType, skipAttrSym);
                           });

                // 判断某个 await 调用的目标是否贴了 SkipAwaitEntityCheck
                bool IsAwaitedMethodHasSkipAttribute(SemanticModel model, AwaitExpressionSyntax expr)
                {
                    var sym = model.GetSymbolInfo(expr.Expression).Symbol
                              ?? model.GetSymbolInfo(expr.Expression)
                                      .CandidateSymbols.FirstOrDefault();
                    if (sym is IMethodSymbol ms)   return HasSkipAwaitEntityCheckAttribute(ms);
                    if (sym is IPropertySymbol ps && ps.GetMethod != null)
                                                    return HasSkipAwaitEntityCheckAttribute(ps.GetMethod);
                    return false;
                }

                // 判断方法里是否含有任何 Entity 变量（参数或局部）
                bool ContainsAnyEntity(IMethodSymbol method, SemanticModel model)
                {
                    // 参数
                    if (method.Parameters.Any(p => InheritsFromEntity(p.Type)))
                        return true;
                    // 局部声明
                    foreach (var declRef in method.DeclaringSyntaxReferences)
                    {
                        if (declRef.GetSyntax() is MethodDeclarationSyntax mds && mds.Body != null)
                        {
                            var flow = model.AnalyzeDataFlow(mds.Body);
                            if (flow.VariablesDeclared.Any(v => InheritsFromEntity(v.GetSymbolTypeSafe())))
                                return true;
                        }
                    }
                    return false;
                }

                // 注册对 await 节点的分析
                compCtx.RegisterSyntaxNodeAction(ctx =>
                {
                    var awaitExpr  = (AwaitExpressionSyntax)ctx.Node;
                    var model      = ctx.SemanticModel;
                    var methodDecl = awaitExpr.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                    if (methodDecl == null) return;

                    var methodSym = model.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
                    if (methodSym == null) return;

                    // 早期退出：方法中无 Entity 变量
                    if (!methodHasEntity.GetOrAdd(methodSym, ms => ContainsAnyEntity(ms, model)))
                        return;

                    // 跳过贴了 SkipAwaitEntityCheck 的方法
                    if (HasSkipAwaitEntityCheckAttribute(methodSym))
                        return;

                    // 跳过带 SkipAwaitEntityCheck 的 await 调用
                    if (IsAwaitedMethodHasSkipAttribute(model, awaitExpr))
                        return;

                    // 若通过上述检查，则执行深度分析
                    AnalyzeAwaitUsage(
                        ctx,
                        awaitExpr,
                        methodDecl,
                        model,
                        InheritsFromEntity,
                        HasSkipAttributeCached);
                }, SyntaxKind.AwaitExpression);
            });
        }

        private static void AnalyzeAwaitUsage(
            SyntaxNodeAnalysisContext context,
            AwaitExpressionSyntax awaitExpr,
            MethodDeclarationSyntax method,
            SemanticModel semanticModel,
            Func<ITypeSymbol,bool> isEntityOrSubclass,
            Func<ITypeSymbol,bool> hasSkipAttributeCached)
        {
            // 本地缓存
            var declaredCache = new Dictionary<SyntaxNode, ISymbol>();
            ISymbol GetDeclared(SyntaxNode n)
                => declaredCache.TryGetValue(n, out var s)
                   ? s
                   : (declaredCache[n] = semanticModel.GetDeclaredSymbol(n));

            var symbolInfoCache = new Dictionary<SyntaxNode, ISymbol>();
            ISymbol GetSymbol(SyntaxNode n)
                => symbolInfoCache.TryGetValue(n, out var s)
                   ? s
                   : (symbolInfoCache[n] = semanticModel.GetSymbolInfo(n).Symbol);

            // 收集 await 前的 Entity 变量
            var varsBeforeAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var p in method.ParameterList.Parameters)
            {
                var ps = GetDeclared(p);
                if (ps != null && isEntityOrSubclass(ps.GetSymbolTypeSafe()))
                    varsBeforeAwait.Add(ps);
            }
            foreach (var blk in awaitExpr.Ancestors().OfType<BlockSyntax>())
            {
                foreach (var stmt in blk.Statements)
                {
                    if (stmt.Span.End >= awaitExpr.SpanStart)
                        break;
                    if (stmt is LocalDeclarationStatementSyntax decl)
                    {
                        foreach (var v in decl.Declaration.Variables)
                        {
                            var sym = GetDeclared(v);
                            if (sym is ILocalSymbol ls && isEntityOrSubclass(ls.Type))
                                varsBeforeAwait.Add(ls);
                        }
                    }
                }
            }

            // 收集 await 左侧或新声明的变量
            var varsFromAwait = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            if (awaitExpr.Parent is AssignmentExpressionSyntax assign)
            {
                var left = GetSymbol(assign.Left);
                if (left != null) varsFromAwait.Add(left);
            }
            else if (awaitExpr.Parent is VariableDeclaratorSyntax vd)
            {
                var sym = GetDeclared(vd);
                if (sym != null) varsFromAwait.Add(sym);
            }

            // 获取受影响的后续语句列表
            var loopDeclared = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var awaitStmt    = awaitExpr.FirstAncestorOrSelf<StatementSyntax>();
            if (awaitStmt == null) return;
            var impacted     = GetImpactedStatements(
                awaitStmt,
                semanticModel,
                loopDeclared,
                GetDeclared);

            var assigned = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            // 在受影响的语句中查找赋值和引用
            foreach (var stmt in impacted)
            {
                // 重新赋值后视作新变量
                foreach (var asg in stmt.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    var sym     = GetSymbol(asg.Left);
                    var rhsType = semanticModel.GetTypeInfo(asg.Right).Type;
                    if (sym != null && varsBeforeAwait.Contains(sym))
                    {
                        assigned.Add(sym);
                        if (!isEntityOrSubclass(rhsType))
                            varsFromAwait.Add(sym);
                    }
                }

                // 查找后续引用
                foreach (var id in EnumerateIdentifiersUnlimited(stmt))
                {
                    var sym = GetSymbol(id);
                    if (sym == null) continue;
                    if (!varsBeforeAwait.Contains(sym))          continue;
                    if (varsFromAwait.Contains(sym))              continue;
                    if (assigned.Contains(sym))                   continue;
                    if (loopDeclared.Contains(sym))               continue;
                    if (hasSkipAttributeCached(sym.GetSymbolTypeSafe())) continue;

                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, id.GetLocation(), id.Identifier.Text));
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

            // 同一块内，await 之后的后续语句
            if (awaitStmt.Parent is BlockSyntax blk)
            {
                int idx = blk.Statements.IndexOf(awaitStmt);
                for (int i = idx + 1; i < blk.Statements.Count; i++)
                    list.Add(blk.Statements[i]);
            }

            // 向上查找直到方法边界
            SyntaxNode parent = awaitStmt;
            while ((parent = parent.Parent) != null)
            {
                // ★ 异步本地函数跳出
                if (parent is LocalFunctionStatementSyntax lfs &&
                    lfs.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(lfs) as IMethodSymbol;
                    if (methodSymbol != null && IsAsyncReturnType(methodSymbol.ReturnType))
                        return list;
                }

                // ★ 异步 Lambda / 匿名方法跳出
                if (parent is ParenthesizedLambdaExpressionSyntax pls && pls.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                    return list;
                if (parent is SimpleLambdaExpressionSyntax sls && sls.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                    return list;
                if (parent is AnonymousMethodExpressionSyntax ams && ams.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
                    return list;

                // try-catch-finally 敏感
                if (parent is TryStatementSyntax tryStmt && parent.Parent is BlockSyntax tryOuter)
                {
                    // 如果 try 块包含 await，则需要检查 try 块是否终止
                    if (ContainsAwait(tryStmt.Block))
                    {
                        bool tryTerminates = Terminates(tryStmt.Block);

                        // ★ 添加所有 catch 块的语句（catch块中也需要检查Entity访问）
                        foreach (var catchClause in tryStmt.Catches)
                        {
                            if (catchClause.Block != null)
                            {
                                list.AddRange(catchClause.Block.Statements);
                            }
                        }

                        // ★ 添加 finally 块的语句（finally块中也需要检查Entity访问）
                        if (tryStmt.Finally?.Block != null)
                        {
                            list.AddRange(tryStmt.Finally.Block.Statements);
                        }

                        // 只有当 try 块没有终止时，才将 try-catch-finally 语句之后的代码视为受影响
                        if (!tryTerminates)
                        {
                            // 添加 try-catch-finally 语句之后的所有语句
                            int idx = tryOuter.Statements.IndexOf(tryStmt);
                            for (int i = idx + 1; i < tryOuter.Statements.Count; i++)
                                list.Add(tryOuter.Statements[i]);
                        }
                    }
                }
                // If 分支敏感
                else if (parent is IfStatementSyntax ifStmt && parent.Parent is BlockSyntax outer)
                {
                    bool ifAwait   = ContainsAwait(ifStmt.Statement);
                    bool elseAwait = ContainsAwait(ifStmt.Else?.Statement);
                    bool ifExit    = Terminates(ifStmt.Statement);
                    bool elseExit  = ifStmt.Else != null && Terminates(ifStmt.Else.Statement);
                    if ((ifAwait && !ifExit) || (elseAwait && !elseExit))
                    {
                        int idx = outer.Statements.IndexOf(ifStmt);
                        for (int i = idx + 1; i < outer.Statements.Count; i++)
                            list.Add(outer.Statements[i]);
                    }
                }
                // while 循环
                else if (parent is WhileStatementSyntax whileStmt && ContainsAwait(whileStmt.Statement))
                {
                    if (whileStmt.Statement is BlockSyntax wb)
                        list.AddRange(wb.Statements);
                    else
                        list.Add(whileStmt.Statement);

                    foreach (var decl in whileStmt.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    {
                        var sym = getDeclaredSymbol(decl);
                        if (sym is ILocalSymbol ls && !loopDeclared.Contains(ls))
                            loopDeclared.Add(ls);
                    }
                }
                // for 循环
                else if (parent is ForStatementSyntax forStmt && ContainsAwait(forStmt.Statement))
                {
                    if (forStmt.Statement is BlockSyntax fb)
                        list.AddRange(fb.Statements);
                    else
                        list.Add(forStmt.Statement);

                    foreach (var decl in forStmt.Statement.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    {
                        var sym = getDeclaredSymbol(decl);
                        if (sym is ILocalSymbol ls && !loopDeclared.Contains(ls))
                            loopDeclared.Add(ls);
                    }
                }
                // foreach 循环
                else if (parent is ForEachStatementSyntax feStmt && ContainsAwait(feStmt.Statement))
                {
                    if (feStmt.Statement is BlockSyntax fb)
                        list.AddRange(fb.Statements);
                    else
                        list.Add(feStmt.Statement);

                    foreach (var decl in feStmt.Statement.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                    {
                        var sym = getDeclaredSymbol(decl);
                        if (sym is ILocalSymbol ls && !loopDeclared.Contains(ls))
                            loopDeclared.Add(ls);
                    }
                }
            }

            return list;
        }

        private static bool ContainsAwait(StatementSyntax stmt)
            => stmt?.DescendantNodes().OfType<AwaitExpressionSyntax>().Any() == true;

        private static bool Terminates(StatementSyntax stmt)
        {
            if (stmt == null) return false;
            if (stmt is BlockSyntax b && b.Statements.Any())
                stmt = b.Statements.Last();
            return stmt is ReturnStatementSyntax
                || stmt is ThrowStatementSyntax
                || stmt is BreakStatementSyntax
                || stmt is ContinueStatementSyntax;
        }

        private static bool IsAsyncReturnType(ITypeSymbol type)
        {
            if (type == null) return false;
            if (type.SpecialType == SpecialType.System_Void) return true;
            foreach (var m in type.GetMembers("GetAwaiter"))
                if (m is IMethodSymbol ms && ms.Parameters.Length == 0)
                    return true;
            return false;
        }
    }
}
