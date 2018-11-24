using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Analyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GtkLocalizationCodeFixProvider)), Shared]
	sealed class VaclavyzerCodeFixProvider : CodeFixProvider
	{	
	    private const string title = "Localize";

	    public override ImmutableArray<string> FixableDiagnosticIds
	    {
	        get { return ImmutableArray.Create(AnalyzerIds.EllipsisAnalyzerId); }
	    }

	    public override FixAllProvider GetFixAllProvider()
	    {
	        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
	        return WellKnownFixAllProviders.BatchFixer;
	    }

	    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	    {
	        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

	        var diagnostic = context.Diagnostics.FirstOrDefault();
	        var diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the type declaration identified by the diagnostic.
			var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
			if (!(node is LiteralExpressionSyntax literal))
				return;

			string typographySymbol;
			switch (diagnostic.Descriptor.Id)
			{
				case AnalyzerIds.EllipsisAnalyzerId:
					typographySymbol = "\u2026";
					break;
				case AnalyzerIds.EnDashAnalyzerId:
					typographySymbol = "\u2013";
					break;
				case AnalyzerIds.MultiplicationAnalyzerId:
					typographySymbol = "\u00D7";
					break;
				default:
					return;
			}

			// Register a code action that will invoke the fix.
			context.RegisterCodeFix(
	            CodeAction.Create(
	                title: title,
	                createChangedDocument: c => LocalizeAsync(context.Document, root, literal, diagnosticSpan, typographySymbol, c),
	                equivalenceKey: title),
	            diagnostic);
	    }

	    private Task<Document> LocalizeAsync(Document document, SyntaxNode root, LiteralExpressionSyntax literal, TextSpan diagnosticSpan, string typographySymbol, CancellationToken cancellationToken)
	    {
			int offsetInString = diagnosticSpan.Start - literal.Span.Start;

			var initialText = literal.Token.ValueText;
			var changedText = initialText.Remove(offsetInString, diagnosticSpan.Length).Insert(offsetInString, typographySymbol);

			var newRoot = root.ReplaceNode(literal, LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(changedText)));
			return Task.FromResult (document.WithSyntaxRoot(newRoot));
	    }
	}
}
