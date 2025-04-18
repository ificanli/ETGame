namespace ET
{
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ModuleCodeFixProvider)), Shared]
public class ModuleCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.ETFieldAccessDiagnosticId);

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Diagnostic diagnostic = context.Diagnostics[0];
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        MemberAccessExpressionSyntax node = root.FindNode(diagnostic.Location.SourceSpan) as MemberAccessExpressionSyntax;
        if (node == null)
            return;

        SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
        IFieldSymbol fieldSymbol = semanticModel.GetSymbolInfo(node).Symbol as IFieldSymbol;
        if (fieldSymbol == null)
            return;

        string targetModule = GetModuleName(fieldSymbol.ContainingType);
        if (string.IsNullOrEmpty(targetModule))
            return;

        // 找调用方类声明
        ClassDeclarationSyntax classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl == null || HasModuleAttribute(classDecl))
            return;

        context.RegisterCodeFix(
            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                title: $"Add [Module(\"{targetModule}\")]",
                createChangedDocument: c => AddModuleAttributeAsync(context.Document, classDecl, targetModule, c),
                equivalenceKey: "AddModuleAttribute"),
            diagnostic);
    }

    private async Task<Document> AddModuleAttributeAsync(Document document, ClassDeclarationSyntax classDecl, string moduleName, CancellationToken cancellationToken)
    {
        AttributeSyntax attr = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Module"))
            .WithArgumentList(
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(moduleName))))));

        AttributeListSyntax attrList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attr));

        ClassDeclarationSyntax newClassDecl = classDecl.AddAttributeLists(attrList);
        SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
        SyntaxNode newRoot = root.ReplaceNode(classDecl, newClassDecl);
        return document.WithSyntaxRoot(newRoot);
    }

    private string GetModuleName(INamedTypeSymbol classSymbol)
    {
        foreach (AttributeData attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ModuleAttribute" &&
                attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is string name)
            {
                return name;
            }
        }
        return null;
    }

    private bool HasModuleAttribute(ClassDeclarationSyntax classDecl)
    {
        foreach (AttributeListSyntax attrList in classDecl.AttributeLists)
        {
            foreach (AttributeSyntax attr in attrList.Attributes)
            {
                if (attr.Name.ToString() == "Module" || attr.Name.ToString().EndsWith(".Module"))
                    return true;
            }
        }
        return false;
    }
}

}