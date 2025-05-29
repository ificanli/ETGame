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
            title: "禁止类之间形成循环调用",
            messageFormat: "检测到类循环依赖: {0}",
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
                // 使用线程安全集合 ConcurrentBag 代替 List
                var graph = new ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<AccessRecord>>(SymbolEqualityComparer.Default);

                startContext.RegisterSyntaxNodeAction(ctx => AnalyzeMemberAccess(ctx, graph), SyntaxKind.IdentifierName);
                startContext.RegisterSyntaxNodeAction(ctx => AnalyzeObjectCreation(ctx, graph), SyntaxKind.ObjectCreationExpression);
                startContext.RegisterCompilationEndAction(ctx => AnalyzeGraph(ctx, graph));
            });
        }

        private void AnalyzeMemberAccess(
            SyntaxNodeAnalysisContext context,
            ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<AccessRecord>> graph)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                return;

            var identifier = (IdentifierNameSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null) return;

            var callerType = context.ContainingSymbol?.ContainingType;
            if (callerType == null) return;

            var targetType = symbol.ContainingType;
            if (targetType == null) return;

            if (SymbolEqualityComparer.Default.Equals(callerType, targetType)) return;
            if (HasIgnoreAttribute(callerType) || HasIgnoreAttribute(targetType)) return;

            AddAccess(graph, callerType, context.ContainingSymbol, targetType, symbol, identifier.GetLocation());
        }

        private void AnalyzeObjectCreation(
            SyntaxNodeAnalysisContext context,
            ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<AccessRecord>> graph)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                return;

            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
            if (symbol == null) return;

            var callerType = context.ContainingSymbol?.ContainingType;
            if (callerType == null) return;

            var targetType = symbol.ContainingType;
            if (targetType == null) return;

            if (SymbolEqualityComparer.Default.Equals(callerType, targetType)) return;
            if (HasIgnoreAttribute(callerType) || HasIgnoreAttribute(targetType)) return;

            AddAccess(graph, callerType, context.ContainingSymbol, targetType, symbol, creation.GetLocation());
        }

        private void AddAccess(
            ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<AccessRecord>> graph,
            INamedTypeSymbol fromType,
            ISymbol callerSymbol,
            INamedTypeSymbol toType,
            ISymbol targetSymbol,
            Location location)
        {
            var bag = graph.GetOrAdd(fromType, _ => new ConcurrentBag<AccessRecord>());
            bag.Add(new AccessRecord(callerSymbol, targetSymbol, toType, location));
        }

        private void AnalyzeGraph(
            CompilationAnalysisContext context,
            ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<AccessRecord>> graph)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                return;

            foreach (var node in graph.Keys)
            {
                DetectClassCycles(node, graph, new List<AccessRecord>(), new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default), context);
            }
        }

        private void DetectClassCycles(
            INamedTypeSymbol currentClass,
            ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<AccessRecord>> graph,
            List<AccessRecord> pathRecords,
            HashSet<INamedTypeSymbol> classPathSet,
            CompilationAnalysisContext context)
        {
            if (HasIgnoreAttribute(currentClass)) return;

            if (classPathSet.Contains(currentClass))
            {
                var index = pathRecords.FindLastIndex(r => SymbolEqualityComparer.Default.Equals(r.TargetClass, currentClass));
                if (index >= 0)
                {
                    var cyclePath = pathRecords.Skip(index).ToList();
                    var message = string.Join("\n", cyclePath.Select(r =>
                        $"{r.CallerSymbol.ContainingType.Name}.{r.CallerSymbol.Name}() -> {r.TargetClass.Name}.{r.TargetSymbol.Name}()"));
                    var firstLocation = cyclePath.First().Location ?? Location.None;
                    context.ReportDiagnostic(Diagnostic.Create(Rule, firstLocation, message));
                }
                return;
            }

            classPathSet.Add(currentClass);

            if (graph.TryGetValue(currentClass, out var records))
            {
                foreach (var record in records)
                {
                    pathRecords.Add(record);
                    DetectClassCycles(record.TargetClass, graph, pathRecords, classPathSet, context);
                    pathRecords.RemoveAt(pathRecords.Count - 1);
                }
            }

            classPathSet.Remove(currentClass);
        }

        private static bool HasIgnoreAttribute(INamedTypeSymbol type)
        {
            return type.GetAttributes().Any(attr => attr.AttributeClass?.Name == "IgnoreCircularDependencyAttribute");
        }

        private class AccessRecord
        {
            public ISymbol CallerSymbol { get; }
            public ISymbol TargetSymbol { get; }
            public INamedTypeSymbol TargetClass { get; }
            public Location Location { get; }

            public AccessRecord(ISymbol callerSymbol, ISymbol targetSymbol, INamedTypeSymbol targetClass, Location location)
            {
                CallerSymbol = callerSymbol;
                TargetSymbol = targetSymbol;
                TargetClass = targetClass;
                Location = location;
            }
        }
    }
}
