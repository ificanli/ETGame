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

                startContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeAssignment(ctx, entityType, messageObjectType),
                    SyntaxKind.SimpleAssignmentExpression);

                startContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeInvocation(ctx, entityType, messageObjectType),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeAssignment(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol entityType,
            INamedTypeSymbol messageObjectType)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
            var fieldSymbol = symbol as IFieldSymbol;
            var propSymbol = symbol as IPropertySymbol;
            if (fieldSymbol == null && propSymbol == null)
                return;

            var containingType = (fieldSymbol ?? (ISymbol)propSymbol).ContainingType;
            if (!DerivesFrom(containingType, entityType))
                return;

            if (IsDisallowedExpression(assignment.Right, context.SemanticModel, messageObjectType))
            {
                var diag = Diagnostic.Create(Rule, assignment.Right.GetLocation(), assignment.Right.ToString());
                context.ReportDiagnostic(diag);
            }
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol entityType,
            INamedTypeSymbol messageObjectType)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                return;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                return;

            var receiverSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            var fieldSymbol = receiverSymbol as IFieldSymbol;
            var propSymbol = receiverSymbol as IPropertySymbol;
            if (fieldSymbol == null && propSymbol == null)
                return;

            var recvType = (fieldSymbol ?? (ISymbol)propSymbol).ContainingType;
            if (!DerivesFrom(recvType, entityType))
                return;

            switch (methodSymbol.Name)
            {
                case "Add":
                    foreach (var arg in invocation.ArgumentList.Arguments)
                    {
                        if (IsDisallowedExpression(arg.Expression, context.SemanticModel, messageObjectType))
                        {
                            var diag = Diagnostic.Create(Rule, arg.GetLocation(), arg.ToString());
                            context.ReportDiagnostic(diag);
                        }
                    }
                    break;

                case "AddRange":
                    var recvTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type as INamedTypeSymbol;
                    if (recvTypeInfo == null || recvTypeInfo.TypeArguments.Length == 0)
                        return;
                    var elementType = recvTypeInfo.TypeArguments[0];
                    if (DerivesFrom(elementType, messageObjectType))
                    {
                        var arg = invocation.ArgumentList.Arguments.FirstOrDefault();
                        if (arg != null && IsDisallowedExpression(arg.Expression, context.SemanticModel, messageObjectType))
                        {
                            var diag = Diagnostic.Create(Rule, arg.GetLocation(), arg.ToString());
                            context.ReportDiagnostic(diag);
                        }
                    }
                    break;
            }
        }

        private static bool IsDisallowedExpression(
            ExpressionSyntax expr,
            SemanticModel model,
            INamedTypeSymbol messageObjectType)
        {
            var typeInfo = model.GetTypeInfo(expr).Type;
            if (typeInfo == null) return false;

            // 允许 struct
            if (typeInfo.IsValueType)
                return false;
            // 允许 string
            if (typeInfo.SpecialType == SpecialType.System_String)
                return false;
            // 允许 C# readonly struct
            if (typeInfo.IsReadOnly)
                return false;
            // 允许标记为不可变的 MessageObject 子类
            if (DerivesFrom(typeInfo, messageObjectType) &&
                typeInfo.GetAttributes().Any(attr =>
                    attr.AttributeClass.Name.EndsWith("Immutable") ||
                    attr.AttributeClass.Name.EndsWith("ImmutableObjectAttribute") ||
                    attr.AttributeClass.Name.EndsWith("ReadOnly")))
                return false;
            // 允许 ImmutableList<T> 等 System.Collections.Immutable 不可变集合
            if (typeInfo is INamedTypeSymbol namedType)
            {
                var original = namedType.OriginalDefinition;
                if (original.ToString() == "System.Collections.Immutable.ImmutableList<T>")
                    return false;
                if (namedType.AllInterfaces.Any(i =>
                        i.OriginalDefinition.ToString() == "System.Collections.Immutable.IImmutableList<T>"))
                    return false;
            }

            // 直接 MessageObject 或子类实例
            if (DerivesFrom(typeInfo, messageObjectType))
                return true;

            // 来自 MessageObject 或子类的 class 成员访问
            var sym = model.GetSymbolInfo(expr).Symbol;
            var fld = sym as IFieldSymbol;
            var prop = sym as IPropertySymbol;
            if (fld != null || prop != null)
            {
                var memberSym = fld ?? (ISymbol)prop;
                var memberType = fld != null ? fld.Type : prop.Type;
                if (DerivesFrom(memberSym.ContainingType, messageObjectType) && memberType.IsReferenceType)
                    return true;
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