using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
    //[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    sealed class GettextConcatenationDiagnosticAnalyzer : LocalizationConcatenationDiagnosticAnalyzer
    {
        protected override string TypeName => "MonoDevelop.Core.GettextCatalog";
    }

    //[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    sealed class MonoAddinsConcatenationDiagnosticAnalyzer : LocalizationConcatenationDiagnosticAnalyzer
    {
        protected override string TypeName => "Mono.Addins.Localization.IAddinLocalizer";
    }

    //[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    sealed class TranslationCatalogConcatenationDiagnosticAnalyzer : LocalizationConcatenationDiagnosticAnalyzer
    {
        protected override string TypeName => "Xamarin.Components.Ide.TranslationCatalog";
    }

    abstract class LocalizationConcatenationDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            AnalyzerIds.GettextConcatenationDiagnosticId,
            "GetString calls should not use concatenation",
			"GetString calls should not use concatenation",
			Category.Gettext,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        protected abstract string TypeName { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext => {
                // Limit search to compilations which reference the specific localizers.
                var compilation = compilationContext.Compilation;
                var type = compilation.GetTypeByMetadataName(TypeName);
                if (type == null)
                    return;

                compilationContext.RegisterOperationAction(operationContext => {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    var targetMethod = invocation.TargetMethod;

                    if (targetMethod == null || targetMethod.Name != "GetString")
                        return;

                    var containingType = targetMethod.ContainingType;
                    if (containingType != type)
                    {
                        // Check if we're looking for an interface type.
                        if (type.TypeKind != TypeKind.Interface)
                            return;

                        if (!containingType.AllInterfaces.Contains(type))
                            return;
                    }

                    if (invocation.Arguments.Length < 1)
                        return;

                    var phrase = invocation.Arguments[0];
                    if (phrase.Parameter.Type.SpecialType != SpecialType.System_String)
                        return;

                    if (phrase.Value.Kind == OperationKind.Literal)
                        return;

                    if (phrase.Value.IsLiteralOperation ())
                        return;

                    operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, phrase.Syntax.GetLocation()));
                }, OperationKind.Invocation);
            });
        }
    }
}
