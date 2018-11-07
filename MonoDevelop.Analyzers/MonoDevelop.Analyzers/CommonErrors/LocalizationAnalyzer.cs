using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class LocalizationConcatenationDiagnosticAnalyzer : DiagnosticAnalyzer
    {
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			AnalyzerIds.GettextConcatenationDiagnosticId,
			"GetString calls should not use concatenation",
			"GetString calls should not use concatenation",
			Category.Gettext,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext => {
                // Limit search to compilations which reference the specific localizers.
                var compilation = compilationContext.Compilation;
				var gettextType = compilation.GetTypeByMetadataName("MonoDevelop.Core.GettextCatalog");
				var translationCatalogType = compilation.GetTypeByMetadataName("Xamarin.Components.Ide.TranslationCatalog");
				var addinsLocalizerType = compilation.GetTypeByMetadataName("Mono.Addins.Localization.IAddinLocalizer");
				if (gettextType == null && translationCatalogType == null && addinsLocalizerType == null)
					return;

                compilationContext.RegisterOperationAction(operationContext => {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    var targetMethod = invocation.TargetMethod;

                    if (targetMethod == null || targetMethod.Name != "GetString")
                        return;

                    var containingType = targetMethod.ContainingType;
                    if (containingType != gettextType && containingType != translationCatalogType)
                    {
                        if (!containingType.AllInterfaces.Contains(addinsLocalizerType))
                            return;
                    }

                    if (invocation.Arguments.Length < 1)
                        return;

                    var phrase = invocation.Arguments[0];
                    if (phrase.Parameter.Type.SpecialType != SpecialType.System_String)
                        return;

                    if (phrase.Value.Kind == OperationKind.Literal)
                        return;

					if (!(phrase.Value is IBinaryOperation) || phrase.Value.IsLiteralOperation ())
                        return;

                    operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, phrase.Syntax.GetLocation()));
                }, OperationKind.Invocation);
            });
        }
    }
}
