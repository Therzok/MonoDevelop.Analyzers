using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MonoDevelop.Analyzers
{
	/// <summary>
	/// A catch clause that catches System.Exception and has an empty body
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EmptyGeneralCatchClauseAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			AnalyzerIds.EmptyCatchAnalyzerId,
			"A catch clause that catches everything without doing anything to the exception",
			"Empty general catch clause suppresses any error",
			Category.Reliability,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterOperationAction(
				(nodeContext) =>
				{
					ReportDiagnostic(nodeContext);
				},
				OperationKind.CatchClause
			);
		}

		static void ReportDiagnostic(OperationAnalysisContext nodeContext)
		{
			var node = (ICatchClauseOperation)nodeContext.Operation;
			// Don't consider a catch clause with "when (...)" as general
			if (node.Filter != null)
				return;

			var exceptionType = node.ExceptionType;
			var isSystemException = exceptionType.SpecialType == SpecialType.System_Object
				|| exceptionType.MetadataName == "Exception"
				&& exceptionType.ContainingNamespace.Name == "System"
				&& exceptionType.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;
			if (!isSystemException || node.Handler.Operations.Any())
				return;

			nodeContext.ReportDiagnostic(Diagnostic.Create(
				descriptor,
				node.Syntax.GetLocation()
			));
		}
	}
}