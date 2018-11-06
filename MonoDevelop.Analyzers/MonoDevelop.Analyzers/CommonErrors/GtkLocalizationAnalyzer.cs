using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MonoDevelop.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class GtkLocalizationAnalyzer : UILocalizationAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			AnalyzerIds.GtkLocalizationAnalyzerId,
			"Localize user facing string",
			"Localize strings that are user facing",
			Category.Gtk,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		protected override DiagnosticDescriptor DiagnosticDescriptor => descriptor;
		protected override string CompilationRequiresTypeName => "Gtk.Widget";

		static readonly HashSet<string> whitelistedProperties = new HashSet<string>
		{
			"ArrowTooltipText", "Label", "Title", "Markup", "MarkupWithMnemonic", "LabelProp", "TooltipText", "TooltipMarkup", "Text"
		};

		static readonly Dictionary<(string, string), (int, string)[]> methodMapping = new Dictionary<(string, string), (int, string)[]>
		{
			{ ("ComboBox", "AppendText"), new[] { (0, "text") } },
			{ ("Notebook", "SetTabLabelText"), new[] { (1, "tab_text") } },
			{ ("Notebook", "SetMenuLabelText"), new[] { (1, "menu_text") } },
			{ ("TreeView", "InsertColumn"), new[] { (1, "title") } },
		};

		static readonly Dictionary<string, (int, string)[]> constructorMapping = new Dictionary<string, (int, string)[]>
		{
			{ "CheckButton", new[] { (0, "label") } },
			{ "Label", new[] { (0, "str") } },
			{ "MenuToolButton", new[] { (1, "label"), } },
			{ "RadioButton", new[] { (0, "label"), (1, "label") } },
			{ "TreeViewColumn", new[] { (0, "title") } },
		};

		protected override bool IsTranslatableProperty(string propertyName) => whitelistedProperties.Contains(propertyName);
		protected override bool IsTranslatableConstructorArgument(string typeName, out (int, string)[] data)
			=> constructorMapping.TryGetValue(typeName, out data);

		protected override bool IsTranslatableMethodArgument(string typeName, string methodName, out (int, string)[] data)
			=> methodMapping.TryGetValue((typeName, methodName), out data);

		protected override bool IsFilteredSpecialCase(string typeName, string methodName, string value)
			// We don't necessarily need to translate user input values.
			=> (typeName == "Entry" && methodName == "Text") ||
				// Accessibility value so the tooltip role doesn't report a glib warning
				(typeName == "Window" && methodName == "Title" && value == "tooltip");
	}
}