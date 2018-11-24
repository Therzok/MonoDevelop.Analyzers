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
			defaultSeverity: DiagnosticSeverity.Info,
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

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ellipsisDescriptor, multiplicationDescriptor, endashDescriptor);

		static readonly char[] toFind = { '.', '-', 'x', 'X' };

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
			SyntaxNode syntax;
			TextSpan adjustedSpan;

			switch (value[index])
			{
				case '.':
					if (value.Length <= index + 2 || value[index + 1] != '.' || value[index + 2] != '.')
						break;

					syntax = context.Operation.Syntax;
					adjustedSpan = new TextSpan(syntax.Span.Start + index, 3);
					
					context.ReportDiagnostic(Diagnostic.Create(ellipsisDescriptor, syntax.SyntaxTree.GetLocation(adjustedSpan)));
					return index + 2;
				case '-':
					//if (index == 0 || value.Length <= index + 1)
					//	break;

					//if (char.IsWhiteSpace(value[index - 1]) || char.IsWhiteSpace(value[index + 1]))
					//	break;

					//syntax = context.Operation.Syntax;
					//adjustedSpan = new TextSpan(syntax.Span.Start + index, 1);

					//context.ReportDiagnostic(Diagnostic.Create(multiplicationDescriptor, syntax.SyntaxTree.GetLocation(adjustedSpan)));
					break;
				case 'x':
				case 'X':
					if (index == 0 || value.Length <= index + 1)
						break;

					if (!ShouldUseMultiplication(value, index))
						break;

					syntax = context.Operation.Syntax;
					adjustedSpan = new TextSpan(syntax.Span.Start + index, 1);

					context.ReportDiagnostic(Diagnostic.Create(multiplicationDescriptor, syntax.SyntaxTree.GetLocation(adjustedSpan)));
					break;
			}
			return index;
		}

		bool ShouldUseMultiplication (string value, int index)
		{
			bool foundDigit = false;

			for (int i = index - 1; i >= 0; --i)
			{
				var charToFind = value[i];
				if (char.IsWhiteSpace(charToFind))
					continue;

				if (char.IsDigit(charToFind))
				{
					foundDigit = true;
					break;
				}
				return false;
			}

			if (!foundDigit)
				return false;

			foundDigit = false;

			for (int i = index + 1; i < value.Length; ++i)
			{
				var charToFind = value[i];
				if (char.IsWhiteSpace(charToFind))
					continue;

				if (char.IsDigit(charToFind))
				{
					foundDigit = true;
					break;
				}
				return false;
			}

			return foundDigit;
		}
	}
}
