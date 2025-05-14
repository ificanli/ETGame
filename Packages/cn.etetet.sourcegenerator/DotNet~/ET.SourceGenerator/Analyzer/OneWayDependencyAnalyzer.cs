#nullable disable
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
            title: "禁止类之间形成任何循环调用",
            messageFormat: "检测到类循环依赖链: {0}",
            category: "Dependency",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var graph = new ConcurrentDictionary<INamedTypeSymbol, List<AccessRecord>>(SymbolEqualityComparer.Default);

                startContext.RegisterSyntaxNodeAction(ctx => AnalyzeMemberAccess(ctx, graph), SyntaxKind.IdentifierName);
                startContext.RegisterSyntaxNodeAction(ctx => AnalyzeObjectCreation(ctx, graph), SyntaxKind.ObjectCreationExpression);
                startContext.RegisterCompilationEndAction(ctx => AnalyzeGraph(ctx, graph));
            });
        }

        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, ConcurrentDictionary<INamedTypeSymbol, List<AccessRecord>> graph)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
            {
                return;
            }
            
            var identifier = (IdentifierNameSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null) return;

            var callerType = context.ContainingSymbol?.ContainingType;
            if (callerType == null) return;

            var targetType = symbol.ContainingType;
            if (targetType == null) return;

            if (SymbolEqualityComparer.Default.Equals(callerType, targetType)) return;

            var kind = symbol switch
            {
                IMethodSymbol => AccessKind.MethodCall,
                IFieldSymbol => AccessKind.FieldAccess,
                IPropertySymbol => AccessKind.PropertyAccess,
                _ => AccessKind.MethodCall
            };

            AddAccess(graph, callerType, context.ContainingSymbol, targetType, identifier.GetLocation(), kind);
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, ConcurrentDictionary<INamedTypeSymbol, List<AccessRecord>> graph)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            if (symbol == null) return;

            var callerType = context.ContainingSymbol?.ContainingType;
            if (callerType == null) return;

            var targetType = symbol.ContainingType;
            if (targetType == null) return;

            if (SymbolEqualityComparer.Default.Equals(callerType, targetType)) return;

            AddAccess(graph, callerType, context.ContainingSymbol, targetType, creation.GetLocation(), AccessKind.ConstructorCall);
        }

        private void AddAccess(ConcurrentDictionary<INamedTypeSymbol, List<AccessRecord>> graph, INamedTypeSymbol fromType, ISymbol callerSymbol, INamedTypeSymbol toType, Location location, AccessKind kind)
        {
            var list = graph.GetOrAdd(fromType, _ => new List<AccessRecord>());
            list.Add(new AccessRecord(callerSymbol, toType, location, kind));
        }

        private void AnalyzeGraph(CompilationAnalysisContext context, ConcurrentDictionary<INamedTypeSymbol, List<AccessRecord>> graph)
        {
            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var stack = new Stack<AccessRecord>();

            foreach (var node in graph.Keys)
            {
                DetectCycles(node, graph, visited, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default), stack, context);
            }
        }

        private void DetectCycles(
            INamedTypeSymbol current,
            ConcurrentDictionary<INamedTypeSymbol, List<AccessRecord>> graph,
            HashSet<INamedTypeSymbol> visited,
            HashSet<INamedTypeSymbol> pathSet,
            Stack<AccessRecord> pathStack,
            CompilationAnalysisContext context)
        {
            if (pathSet.Contains(current))
            {
                var cycle = pathStack.Reverse()
                    .SkipWhile(r => !SymbolEqualityComparer.Default.Equals(r.CallerSymbol.ContainingType, current))
                    .ToList();

                if (cycle.Any())
                {
                    var message = string.Join(" -> ", cycle.Select(r => $"{r.CallerSymbol.ContainingType.Name}.{r.CallerSymbol.Name}"));
                    var firstLocation = cycle.First().Location ?? Location.None;
                    context.ReportDiagnostic(Diagnostic.Create(Rule, firstLocation, message));
                }
                return;
            }

            visited.Add(current);
            pathSet.Add(current);

            if (graph.TryGetValue(current, out var records))
            {
                foreach (var record in records)
                {
                    pathStack.Push(record);
                    DetectCycles(record.TargetClass, graph, visited, pathSet, pathStack, context);
                    pathStack.Pop();
                }
            }

            pathSet.Remove(current);
        }

        private class AccessRecord
        {
            public ISymbol CallerSymbol { get; }
            public INamedTypeSymbol TargetClass { get; }
            public Location Location { get; }
            public AccessKind Kind { get; }

            public AccessRecord(ISymbol callerSymbol, INamedTypeSymbol targetClass, Location location, AccessKind kind)
            {
                CallerSymbol = callerSymbol;
                TargetClass = targetClass;
                Location = location;
                Kind = kind;
            }
        }

        private enum AccessKind
        {
            MethodCall,
            FieldAccess,
            ConstructorCall,
            PropertyAccess
        }
    }
}
