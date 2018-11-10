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

namespace MonoDevelop.Analyzers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GtkLocalizationCodeFixProvider)), Shared]
	sealed class GtkLocalizationCodeFixProvider : CodeFixProvider
	{	
	    private const string title = "Localize";

	    public override ImmutableArray<string> FixableDiagnosticIds
	    {
	        get { return ImmutableArray.Create(AnalyzerIds.GtkLocalizationAnalyzerId); }
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
			var node = root.FindNode(diagnosticSpan);
			if (!(node is LiteralExpressionSyntax literal))
				return;

	        // Register a code action that will invoke the fix.
	        context.RegisterCodeFix(
	            CodeAction.Create(
	                title: title,
	                createChangedDocument: c => LocalizeAsync(context.Document, root, literal, c),
	                equivalenceKey: title),
	            diagnostic);
	    }

	    private Task<Document> LocalizeAsync(Document document, SyntaxNode root, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
	    {
			// TODO: Add support for AddinManager.CurrentLocalizer and TranslationCatalog
			var getTextGetString = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("GettextCatalog"), IdentifierName("GetString"));
			var fullInvocation = InvocationExpression(getTextGetString, ArgumentList(SingletonSeparatedList(Argument(literal))));

			var newRoot = root.ReplaceNode(literal, fullInvocation);
			return Task.FromResult (document.WithSyntaxRoot(newRoot));
	    }
	}
}
