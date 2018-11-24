using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class Vaclavyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor ellipsisDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.EllipsisAnalyzerId,
			"Vaclav typography rules: ellipsis",
			"Vaclav typography rules: ellipsis",
			Category.UIUXDesign,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true
		);
		static readonly DiagnosticDescriptor multiplicationDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.MultiplicationAnalyzerId,
			"Vaclav typography rules: multiplication",
			"Vaclav typography rules: multiplication",
			Category.UIUXDesign,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);
		static readonly DiagnosticDescriptor endashDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.EnDashAnalyzerId,
			"Vaclav typography rules: endash",
			"Vaclav typography rules: endash",
			Category.UIUXDesign,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ellipsisDescriptor);

		static readonly char[] toFind = { '.', '-', 'x' };

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterOperationAction(operationContext =>
			{
				var literal = (ILiteralOperation)operationContext.Operation;
				if (!(literal.ConstantValue.Value is string value))
					return;

				int startIndex = 0;
				while (startIndex <= toFind.Length)
				{
					int found = value.IndexOfAny(toFind, startIndex);
					if (found == -1)
						break;

					found = HandleFindChar(value, found, operationContext);
					startIndex = found + 1;
				}
			}, OperationKind.Literal);
		}

		int HandleFindChar (string value, int index, OperationAnalysisContext context)
		{
			var ch = value[index];
			switch (ch)
			{
				case '.':
					if (value.Length <= index + 2 || value[index + 1] != '.' || value[index + 2] != '.')
						break;

					SyntaxNode syntax = context.Operation.Syntax;
					var adjustedSpan = new TextSpan(syntax.Span.Start + index, 3);
					
					context.ReportDiagnostic(Diagnostic.Create(ellipsisDescriptor, syntax.SyntaxTree.GetLocation(adjustedSpan)));
					return index + 2;
				case '-':
					// look for the characters on the left and on the right
					break;
				case 'x':
					// look for digits on the left and on the right
					break;
			}
			return index;
		}

		//bool IsSurroundedBy (string value, int middle, Func<char> validator)
		//{
		//	int left = middle - 1;
		//	int right = middle + 1;

		//	if (left < 0 || right >= value.Length)
		//		return false;


		//}
	}
}
