using System.Collections.Generic;
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
		static readonly Dictionary<string, string[]> propertyMapping = new Dictionary<string, string[]>
		{
			{ "Button", new[] { "Label" } },
			{ "ColorButton", new[] { "Title" } },
			{ "FontButton", new[] { "Title" } },
			{ "Frame", new[] { "Label" } },
			{ "MenuToolButton", new[] { "ArrowTooltipText" } },
			{ "Label", new[] { "Text", "Markup", "MarkupWithMnemonic", "LabelProp" } },
			{ "ProgressBar", new[] { "Text" } },
			{ "ToolButton", new[] { "Label" } },
			{ "Widget", new[] { "TooltipText", "TooltipMarkup" } },
			// Gtk.FileChooser
		};

		static readonly HashSet<string> whitelistedProperties = new HashSet<string>
		{
			"ArrowTooltipText", "Label", "Title", "Markup", "MarkupWithMnemonic", "LabelProp", "TooltipText", "TooltipMarkup", "Text"
		};

		// methodMapping
		// { "Gtk.Notebook", new HashSet<string> { "SetTabLabelText", "SetMenuLabelText" }} / tab_text, menu_text
		// { "Gtk.TreeView", new HashSet<string> { "InsertColumn" }, "title"
		// { "Gtk.ComboBox", new HashSet<string> { InsertText }}, "text"

		static readonly Dictionary<string, (int, string)[]> constructorMapping = new Dictionary<string, (int, string)[]>
		{
			{ "Gtk.CheckButton", new[] { (1, "label") } },
			{ "Gtk.Label", new[] { (1, "str") } },
			{ "Gtk.MenuToolButton", new[] { (2, "label"), } },
			{ "Gtk.RadioButton", new[] { (1, "label"), (2, "label") } },
		};

		const string gtkLabelTypeName = "Gtk.Label";
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(compilationContext => {
				// Limit search to compilations which reference Gtk.
				var compilation = compilationContext.Compilation;
				var gtktype = compilation.GetTypeByMetadataName("Gtk.Widget");
				if (gtktype == null)
					return;

				compilationContext.RegisterOperationAction(operationContext =>
				{
					// blacklist gtk.window.title

					var assignment = (IAssignmentOperation)operationContext.Operation;

					if (!(assignment.Target is IPropertyReferenceOperation property))
						return;

					// Handle string.Format assignment - value = string.Format("<markup>{0}</markup>", "text");
					if (!(assignment.Value is ILiteralOperation literal) || assignment.Type.SpecialType != SpecialType.System_String)
						return;

					string value = (string)literal.ConstantValue.Value;
					if (string.IsNullOrWhiteSpace (value))
						return;

					if (value.StartsWith("gtk-", System.StringComparison.Ordinal))
						return;

					if (!value.Any(x => char.IsLetter(x)))
						return;

					if (value == "MonoDevelop")
						return;

					if (!TryFindPropertyMapping(property.Property.ContainingType, gtktype, property.Property.Name))
						return;

					operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, literal.Syntax.GetLocation()));
				}, OperationKind.SimpleAssignment);
			});
		}

		static bool TryFindPropertyMapping (INamedTypeSymbol symbol, INamedTypeSymbol widgetType, string methodName)
		{
			if (!symbol.IsDerivedFromClass(widgetType))
				return false;

			return whitelistedProperties.Contains(methodName);
		}
	}
}