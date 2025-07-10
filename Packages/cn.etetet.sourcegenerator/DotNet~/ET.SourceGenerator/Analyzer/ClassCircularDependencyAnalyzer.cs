#nullable disable
#pragma warning disable RS1024 // 使用 SymbolEqualityComparer 比较符号
using System;
using System.Collections.Concurrent;
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
    public class ClassCircularDependencyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: "CCD001",
            title: "禁止类循环依赖",
            messageFormat: "检测到类循环依赖调用链: {0}",
            category: "Dependency",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                if (!AnalyzerHelper.IsAssemblyNeedAnalyze(startContext.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                {
                    return;
                }
                
                var graph = new ConcurrentDictionary<INamedTypeSymbol, List<MethodEdge>>(SymbolEqualityComparer.Default);

                startContext.RegisterSyntaxNodeAction(
                    ctx => CollectTypeEdges(ctx, graph),
                    SyntaxKind.InvocationExpression,
                    SyntaxKind.ObjectCreationExpression);

                startContext.RegisterCompilationEndAction(endContext =>
                    AnalyzeTypeCycles(endContext, graph));
            });
        }

        private void CollectTypeEdges(
            SyntaxNodeAnalysisContext context,
            ConcurrentDictionary<INamedTypeSymbol, List<MethodEdge>> graph)
        {
            if (!(context.ContainingSymbol is IMethodSymbol callerMethod))
                return;

            var callerType = callerMethod.ContainingType;
            IMethodSymbol targetMethod = null;
            var node = context.Node;

            if (node is InvocationExpressionSyntax inv)
                targetMethod = context.SemanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            else if (node is ObjectCreationExpressionSyntax obj)
                targetMethod = context.SemanticModel.GetSymbolInfo(obj).Symbol as IMethodSymbol;

            if (targetMethod == null)
                return;

            var targetType = targetMethod.ContainingType;
            if (SymbolEqualityComparer.Default.Equals(callerType, targetType))
                return;
            if (HasIgnoreAttribute(callerType) || HasIgnoreAttribute(targetType))
                return;

            var location = node.GetLocation();
            var edge = new MethodEdge
            {
                CallerType = callerType,
                CallerMethod = callerMethod,
                CalleeType = targetType,
                CalleeMethod = targetMethod,
                Location = location
            };

            graph.AddOrUpdate(
                callerType,
                _ => new List<MethodEdge> { edge },
                (_, list) => { list.Add(edge); return list; });
        }

        private void AnalyzeTypeCycles(
            CompilationAnalysisContext context,
            ConcurrentDictionary<INamedTypeSymbol, List<MethodEdge>> graph)
        {
            var comparer = SymbolEqualityComparer.Default as IEqualityComparer<INamedTypeSymbol>;
            var nodes = graph.Keys
                             .Concat(graph.Values.SelectMany(v => v.Select(e => e.CalleeType)))
                             .Distinct(comparer)
                             .ToList();

            // Tarjan 查找强连通分量
            int index = 0;
            var indices = new Dictionary<INamedTypeSymbol, int>(comparer);
            var lowlink = new Dictionary<INamedTypeSymbol, int>(comparer);
            var stack = new Stack<INamedTypeSymbol>();
            var onStack = new HashSet<INamedTypeSymbol>(comparer);
            var sccs = new List<List<INamedTypeSymbol>>();

            void StrongConnect(INamedTypeSymbol v)
            {
                indices[v] = lowlink[v] = index++;
                stack.Push(v);
                onStack.Add(v);

                if (graph.TryGetValue(v, out var edges))
                {
                    foreach (var w in edges.Select(e => e.CalleeType))
                    {
                        if (!indices.ContainsKey(w))
                        {
                            StrongConnect(w);
                            lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                        }
                        else if (onStack.Contains(w))
                        {
                            lowlink[v] = Math.Min(lowlink[v], indices[w]);
                        }
                    }
                }

                if (lowlink[v] == indices[v])
                {
                    var comp = new List<INamedTypeSymbol>();
                    INamedTypeSymbol w;
                    do
                    {
                        w = stack.Pop();
                        onStack.Remove(w);
                        comp.Add(w);
                    } while (!SymbolEqualityComparer.Default.Equals(w, v));

                    if (comp.Count > 1)
                        sccs.Add(comp);
                }
            }

            foreach (var node in nodes)
                if (!indices.ContainsKey(node))
                    StrongConnect(node);

            // 对每个 SCC，找最短环并为每个调用点发出诊断
            foreach (var comp in sccs)
            {
                var cycle = FindShortestTypeCycle(comp, graph, comparer);
                if (cycle == null || cycle.Count < 2)
                    continue;
                // 去掉末尾重复起点
                if (SymbolEqualityComparer.Default.Equals(cycle[0], cycle.Last()))
                    cycle.RemoveAt(cycle.Count - 1);

                // 构造完整链路字符串
                var steps = new List<string>();
                var edges = new List<MethodEdge>();
                for (int i = 0; i < cycle.Count; i++)
                {
                    var from = cycle[i];
                    var to = cycle[(i + 1) % cycle.Count];
                    
                    // 安全地获取边信息，避免空引用异常
                    if (graph.TryGetValue(from, out var fromEdges))
                    {
                        var edge = fromEdges.FirstOrDefault(e => SymbolEqualityComparer.Default.Equals(e.CalleeType, to));
                        if (edge != null)
                        {
                            steps.Add($"{edge.CallerType.Name}.{edge.CallerMethod.Name}() -> {edge.CalleeType.Name}.{edge.CalleeMethod.Name}()");
                            edges.Add(edge);
                        }
                    }
                }
                var msg = string.Join(" | ", steps);

                // 为每个调用位置创建诊断，使其可点击
                foreach (var e in edges)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, e.Location, msg));
                }
            }
        }

        private List<INamedTypeSymbol> FindShortestTypeCycle(
            List<INamedTypeSymbol> component,
            ConcurrentDictionary<INamedTypeSymbol, List<MethodEdge>> graph,
            IEqualityComparer<INamedTypeSymbol> comparer)
        {
            foreach (var start in component)
            {
                var queue = new Queue<List<INamedTypeSymbol>>();
                var visited = new HashSet<INamedTypeSymbol>(comparer);

                if (graph.TryGetValue(start, out var edges))
                {
                    foreach (var e in edges)
                    {
                        if (component.Contains(e.CalleeType, comparer))
                            queue.Enqueue(new List<INamedTypeSymbol> { start, e.CalleeType });
                    }
                }

                while (queue.Count > 0)
                {
                    var path = queue.Dequeue();
                    var last = path.Last();
                    if (SymbolEqualityComparer.Default.Equals(last, start))
                        return path;

                    if (!visited.Add(last))
                        continue;

                    if (graph.TryGetValue(last, out var nextEdges))
                    {
                        foreach (var ne in nextEdges)
                        {
                            if (component.Contains(ne.CalleeType, comparer))
                            {
                                var newPath = new List<INamedTypeSymbol>(path) { ne.CalleeType };
                                queue.Enqueue(newPath);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static bool HasIgnoreAttribute(INamedTypeSymbol type)
            => type.GetAttributes().Any(a => a.AttributeClass?.Name == "IgnoreCircularDependencyAttribute");

        private class MethodEdge
        {
            public INamedTypeSymbol CallerType;
            public IMethodSymbol CallerMethod;
            public INamedTypeSymbol CalleeType;
            public IMethodSymbol CalleeMethod;
            public Location Location;
        }
    }
}
