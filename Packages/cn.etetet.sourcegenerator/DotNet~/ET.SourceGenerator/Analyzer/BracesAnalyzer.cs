using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
#nullable disable

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BracesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BR001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            "Statement must use braces",
            "The '{0}' statement must use braces",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeIf, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(AnalyzeElse, SyntaxKind.ElseClause);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.DoStatement);
        }

        private void AnalyzeIf(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
            {
                return;
            }
            
            var ifStmt = (IfStatementSyntax)context.Node;
            if (!(ifStmt.Statement is BlockSyntax))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, ifStmt.IfKeyword.GetLocation(), "if"));
            }
        }

        private void AnalyzeElse(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
            {
                return;
            }
            
            var elseClause = (ElseClauseSyntax)context.Node;
            if (!(elseClause.Statement is BlockSyntax) &&
                !(elseClause.Statement is IfStatementSyntax)) // allow else-if
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, elseClause.ElseKeyword.GetLocation(), "else"));
            }
        }

        private void AnalyzeLoop(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
            {
                return;
            }
            
            StatementSyntax body = null;
            string keyword = null;

            switch (context.Node)
            {
                case ForStatementSyntax forStmt:
                    body = forStmt.Statement;
                    keyword = "for";
                    break;
                case ForEachStatementSyntax forEachStmt:
                    body = forEachStmt.Statement;
                    keyword = "foreach";
                    break;
                case WhileStatementSyntax whileStmt:
                    body = whileStmt.Statement;
                    keyword = "while";
                    break;
                case DoStatementSyntax doStmt:
                    body = doStmt.Statement;
                    keyword = "do";
                    break;
            }

            if (body != null && !(body is BlockSyntax))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetFirstToken().GetLocation(), keyword));
            }
        }
    }
}