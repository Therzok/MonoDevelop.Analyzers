using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class StringBuilderAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor concatDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.StringBuilderAppendConcatId,
			"StringBuilder Append Optimization",
			"Use offset overloads of StringBuilder for better performance by avoiding string allocations",
			Category.Performance,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		static readonly DiagnosticDescriptor substringDescriptor = new DiagnosticDescriptor(
			AnalyzerIds.StringBuilderAppendSubstringId,
			"StringBuilder Append Optimization",
			"Use offset overloads of StringBuilder for better performance by avoiding string allocations",
			Category.Performance,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(concatDescriptor, substringDescriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterOperationAction(operationContext => {
				if (!(operationContext.Operation is IInvocationOperation op))
					return;

				// Verify it's a stringbuilder
				var callerInstance = op.Instance;
				if (callerInstance == null)
					return;

				var method = op.TargetMethod;
				if (!TryValidateStringBuilderAppend(callerInstance.Type, method))
					return;

				// We have a few cases we want to optimize:
				// sb.Append (a + b);
				// sb.Append (a.Substring(...));
				// TODO: Optimize appendformat to appends - harder
				var arg = op.Arguments[0];
				if (TryValidateAppendConcatenation(arg))
					operationContext.ReportDiagnostic(Diagnostic.Create(concatDescriptor, op.Syntax.GetLocation()));
				else if (TryValidateAppendSubstring(arg))
					operationContext.ReportDiagnostic(Diagnostic.Create(substringDescriptor, op.Syntax.GetLocation()));

			}, OperationKind.Invocation);
		}

		bool TryValidateStringBuilderAppend(ITypeSymbol type, IMethodSymbol method)
		{
			if (type.Name != "StringBuilder" && type.ContainingNamespace.Name != "System")
				return false;

			// Verify it's the append method
			var methodName = method.Name;
			if (methodName != "Append" && methodName != "AppendLine" && methodName != "AppendFormat")
				return false;

			return true;
		}

		bool TryValidateAppendConcatenation(IArgumentOperation argument)
		{
			return argument.Value is IBinaryOperation binOp && binOp.OperatorKind == BinaryOperatorKind.Add && !binOp.IsLiteralOperation ();
		}

		bool TryValidateAppendSubstring(IArgumentOperation argument)
		{
			return argument.Value is IInvocationOperation invocation && invocation.TargetMethod.Name == "Substring";
		}
	}
}