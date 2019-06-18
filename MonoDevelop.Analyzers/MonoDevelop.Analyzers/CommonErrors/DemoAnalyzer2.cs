using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace MonoDevelop.Analyzers.CommonErrors
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DemoAnalyzer2 : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);
		static DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			"DEMO002",
			"All members should be virtual in non-sealed class",
			"All members should be virtual in non-sealed class",
			"Java like",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterSymbolAction(symbolContext =>
			{
				var methodSymbol = (IMethodSymbol)symbolContext.Symbol;
				INamedTypeSymbol typeSymbol = methodSymbol.ContainingType;

				if (methodSymbol.IsVirtual)
					return;

				if (typeSymbol.IsSealed)
					return;

				symbolContext.ReportDiagnostic(
					Diagnostic.Create(
						descriptor,
						methodSymbol.DeclaringSyntaxReferences[0].GetSyntax(symbolContext.CancellationToken).GetLocation()
					)
				);
			}, SymbolKind.Method);
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class Demo2Fix : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DEMO002");

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			context.RegisterCodeFix(new MyCodeAction(context), context.Diagnostics.FirstOrDefault());
			return Task.CompletedTask;
		}

		class MyCodeAction : CodeAction
		{
			public override string Title => "JAVALYZER4000";

			readonly CodeFixContext context;
			public MyCodeAction(CodeFixContext context)
			{
				this.context = context;
			}

			protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var doc = context.Document;
				var root = await doc.GetSyntaxRootAsync(cancellationToken);

				var syntaxGenerator = SyntaxGenerator.GetGenerator(doc);

				var node = root.FindNode(context.Span);

				var initialModifiers = syntaxGenerator.GetModifiers(node);

				var newMOdifiers = initialModifiers.WithIsVirtual(true);
				var newNode = syntaxGenerator.WithModifiers(node, newMOdifiers);

				var newRoot = root.ReplaceNode(node, newNode);

				return doc.WithSyntaxRoot(newRoot);
			}
		}
	}
}
