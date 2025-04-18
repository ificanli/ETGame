using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using DiagnosticDescriptor = Microsoft.CodeAnalysis.DiagnosticDescriptor;
using DiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;
using LanguageNames = Microsoft.CodeAnalysis.LanguageNames;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ModuleAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor FieldAccessRule = new(DiagnosticIds.ETFieldAccessDiagnosticId,
            "Field access not allowed",
            "Access to field '{0}' is not allowed due to module restriction",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor CyclicCallRule = new(DiagnosticIds.ETCyclicCallDiagnosticId,
            "Circular module dependency",
            "Module '{0}' must not call module '{1}' because the reverse call already exists",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(FieldAccessRule, CyclicCallRule);

        public override void Initialize(AnalysisContext context)
        {
            if (AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var moduleCallMap = new Dictionary<string, HashSet<string>>();

                startContext.RegisterSyntaxNodeAction(ctx =>
                {
                    if (AnalyzerHelper.IsAssemblyNeedAnalyze(ctx.Compilation.AssemblyName, AnalyzeAssembly.All))
                    {
                        return;
                    }

                    AnalyzeFieldAccess(ctx);
                    
                }, SyntaxKind.SimpleMemberAccessExpression);

                startContext.RegisterSyntaxNodeAction(ctx =>
                {
                    if (AnalyzerHelper.IsAssemblyNeedAnalyze(ctx.Compilation.AssemblyName, AnalyzeAssembly.All))
                    {
                        return;
                    }
                    AnalyzeMethodCall(ctx, moduleCallMap);
                }, SyntaxKind.InvocationExpression);
            });
        }

        private void AnalyzeFieldAccess(SyntaxNodeAnalysisContext context)
        {
            MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)context.Node;
            IFieldSymbol? symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IFieldSymbol;
            if (symbol == null) return;

            INamedTypeSymbol? fieldDeclaringClass = symbol.ContainingType;
            string fieldModule = GetModuleName(fieldDeclaringClass);
            INamedTypeSymbol? accessingClass = context.ContainingSymbol?.ContainingType;
            if (accessingClass == null)
            {
                return;
            }
            string accessModule = GetModuleName(accessingClass);

            if (symbol.DeclaredAccessibility == Accessibility.Public &&
                !string.IsNullOrEmpty(fieldModule) &&
                fieldModule == accessModule)
            {
                return;
            }

            Diagnostic diagnostic = Diagnostic.Create(FieldAccessRule, memberAccess.GetLocation(), symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private void AnalyzeMethodCall(SyntaxNodeAnalysisContext context, Dictionary<string, HashSet<string>> moduleCallMap)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
            IMethodSymbol? methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null) return;

            INamedTypeSymbol? targetClass = methodSymbol.ContainingType;
            string targetModule = GetModuleName(targetClass);

            INamedTypeSymbol? callerClass = context.ContainingSymbol?.ContainingType;
            if (callerClass == null)
            {
                return;
            }
            string callerModule = GetModuleName(callerClass);

            if (string.IsNullOrEmpty(targetModule) || string.IsNullOrEmpty(callerModule))
                return;

            if (callerModule == targetModule)
                return;

            // 检查是否存在相反方向的依赖
            if (moduleCallMap.TryGetValue(targetModule, out var calledModules) &&
                calledModules.Contains(callerModule))
            {
                Diagnostic diagnostic = Diagnostic.Create(CyclicCallRule,
                    invocation.GetLocation(),
                    callerModule,
                    targetModule);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // 记录当前调用关系
            if (!moduleCallMap.ContainsKey(callerModule))
            {
                moduleCallMap[callerModule] = new HashSet<string>();
            }

            moduleCallMap[callerModule].Add(targetModule);
        }

        private string GetModuleName(INamedTypeSymbol classSymbol)
        {
            foreach (AttributeData? attr in classSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "ModuleAttribute" &&
                    attr.ConstructorArguments.Length == 1 &&
                    attr.ConstructorArguments[0].Value is string name)
                {
                    return name;
                }
            }

            return "Global";
        }
    }
}