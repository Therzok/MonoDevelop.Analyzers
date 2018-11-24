using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
	abstract class UILocalizationAnalyzer : DiagnosticAnalyzer
	{
		// TODO: Mark strings in user-code as user facing via attribute

		protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }
		protected abstract string CompilationRequiresTypeName { get; }
		protected abstract bool IsTranslatableProperty(string propertyName);
		protected abstract bool IsTranslatableMethodArgument(string typeName, string methodName, out (int, string)[] data);
		protected abstract bool IsTranslatableConstructorArgument(string typeName, out (int, string)[] data);
		protected abstract bool IsFilteredSpecialCase(string typeName, string methodName, string value);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(compilationContext => {
				// Limit search to compilations which reference Gtk.
				var compilation = compilationContext.Compilation;
				var baseType = compilation.GetTypeByMetadataName(CompilationRequiresTypeName);
				if (baseType == null)
					return;

				var translationCatalogType = WellKnownTypes.TranslationCatalog(compilation);
				var gettextType = WellKnownTypes.GettextCatalog(compilation);
				var addinsLocalizerType = WellKnownTypes.AddinLocalizer(compilation);
				if (gettextType == null && translationCatalogType == null && addinsLocalizerType == null)
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
					if (!TryFindPropertyMapping(type, baseType, methodName))
						return;

					if (IsFilteredSpecialCase(type.Name, methodName, value))
						return;

					operationContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, literalString.Syntax.GetLocation()));
				}, OperationKind.SimpleAssignment);

				// object creation -> constructor
				compilationContext.RegisterOperationAction(operationContext =>
				{
					var creation = (IObjectCreationOperation)operationContext.Operation;
					if (!(creation.Type is INamedTypeSymbol namedType))
						return;

					if (!namedType.IsDerivedFromClass(baseType))
						return;

					if (!IsTranslatableConstructorArgument(namedType.Name, out var data))
						return;

					var constructorParameters = creation.Constructor.Parameters;
					foreach ((int argPos, string argName) in data)
					{
						if (constructorParameters.Length <= argPos)
							continue;

						var param = constructorParameters[argPos];
						if (param.Type.SpecialType == SpecialType.System_String && param.Name == argName)
						{
							var argValue = creation.Arguments[argPos].Value;
							if (IsTranslatableLiteral(argValue, out string value))
								operationContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, argValue.Syntax.GetLocation()));
						}
					}
				}, OperationKind.ObjectCreation);

				compilationContext.RegisterOperationAction(operationContext =>
				{
					var invocation = (IInvocationOperation)operationContext.Operation;
					var method = invocation.TargetMethod;
					var containingType = method.ContainingType;
					if (!containingType.IsDerivedFromClass(baseType))
						return;

					if (!IsTranslatableMethodArgument(containingType.Name, method.Name, out var data))
						return;

					var parameters = method.Parameters;
					foreach ((int argPos, string argName) in data)
					{
						if (parameters.Length <= argPos)
							continue;

						var param = parameters[argPos];
						if (param.Type.SpecialType == SpecialType.System_String && param.Name == argName)
						{
							var argValue = invocation.Arguments[argPos].Value;
							if (IsTranslatableLiteral(argValue, out string value))
								operationContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, argValue.Syntax.GetLocation()));
						}
					}
				}, OperationKind.Invocation);
			});
		}

		static bool IsTranslatableLiteral(IOperation operation, out string value)
		{
			value = null;
			// TODO: Handle string.Format assignment - value = string.Format("<markup>{0}</markup>", "text");
			if (!(operation is ILiteralOperation literal) || literal.Type.SpecialType != SpecialType.System_String)
				return false;

			value = (string)literal.ConstantValue.Value;
			return IsTranslatableString(value);
		}

		static bool IsTranslatableString(string value)
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

		static bool HasTextIgnoringMarkupAttributes(string value)
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

		bool TryFindPropertyMapping(INamedTypeSymbol symbol, INamedTypeSymbol baseType, string propertyName)
			=> symbol.IsDerivedFromClass(baseType) && IsTranslatableProperty(propertyName);
	}
}