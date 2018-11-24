using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class GettextCrawlerAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			AnalyzerIds.GettextCrawlerDiagnosticId,
			"GetString calls should only use literal strings",
			"GetString calls should only use literal strings",
			Category.Gettext,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(compilationContext =>
			{
				// false positive list:
				// ComponentModelLocalization:40
				// 
				// Limit search to compilations which reference the specific localizers.
				var compilation = compilationContext.Compilation;
				var translationCatalogType = WellKnownTypes.TranslationCatalog(compilation);
				var gettextType = WellKnownTypes.GettextCatalog(compilation);
				var addinsLocalizerType = WellKnownTypes.AddinLocalizer(compilation);
				var addinsLocalizerInterface = WellKnownTypes.IAddinLocalizer(compilation);
				var unixCatalog = WellKnownTypes.MonoUnixCatalog(compilation);
				if (gettextType == null && translationCatalogType == null && addinsLocalizerType == null && addinsLocalizerInterface == null && unixCatalog == null)
					return;

				compilationContext.RegisterOperationAction(operationContext =>
				{
					// if we're in a catalog context, do not flag
					var symbol = operationContext.ContainingSymbol;
					if (symbol.Name == "GetString" || symbol.Name == "GetPluralString") {
						if (symbol.GetContainingTypeOrThis().IsCatalogType(gettextType, translationCatalogType, unixCatalog, addinsLocalizerType, addinsLocalizerInterface))
							return;
					}

					var invocation = (IInvocationOperation)operationContext.Operation;
					var targetMethod = invocation.TargetMethod;

					if (targetMethod == null || (targetMethod.Name != "GetString" && targetMethod.Name != "GetStringPlural"))
						return;

					var containingType = targetMethod.ContainingType;
					if (!containingType.IsCatalogType(gettextType, translationCatalogType, unixCatalog, addinsLocalizerType, addinsLocalizerInterface))
						return;

					if (invocation.Arguments.Length < 1)
						return;

					var phrase = invocation.Arguments[0];
					if (phrase.Parameter.Type.SpecialType != SpecialType.System_String)
						return;

					if (phrase.Value.IsLiteralOperation())
						return;

					operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, phrase.Syntax.GetLocation()));
				}, OperationKind.Invocation);
			});
		}
	}
}
