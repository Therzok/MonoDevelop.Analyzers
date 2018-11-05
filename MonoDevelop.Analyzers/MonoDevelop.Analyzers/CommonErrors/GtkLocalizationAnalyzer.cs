using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class GtkLocalizationAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			AnalyzerIds.GtkLocalizationAnalyzerId,
			"Localize user facing string",
			"Localize strings that are user facing",
			Category.Gtk,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		// TODO: Mark strings in user-code as user facing via attribute

		const string gtkLabelTypeName = "Gtk.Label";
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(compilationContext => {
				// Limit search to compilations which reference Gtk.
				var compilation = compilationContext.Compilation;
				var gtktype = compilation.GetTypeByMetadataName(gtkLabelTypeName);
				if (gtktype == null)
					return;

				compilationContext.RegisterOperationAction(operationContext =>
				{
					var assignment = (IAssignmentOperation)operationContext.Operation;

					if (!(assignment.Target is IPropertyReferenceOperation property))
						return;

					if (!(assignment.Value is ILiteralOperation literal) || assignment.Type.SpecialType != SpecialType.System_String)
						return;

					var propertyTypeSymbol = property.Property.ContainingType;
					if (!propertyTypeSymbol.IsDerivedFromClass(gtktype))
						return;

					if (property.Property.Name == "Text")
					{
						operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, literal.Syntax.GetLocation()));
					}
				}, OperationKind.SimpleAssignment);
			});
		}
	}
}