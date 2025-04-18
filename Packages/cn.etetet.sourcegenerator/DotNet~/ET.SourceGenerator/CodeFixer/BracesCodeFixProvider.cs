#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace ET
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BracesCodeFixProvider)), Shared]
    public class BracesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(BracesAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            Diagnostic diagnostic = context.Diagnostics[0];
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);

            context.RegisterCodeFix(Microsoft.CodeAnalysis.CodeActions.CodeAction.Create("Add braces",
                    ct => AddBracesAsync(context.Document, node, ct),
                    equivalenceKey: "AddBraces"),
                diagnostic);
        }

        private async Task<Document> AddBracesAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);

            SyntaxNode newRoot = null;

            if (node is IfStatementSyntax ifStmt && !(ifStmt.Statement is BlockSyntax))
            {
                IfStatementSyntax newIf = ifStmt.WithStatement(WrapWithBlock(ifStmt.Statement));
                newRoot = root.ReplaceNode(ifStmt, newIf);
            }
            else if (node is ElseClauseSyntax elseClause && !(elseClause.Statement is BlockSyntax))
            {
                ElseClauseSyntax newElse = elseClause.WithStatement(WrapWithBlock(elseClause.Statement));
                newRoot = root.ReplaceNode(elseClause, newElse);
            }
            else if (node is ForStatementSyntax forStmt && !(forStmt.Statement is BlockSyntax))
            {
                ForStatementSyntax newFor = forStmt.WithStatement(WrapWithBlock(forStmt.Statement));
                newRoot = root.ReplaceNode(forStmt, newFor);
            }
            else if (node is ForEachStatementSyntax forEachStmt && !(forEachStmt.Statement is BlockSyntax))
            {
                ForEachStatementSyntax newForEach = forEachStmt.WithStatement(WrapWithBlock(forEachStmt.Statement));
                newRoot = root.ReplaceNode(forEachStmt, newForEach);
            }
            else if (node is WhileStatementSyntax whileStmt && !(whileStmt.Statement is BlockSyntax))
            {
                WhileStatementSyntax newWhile = whileStmt.WithStatement(WrapWithBlock(whileStmt.Statement));
                newRoot = root.ReplaceNode(whileStmt, newWhile);
            }
            else if (node is DoStatementSyntax doStmt && !(doStmt.Statement is BlockSyntax))
            {
                DoStatementSyntax newDo = doStmt.WithStatement(WrapWithBlock(doStmt.Statement));
                newRoot = root.ReplaceNode(doStmt, newDo);
            }

            return newRoot != null ? document.WithSyntaxRoot(newRoot) : document;
        }

        private static BlockSyntax WrapWithBlock(StatementSyntax statement)
        {
            return SyntaxFactory.Block(statement).WithTrailingTrivia(statement.GetTrailingTrivia());
        }
    }
}