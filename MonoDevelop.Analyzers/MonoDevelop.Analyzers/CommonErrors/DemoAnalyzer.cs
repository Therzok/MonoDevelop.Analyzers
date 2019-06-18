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

namespace MonoDevelop.Analyzers.CommonErrors
{
	[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
	public class SampleAnalyzer : DiagnosticAnalyzer
	{
		public const string analyzerId = "DEMO001";
		const string category = "Quality";

		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			analyzerId,
			"Inherited type needs to be exported.",
			"Inherited type needs to be exported.",
			category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

		public override void Initialize(AnalysisContext context)
		{
			// PERF
			context.EnableConcurrentExecution();

			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(compilationContext =>
			{
				// Don't do analysis on an assembly which is not needed to.
				var baseType = compilationContext.Compilation.GetTypeByMetadataName("TestProject.BaseClass");
				if (baseType == null)
					return;

				var attribute = compilationContext.Compilation.GetTypeByMetadataName("TestProject.MyAttribute");
				if (attribute == null)
					return;

				// Register actual analysis now

				compilationContext.RegisterSymbolAction(symbolContext =>
				{
					var symbol = (INamedTypeSymbol)symbolContext.Symbol;

					// Do some more pre-filtering.
					if (symbol.IsAbstract)
						return;

					// Check it derives from our class.
					if (!symbol.IsDerivedFromClass(baseType))
						return;

					// If we have the attribute already, bail.
					if (symbol.GetAttributes().Any(x => x.AttributeClass == attribute))
						return;

					var syntax = symbol.DeclaringSyntaxReferences[0].GetSyntax(symbolContext.CancellationToken);
					symbolContext.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
				}, SymbolKind.NamedType);
			});
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
	public class SampleCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create (SampleAnalyzer.analyzerId);
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public async override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			SyntaxNode node = root.FindNode(context.Span);
			if (!(node is ClassDeclarationSyntax classDeclaration))
				return;

			var diagnostic = context.Diagnostics.FirstOrDefault();
			if (diagnostic == null)
				return;

			context.RegisterCodeFix(new AddAttributeCodeAction(context, classDeclaration), diagnostic);
		}

		class AddAttributeCodeAction : CodeAction
		{
			public override string Title => "Add MyAttribute";

			readonly CodeFixContext context;
			readonly ClassDeclarationSyntax initialNode;

			public AddAttributeCodeAction(CodeFixContext context, ClassDeclarationSyntax initialNode)
			{
				this.context = context;
				this.initialNode = initialNode;
			}

			protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
			{
				var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

				var attributeName = SyntaxFactory.IdentifierName("TestProject.MyAttribute");
				var attribute = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(attributeName));
				var attributeList = SyntaxFactory.AttributeList(attribute);
				var newNode = initialNode.AddAttributeLists(attributeList);

				return context.Document.WithSyntaxRoot(
					root.ReplaceNode(
						initialNode,
						newNode
					)
				);
			}
		}
	}
}
