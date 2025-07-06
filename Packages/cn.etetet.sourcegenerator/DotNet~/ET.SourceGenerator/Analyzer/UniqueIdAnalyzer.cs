using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UniqueIdAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>ImmutableArray.Create(UniqueIdRangeAnaluzerRule.Rule,UniqueIdDuplicateAnalyzerRule.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // 改为在编译结束时进行分析，确保能看到所有partial类
            context.RegisterCompilationAction(this.AnalyzeCompilation);
        }
        
        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }

            // 找到所有带UniqueId特性的类型
            var uniqueIdTypes = new List<INamedTypeSymbol>();
            foreach (var assembly in context.Compilation.SourceModule.ReferencedAssemblySymbols.Concat(new[] { context.Compilation.Assembly }))
            {
                foreach (var type in GetAllTypesInAssembly(assembly))
                {
                    if (type.GetFirstAttribute(Definition.UniqueIdAttribute) != null)
                    {
                        uniqueIdTypes.Add(type);
                    }
                }
            }

            // 分析每个UniqueId类型
            foreach (var namedTypeSymbol in uniqueIdTypes)
            {
                AnalyzeUniqueIdType(context, namedTypeSymbol);
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypesInAssembly(IAssemblySymbol assembly)
        {
            return GetAllTypesInNamespace(assembly.GlobalNamespace);
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypesInNamespace(INamespaceSymbol namespaceSymbol)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                yield return type;
                foreach (var nestedType in GetAllNestedTypes(type))
                {
                    yield return nestedType;
                }
            }

            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var type in GetAllTypesInNamespace(childNamespace))
                {
                    yield return type;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nestedType in type.GetTypeMembers())
            {
                yield return nestedType;
                foreach (var doublyNestedType in GetAllNestedTypes(nestedType))
                {
                    yield return doublyNestedType;
                }
            }
        }

        private void AnalyzeUniqueIdType(CompilationAnalysisContext context, INamedTypeSymbol namedTypeSymbol)
        {
            var attr = namedTypeSymbol.GetFirstAttribute(Definition.UniqueIdAttribute);
            if (attr == null) return;

            // 获取id 最小值最大值
            int minId, maxId;
            
            if (attr.ConstructorArguments.Length >= 2)
            {
                // 显式指定了参数
                var minIdValue = attr.ConstructorArguments[0].Value;
                var maxIdValue = attr.ConstructorArguments[1].Value;
                
                if (minIdValue == null || maxIdValue == null)
                {
                    return;
                }
                
                minId = (int)minIdValue;
                maxId = (int)maxIdValue;
            }
            else
            {
                // 使用默认参数值
                minId = int.MinValue;
                maxId = int.MaxValue;
            }

            HashSet<int> IdSet = new HashSet<int>();

            // 获取所有成员（Roslyn会自动合并partial类的成员）
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                if (member is IFieldSymbol { IsConst: true } fieldSymbol && 
                    IsIntegerConstant(fieldSymbol.ConstantValue, out int id))
                {
                    if (id < minId || id > maxId)
                    {
                        ReportDiagnostic(context, fieldSymbol, id, UniqueIdRangeAnaluzerRule.Rule, namedTypeSymbol.Name);
                    }
                    else if (IdSet.Contains(id))
                    {
                        ReportDiagnostic(context, fieldSymbol, id, UniqueIdDuplicateAnalyzerRule.Rule, namedTypeSymbol.Name);
                    }
                    else
                    {
                        IdSet.Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// 检查常量值是否为整数类型，并转换为int
        /// </summary>
        private static bool IsIntegerConstant(object? constantValue, out int id)
        {
            id = 0;
            
            if (constantValue == null)
                return false;
                
            // 支持多种整数类型
            switch (constantValue)
            {
                case int intValue:
                    id = intValue;
                    return true;
                case ushort ushortValue:
                    id = ushortValue;
                    return true;
                case short shortValue:
                    id = shortValue;
                    return true;
                case uint uintValue when uintValue <= int.MaxValue:
                    id = (int)uintValue;
                    return true;
                case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                    id = (int)longValue;
                    return true;
                case ulong ulongValue when ulongValue <= int.MaxValue:
                    id = (int)ulongValue;
                    return true;
                case byte byteValue:
                    id = byteValue;
                    return true;
                case sbyte sbyteValue:
                    id = sbyteValue;
                    return true;
                default:
                    return false;
            }
        }

        private static void ReportDiagnostic(CompilationAnalysisContext context, IFieldSymbol fieldSymbol, int idValue, DiagnosticDescriptor rule, string className)
        {
            foreach (var syntaxReference in fieldSymbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxReference.GetSyntax();
                Diagnostic diagnostic = Diagnostic.Create(rule, syntax.GetLocation(), className, fieldSymbol.Name, idValue.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
        
    }
}

