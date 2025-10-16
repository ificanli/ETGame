#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessageObjectReferenceAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title = "禁止Entity引用MessageObject";
        private static readonly LocalizableString MessageFormat =
            "禁止在Entity及其子类中引用MessageObject及其子类或其class类型成员: {0}";
        private static readonly LocalizableString Description =
            "Entity不能引用MessageObject及其子类的实例或class类型成员，除非类型为struct、string、不可变类型(如ImmutableList<T>)或标记为不可变的类型.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "ETMO001",
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(startContext =>
            {
                var entityType = startContext.Compilation.GetTypeByMetadataName("ET.Entity");
                var messageObjectType = startContext.Compilation.GetTypeByMetadataName("ET.MessageObject");
                if (entityType == null || messageObjectType == null)
                    return;

                // Only check field and property declarations
                startContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeFieldDeclaration(ctx, entityType, messageObjectType),
                    SyntaxKind.FieldDeclaration);

                startContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzePropertyDeclaration(ctx, entityType, messageObjectType),
                    SyntaxKind.PropertyDeclaration);
            });
        }

        private static void AnalyzeFieldDeclaration(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol entityType,
            INamedTypeSymbol messageObjectType)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            var containingType = context.ContainingSymbol?.ContainingType;

            if (containingType == null || !DerivesFrom(containingType, entityType))
                return;

            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                if (fieldSymbol == null)
                    continue;

                if (IsDisallowedType(fieldSymbol.Type, messageObjectType))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        variable.GetLocation(),
                        $"Field '{fieldSymbol.Name}' with type '{fieldSymbol.Type.ToDisplayString()}'");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzePropertyDeclaration(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol entityType,
            INamedTypeSymbol messageObjectType)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            var containingType = context.ContainingSymbol?.ContainingType;

            if (containingType == null || !DerivesFrom(containingType, entityType))
                return;

            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;
            if (propertySymbol == null)
                return;

            if (IsDisallowedType(propertySymbol.Type, messageObjectType))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    propertyDeclaration.Type.GetLocation(),
                    $"Property '{propertySymbol.Name}' with type '{propertySymbol.Type.ToDisplayString()}'");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsDisallowedType(ITypeSymbol type, INamedTypeSymbol messageObjectType)
        {
            if (type == null)
                return false;

            // Allow value types (struct, enum, primitives)
            if (type.IsValueType)
                return false;

            // Allow string
            if (type.SpecialType == SpecialType.System_String)
                return false;

            // Allow readonly struct
            if (type.IsReadOnly)
                return false;

            // Check if type is MessageObject or derives from it
            if (DerivesFrom(type, messageObjectType))
            {
                // Allow if marked as Immutable
                if (type.GetAttributes().Any(attr =>
                    attr.AttributeClass?.Name.Contains("Immutable") == true ||
                    attr.AttributeClass?.Name.Contains("ReadOnly") == true))
                    return false;

                return true;
            }

            // Check generic type arguments (List<T>, Dictionary<K,V>, etc.)
            if (type is INamedTypeSymbol namedType)
            {
                // Allow System.Collections.Immutable types
                var typeString = namedType.OriginalDefinition.ToString();
                if (typeString.StartsWith("System.Collections.Immutable."))
                    return false;

                // Check all generic type arguments recursively
                foreach (var typeArg in namedType.TypeArguments)
                {
                    if (IsDisallowedType(typeArg, messageObjectType))
                        return true;
                }
            }

            // Check array element type
            if (type is IArrayTypeSymbol arrayType)
            {
                return IsDisallowedType(arrayType.ElementType, messageObjectType);
            }

            return false;
        }

        private static bool DerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            while (type != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type, baseType))
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}