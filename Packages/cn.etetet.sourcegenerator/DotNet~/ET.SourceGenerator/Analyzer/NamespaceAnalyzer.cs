#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ET0112";
        public const string DependencyDiagnosticId = "ET0113";
        public const string ClientServerDependencyDiagnosticId = "ET0114";
        
        private static readonly LocalizableString Title = "命名空间不匹配";
        private static readonly LocalizableString MessageFormat = "类 '{0}' 在目录 '{1}' 中，应使用命名空间 '{2}'，但实际使用的是 '{3}'";
        private static readonly LocalizableString Description = "根据目录结构检查类的命名空间是否正确.";
        
        private static readonly LocalizableString DependencyTitle = "禁止的命名空间依赖";
        private static readonly LocalizableString DependencyMessageFormat = "ET命名空间不能依赖 '{0}' 命名空间";
        private static readonly LocalizableString DependencyDescription = "ET命名空间（Share代码）不能使用ET.Client或ET.Server命名空间的类型.";
        
        private static readonly LocalizableString ClientServerDependencyTitle = "禁止Client调用Server";
        private static readonly LocalizableString ClientServerDependencyMessageFormat = "ET.Client命名空间不能依赖'{0}'命名空间";
        private static readonly LocalizableString ClientServerDependencyDescription = "ET.Client命名空间（客户端代码）不能使用ET.Server命名空间的类型.";
        
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);
            
        private static readonly DiagnosticDescriptor DependencyRule = new DiagnosticDescriptor(
            DependencyDiagnosticId,
            DependencyTitle,
            DependencyMessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: DependencyDescription);
            
        private static readonly DiagnosticDescriptor ClientServerDependencyRule = new DiagnosticDescriptor(
            ClientServerDependencyDiagnosticId,
            ClientServerDependencyTitle,
            ClientServerDependencyMessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: ClientServerDependencyDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule, DependencyRule, ClientServerDependencyRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeClassWithoutNamespace, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        }

        private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
            var actualNamespace = namespaceDeclaration.Name.ToString();
            AnalyzeNamespaceDeclaration(context, actualNamespace, namespaceDeclaration.GetLocation());
        }


        private static void AnalyzeClassWithoutNamespace(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            
            // 检查类是否在命名空间内
            var parent = classDeclaration.Parent;
            bool hasNamespace = false;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax)
                {
                    hasNamespace = true;
                    break;
                }
                parent = parent.Parent;
            }

            if (!hasNamespace)
            {
                var filePath = context.Node.SyntaxTree.FilePath;
                var expectedNamespace = GetExpectedNamespace(filePath);
                
                if (!string.IsNullOrEmpty(expectedNamespace))
                {
                    var className = classDeclaration.Identifier.ValueText;
                    var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(),
                        className, GetRelativePath(filePath), expectedNamespace, "全局命名空间");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeNamespaceDeclaration(SyntaxNodeAnalysisContext context, string actualNamespace, Location location)
        {
            var filePath = context.Node.SyntaxTree.FilePath;
            var expectedNamespace = GetExpectedNamespace(filePath);
            
            if (!string.IsNullOrEmpty(expectedNamespace) && actualNamespace != expectedNamespace)
            {
                var diagnostic = Diagnostic.Create(Rule, location,
                    "类", GetRelativePath(filePath), expectedNamespace, actualNamespace);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string GetExpectedNamespace(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var normalizedPath = filePath.Replace('\\', '/');
            
            // Client目录规则：ET.Client
            if (IsInClientDirectory(normalizedPath))
                return "ET.Client";
            
            // Server目录规则：ET.Server
            if (IsInServerDirectory(normalizedPath))
                return "ET.Server";
            
            // Share目录规则：ET
            if (IsInShareDirectory(normalizedPath))
                return "ET";
            
            return null;
        }

        private static bool IsInClientDirectory(string normalizedPath)
        {
            return normalizedPath.Contains("/Packages/") && (
                normalizedPath.Contains("/Scripts/Model/Client/") ||
                normalizedPath.Contains("/Scripts/Hotfix/Client/") ||
                normalizedPath.Contains("/Scripts/ModelView/Client/") ||
                normalizedPath.Contains("/Scripts/HotfixView/Client/")
            );
        }

        private static bool IsInServerDirectory(string normalizedPath)
        {
            return normalizedPath.Contains("/Packages/") && (
                normalizedPath.Contains("/Scripts/Model/Server/") ||
                normalizedPath.Contains("/Scripts/Hotfix/Server/")
            );
        }

        private static bool IsInShareDirectory(string normalizedPath)
        {
            return normalizedPath.Contains("/Packages/") && (
                normalizedPath.Contains("/Scripts/Model/Share/") ||
                normalizedPath.Contains("/Scripts/Hotfix/Share/")
            );
        }

        private static string GetRelativePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";
            
            var packagesIndex = filePath.IndexOf("/Packages/", StringComparison.OrdinalIgnoreCase);
            if (packagesIndex >= 0)
                return filePath.Substring(packagesIndex);
            
            return Path.GetFileName(filePath);
        }
        
        private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            var usingDirective = (UsingDirectiveSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            var normalizedPath = filePath?.Replace('\\', '/') ?? "";
            
            var namespaceName = usingDirective.Name?.ToString();
            if (namespaceName == null) return;
            
            // ET命名空间（Share）不能使用ET.Client或ET.Server
            if (IsInShareDirectory(normalizedPath))
            {
                if (namespaceName == "ET.Client" || namespaceName == "ET.Server" || 
                    namespaceName.StartsWith("ET.Client.") || namespaceName.StartsWith("ET.Server."))
                {
                    var diagnostic = Diagnostic.Create(DependencyRule, usingDirective.GetLocation(), namespaceName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            // ET.Client命名空间不能使用ET.Server
            else if (IsInClientDirectory(normalizedPath))
            {
                if (namespaceName == "ET.Server" || namespaceName.StartsWith("ET.Server."))
                {
                    var diagnostic = Diagnostic.Create(ClientServerDependencyRule, usingDirective.GetLocation(), namespaceName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        
        private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifierName = (IdentifierNameSyntax)context.Node;
            var filePath = context.Node.SyntaxTree.FilePath;
            var normalizedPath = filePath?.Replace('\\', '/') ?? "";
                
            var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName);
            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
                if (namespaceName == null) return;
                
                // ET命名空间（Share）不能使用ET.Client或ET.Server
                if (IsInShareDirectory(normalizedPath))
                {
                    if (namespaceName == "ET.Client" || namespaceName == "ET.Server" ||
                        namespaceName.StartsWith("ET.Client.") || namespaceName.StartsWith("ET.Server."))
                    {
                        var diagnostic = Diagnostic.Create(DependencyRule, identifierName.GetLocation(), namespaceName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                // ET.Client命名空间不能使用ET.Server
                else if (IsInClientDirectory(normalizedPath))
                {
                    if (namespaceName == "ET.Server" || namespaceName.StartsWith("ET.Server."))
                    {
                        var diagnostic = Diagnostic.Create(ClientServerDependencyRule, identifierName.GetLocation(), namespaceName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}