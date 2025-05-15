#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LambdaEntityCaptureAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NoETEntityClosure";
        private static readonly LocalizableString Title = "禁止Lambda捕获ET.Entity类型变量";
        private static readonly LocalizableString MessageFormat = "Lambda表达式禁止捕获ET.Entity类型变量 '{0}'，因为生命周期不安全";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
            {
                return;
            }
            
            var lambda = (LambdaExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var dataFlow = semanticModel.AnalyzeDataFlow(lambda);

            foreach (var captured in dataFlow.Captured)
            {
                var type = (captured as ILocalSymbol)?.Type ?? (captured as IParameterSymbol)?.Type;
                if (type != null && IsOrInheritsFromETEntity(type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule, lambda.GetLocation(),
                        captured.Name));
                }
            }
        }

        // 判断类型是否为 ET.Entity 或其子类
        private static bool IsOrInheritsFromETEntity(ITypeSymbol type)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (t.ToDisplayString() == "ET.Entity")
                    return true;
            }
            return false;
        }
    }
}
