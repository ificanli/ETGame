#nullable disable
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EntityMemberAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ET0111";
        private static readonly LocalizableString Title = "禁止将 Entity 类型作为成员";
        private static readonly LocalizableString MessageFormat = "成员类型 '{0}' 引用了 Entity；直接使用不被允许，请使用 EntityRef";
        private static readonly LocalizableString Description = "禁止在类或结构体中直接声明 Entity 类型的字段或属性，或将其放入容器；应使用 EntityRef.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        // 特性名称：用于标记允许引用 Entity 的字段或属性
        private const string AllowAttributeName = "AllowEntityMemberAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzeMember, SymbolKind.Field, SymbolKind.Property);
        }

        private static void AnalyzeMember(SymbolAnalysisContext context)
        {
            // 如果成员上标记了 [AllowEntityMember]，则跳过诊断
            if (HasAllowAttribute(context.Symbol))
                return;

            var containingType = context.Symbol.ContainingType;
            if (containingType != null && IsEntityRef(containingType))
            {
                return;
            }

            IFieldSymbol fieldSymbol = context.Symbol as IFieldSymbol;
            IPropertySymbol propSymbol = context.Symbol as IPropertySymbol;
            ITypeSymbol memberType;
            Location location;

            if (fieldSymbol != null)
            {
                memberType = fieldSymbol.Type;
                location = fieldSymbol.Locations[0];
            }
            else if (propSymbol != null)
            {
                // 仅检测自动属性
                var syntaxRef = propSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (syntaxRef != null)
                {
                    var propSyntax = syntaxRef.GetSyntax() as PropertyDeclarationSyntax;
                    if (propSyntax != null &&
                        (propSyntax.ExpressionBody != null || propSyntax.AccessorList == null ||
                         propSyntax.AccessorList.Accessors.Any(a => a.Body != null || a.ExpressionBody != null)))
                    {
                        // 非自动属性或有自定义访问器，跳过检查
                        return;
                    }
                }

                memberType = propSymbol.Type;
                location = propSymbol.Locations[0];
            }
            else
            {
                return;
            }

            if (ContainsEntity(memberType))
            {
                var diagnostic = Diagnostic.Create(Rule, location, memberType.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool HasAllowAttribute(ISymbol symbol)
        {
            return symbol.GetAttributes().Any(attr =>
                attr.AttributeClass.Name == AllowAttributeName ||
                attr.AttributeClass.ToDisplayString() == AllowAttributeName);
        }

        private static bool ContainsEntity(ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            // 跳过委托类型，如 Action<>, Func<>
            if (symbol.TypeKind == TypeKind.Delegate)
                return false;

            if (IsEntityRef(symbol))
                return false;

            if (IsEntityDerived(symbol))
                return true;

            if (symbol is IArrayTypeSymbol arrayType)
                return ContainsEntity(arrayType.ElementType);

            if (symbol is INamedTypeSymbol named && named.IsGenericType)
                return named.TypeArguments.Any(arg => ContainsEntity(arg));

            return false;
        }

        private static bool IsEntityDerived(ITypeSymbol symbol)
        {
            for (var current = symbol; current != null; current = current.BaseType)
            {
                if (current.Name == "Entity")
                    return true;
            }
            return false;
        }

        private static bool IsEntityRef(ITypeSymbol symbol)
        {
            if (symbol.Name == "EntityRef")
                return true;
            if (symbol is INamedTypeSymbol named && named.IsGenericType && named.Name == "EntityRef")
                return true;
            return false;
        }
    }
}
