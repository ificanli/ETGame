#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ModuleAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor FieldWriteRule = new DiagnosticDescriptor(
            "MOD001",
            "跨模块字段写入违规",
            "不能跨模块修改字段，当前模块 '{0}'，被访问模块 '{1}'",
            "ModuleEnforcement",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor FieldAccessRule = new DiagnosticDescriptor(
            "MOD003",
            "跨模块字段访问双向违规",
            "模块 '{0}' 已访问模块 '{1}' 字段，禁止模块 '{1}' 再访问模块 '{0}' 字段",
            "ModuleEnforcement",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MethodCallRule = new DiagnosticDescriptor(
            "MOD002",
            "跨模块方法调用双向引用违规",
            "模块 '{0}' 已调用模块 '{1}'，禁止模块 '{1}' 再调用模块 '{0}'",
            "ModuleEnforcement",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(FieldWriteRule, FieldAccessRule, MethodCallRule);

        private readonly Dictionary<string, HashSet<string>> _fieldAccess = new();
        private readonly Dictionary<string, HashSet<string>> _moduleCalls = new();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(AnalyzeCompilationStart);
        }

        private void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
        {
            var moduleHelper = new ModuleHelper();
            var fieldAccess = new Dictionary<string, HashSet<string>>();
            var moduleCalls = new Dictionary<string, HashSet<string>>();

            // 字段读取（包括 const、enum、普通字段）：统一记录，禁止双向访问
            context.RegisterOperationAction(opContext =>
            {
                var fieldAccessOp = (IFieldReferenceOperation)opContext.Operation;
                var accessingType = opContext.ContainingSymbol?.ContainingType;
                var accessedType = fieldAccessOp.Field?.ContainingType;

                if (accessingType == null || accessedType == null)
                    return;

                var sourceModule = moduleHelper.GetModuleName(accessingType);
                var targetModule = moduleHelper.GetModuleName(accessedType);

                if (sourceModule != targetModule)
                {
                    if (!fieldAccess.TryGetValue(sourceModule, out var targets))
                    {
                        targets = new HashSet<string>();
                        fieldAccess[sourceModule] = targets;
                    }

                    targets.Add(targetModule);

                    if (fieldAccess.TryGetValue(targetModule, out var reverse) && reverse.Contains(sourceModule))
                    {
                        opContext.ReportDiagnostic(Diagnostic.Create(FieldAccessRule, fieldAccessOp.Syntax.GetLocation(), targetModule, sourceModule));
                    }
                }
            }, OperationKind.FieldReference);

            // 字段写入（包括任何字段类型）：禁止跨模块
            context.RegisterOperationAction(opContext =>
            {
                IFieldSymbol fieldSymbol = null;

                switch (opContext.Operation)
                {
                    case IAssignmentOperation assignment when assignment.Target is IFieldReferenceOperation fieldRef:
                        fieldSymbol = fieldRef.Field;
                        break;
                    case IIncrementOrDecrementOperation incDec when incDec.Target is IFieldReferenceOperation fieldRef:
                        fieldSymbol = fieldRef.Field;
                        break;
                }

                if (fieldSymbol == null)
                    return;

                var accessingType = opContext.ContainingSymbol?.ContainingType;
                var accessedType = fieldSymbol.ContainingType;

                if (accessingType == null || accessedType == null)
                    return;

                var sourceModule = moduleHelper.GetModuleName(accessingType);
                var targetModule = moduleHelper.GetModuleName(accessedType);

                if (sourceModule != targetModule)
                {
                    opContext.ReportDiagnostic(Diagnostic.Create(FieldWriteRule, opContext.Operation.Syntax.GetLocation(), sourceModule, targetModule));
                }

            }, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.Increment, OperationKind.Decrement);

            // 方法调用：禁止双向
            context.RegisterOperationAction(opContext =>
            {
                var invocation = (IInvocationOperation)opContext.Operation;
                var callerType = opContext.ContainingSymbol?.ContainingType;
                var calleeType = invocation.TargetMethod?.ContainingType;

                if (callerType == null || calleeType == null)
                    return;

                var callerModule = moduleHelper.GetModuleName(callerType);
                var calleeModule = moduleHelper.GetModuleName(calleeType);

                if (callerModule != calleeModule)
                {
                    if (!moduleCalls.TryGetValue(callerModule, out var targets))
                    {
                        targets = new HashSet<string>();
                        moduleCalls[callerModule] = targets;
                    }

                    targets.Add(calleeModule);

                    if (moduleCalls.TryGetValue(calleeModule, out var reverse) && reverse.Contains(callerModule))
                    {
                        opContext.ReportDiagnostic(Diagnostic.Create(MethodCallRule, invocation.Syntax.GetLocation(), calleeModule, callerModule));
                    }
                }

            }, OperationKind.Invocation);
        }

        private class ModuleHelper
        {
            private readonly Dictionary<INamedTypeSymbol, string> _cache = new(SymbolEqualityComparer.Default);

            public string GetModuleName(INamedTypeSymbol type)
            {
                if (_cache.TryGetValue(type, out var module))
                    return module;

                module = type.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "ModuleAttribute")
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString();

                if (module == null && type.ContainingType != null)
                {
                    module = GetModuleName(type.ContainingType);
                }

                if (module == null)
                {
                    module = "Global";
                }

                _cache[type] = module;
                return module;
            }
        }
    }
}
