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
			"Vaclav typography rules: ellipsis (\\u2026)",
			"Vaclav typography rules: ellipsis (\\u2026)",
			Category.UIUXDesign,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true
		);
		static readonly DiagnosticDescriptor multiplicationDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.MultiplicationAnalyzerId,
			"Vaclav typography rules: multiplication (\\u00D7)",
			"Vaclav typography rules: multiplication (\\u00D7)",
			Category.UIUXDesign,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true
		);
		static readonly DiagnosticDescriptor endashDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.EnDashAnalyzerId,
			"Vaclav typography rules: endash (\\u2013)",
			"Vaclav typography rules: endash (\\u2013)",
			Category.UIUXDesign,
			defaultSeverity: DiagnosticSeverity.Info,
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
					if (index == 0 || value.Length <= index + 1)
						break;

					if (!ShouldUseEnDash(value, index))
						break;

					syntax = context.Operation.Syntax;
					adjustedSpan = new TextSpan(syntax.Span.Start + index, 1);

					context.ReportDiagnostic(Diagnostic.Create(endashDescriptor, syntax.SyntaxTree.GetLocation(adjustedSpan)));
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

		bool SkipWhitespace (string value, int index, out int left, out int right)
		{
			left = index - 1;
			right = index + 1;

			while (left >= 0)
			{
				var ch = value[left];
				if (!char.IsWhiteSpace(ch))
					break;
				left--;
			}

			if (left < 0)
				return false;

			var length = value.Length;
			while (right < length)
			{
				var ch = value[right];
				if (!char.IsWhiteSpace(ch))
					break;
				right++;
			}

			if (right >= value.Length)
				return false;

			return true;
		}

		bool ShouldUseEnDash (string value, int index)
		{
			return SkipWhitespace(value, index, out int left, out int right) &&
				char.IsLetter(value[left]) && char.IsLetter(value[right]);
		}

		bool ShouldUseMultiplication (string value, int index)
		{
			return SkipWhitespace(value, index, out int left, out int right) &&
				char.IsDigit(value[left]) && char.IsDigit(value[right]);
		}
	}
}
