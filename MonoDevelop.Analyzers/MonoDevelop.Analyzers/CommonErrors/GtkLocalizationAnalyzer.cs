using System;
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
			{ "CheckButton", new[] { (0, "label") } },
			{ "Label", new[] { (0, "str") } },
			{ "MenuToolButton", new[] { (1, "label"), } },
			{ "RadioButton", new[] { (0, "label"), (1, "label") } },
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
					var assignment = (IAssignmentOperation)operationContext.Operation;
					if (!(assignment.Target is IPropertyReferenceOperation property))
						return;

					var literalString = assignment.Value;
					if (!IsTranslatableLiteral(literalString, out string value))
						return;

					var type = property.Property.ContainingType;
					var methodName = property.Property.Name;
					if (!TryFindPropertyMapping(type, gtktype, methodName))
						return;

					// We don't necessarily need to translate user input values.
					if (type.Name == "Entry" && methodName == "Text")
						return;

					// Accessibility value so the tooltip role doesn't report a glib warning
					if (type.Name == "Window" && methodName == "Title" && value == "tooltip")
						return;

					operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, literalString.Syntax.GetLocation()));
				}, OperationKind.SimpleAssignment);

				// object creation -> constructor
				compilationContext.RegisterOperationAction(operationContext =>
				{
					var creation = (IObjectCreationOperation)operationContext.Operation;
					if (!(creation.Type is INamedTypeSymbol namedType))
						return;

					if (!namedType.IsDerivedFromClass(gtktype))
						return;

					if (!constructorMapping.TryGetValue(namedType.Name, out var data))
						return;

					var constructorParameters = creation.Constructor.Parameters;
					foreach ((int argPos, string argName) in data)
					{
						if (constructorParameters.Length < argPos)
							continue;

						var param = constructorParameters[argPos];
						if (param.Type.SpecialType == SpecialType.System_String && param.Name == argName) {
							var argValue = creation.Arguments[argPos].Value;
							if (IsTranslatableLiteral(argValue, out string value))
								operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, argValue.Syntax.GetLocation ()));
						}
					}
				}, OperationKind.ObjectCreation);

				// invocation -> method
			});
		}

		static bool IsTranslatableLiteral (IOperation operation, out string value)
		{
			value = null;
			// TODO: Handle string.Format assignment - value = string.Format("<markup>{0}</markup>", "text");
			if (!(operation is ILiteralOperation literal) || literal.Type.SpecialType != SpecialType.System_String)
				return false;

			value = (string)literal.ConstantValue.Value;
			return IsTranslatableString(value);
		}

		static bool IsTranslatableString (string value)
		{
				// Ignore empty strings
			return !string.IsNullOrEmpty(value) &&
				// Ignore gtk stock strings
				!value.StartsWith("gtk-", StringComparison.Ordinal) &&
				// App name should not be localized
				value != "MonoDevelop" &&
				// Check that we have any character that is localizable
				HasTextIgnoringMarkupAttributes(value);
		}

		static bool HasTextIgnoringMarkupAttributes (string value)
		{
			int openAttributeCount = 0;
			foreach (var ch in value)
			{
				if (ch == '<')
					openAttributeCount++;
				else if (ch == '>')
					openAttributeCount--;

				if (openAttributeCount == 0 && char.IsLetter(ch))
					return true;
			}
			return false;
		}

		static bool TryFindPropertyMapping (INamedTypeSymbol symbol, INamedTypeSymbol widgetType, string methodName)
		{
			if (!symbol.IsDerivedFromClass(widgetType))
				return false;

			return whitelistedProperties.Contains(methodName);
		}
	}
}