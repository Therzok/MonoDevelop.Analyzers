using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MonoDevelop.Analyzers
{
	// TODO: Atk localization, 
	// TODO: Cocoa localization
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class GtkLocalizationAnalyzer : UILocalizationAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			AnalyzerIds.GtkLocalizationAnalyzerId,
			"Localize user facing string",
			"Localize strings that are user facing",
			Category.Gtk,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);
		protected override DiagnosticDescriptor DiagnosticDescriptor => descriptor;
		protected override string CompilationRequiresTypeName => "Gtk.Widget";

		// TODO: FileChooser, RecentChooser unhandled
		// Tooltip inherits from GLib.Object, not Gtk.Object, so it needs to be whitelisted
		static readonly HashSet<string> whitelistedProperties = new HashSet<string>
		{
			"ArrowTooltipText", "Label", "Title", "Markup", "MarkupWithMnemonic", "LabelProp", "TooltipText", "TooltipMarkup", "Text", "TearoffTitle", "PreviewText", "SecondaryText"
		};

		static readonly Dictionary<(string, string), (int, string)[]> methodMapping = new Dictionary<(string, string), (int, string)[]>
		{
			{ ("CellView", "NewWithText"), new[] { (0, "text") } },
			{ ("ComboBox", "AppendText"), new[] { (0, "text") } },
			{ ("ComboBox", "InsertText"), new[] { (1, "text") } },
			{ ("ComboBox", "PrependText"), new[] { (0, "text") } },
			{ ("Dialog", "AddButton"), new[] { (0, "button_text") } },
			{ ("Entry", "InsertText"), new[] { (0, "new_text") } },
			{ ("Expander", "New"), new[] { (0, "label") } },
			{ ("Label", "New"), new[] { (0, "label") } },
			{ ("Notebook", "SetTabLabelText"), new[] { (1, "tab_text") } },
			{ ("Notebook", "SetMenuLabelText"), new[] { (1, "menu_text") } },
			{ ("Statusbar", "Push"), new[] { (1, "text") } },
			{ ("ToggleButton", "NewWithLabel"), new[] { (0, "label") } },
			{ ("TreeView", "InsertColumn"), new[] { (1, "title") } },
		};

		static readonly Dictionary<string, (int, string)[]> constructorMapping = new Dictionary<string, (int, string)[]>
		{
			// ComboBox (string[])
			{ "AccelLabel", new[] { (0, "str1ng") } },
			{ "Button", new[] { (0, "label") } },
			{ "CellView", new[] { (0, "markup") } },
			{ "CheckButton", new[] { (0, "label") } },
			{ "CheckMenuItem", new[] { (0, "label") } },
			{ "ColorSelectionDialog", new[] { (0, "title") } },
			{ "Dialog", new[] { (0, "title") } },
			{ "Entry", new[] { (0, "initial_text") } },
			{ "FontSelectionDialog", new[] { (0, "title") } },
			{ "Frame", new[] { (0, "label") } },
			{ "ImageMenuItem", new[] { (0, "label"), (0, "stock_id") } },
			{ "Label", new[] { (0, "str") } },
			{ "LinkButton", new[] { (1, "label") } },
			{ "MenuItem", new[] { (0, "label"), } },
			{ "MenuToolButton", new[] { (1, "label"), (0, "stock_id") } },
			{ "PageSetupUnixDialog", new[] { (0, "title"), } },
			{ "RadioAction", new[] { (1, "label"), (2, "tooltip") } },
			{ "RadioButton", new[] { (0, "label"), (1, "label") } },
			{ "RadioMenuItem", new[] { (0, "label"), (1, "label") } },
			{ "RadioToolButton", new[] { (1, "stock_id") } },
			{ "ToggleAction", new[] { (1, "label"), (2, "tooltip") } },
			{ "ToggleButton", new[] { (0, "label") } },
			{ "ToggleToolButton", new[] { (0, "stock_id") } },
			{ "ToolButton", new[] { (0, "stock_id"), (1, "label") } },
			{ "TreeViewColumn", new[] { (0, "title") } },
			{ "Window", new[] { (0, "title") } },
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