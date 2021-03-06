﻿using System;
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
			if (diagnostic == null)
				return;

	        var diagnosticSpan = diagnostic.Location.SourceSpan;

			// Find the type declaration identified by the diagnostic.
			var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
			if (!(node is LiteralExpressionSyntax literal))
				return;

			var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
			var compilation = semanticModel.Compilation;

			if (WellKnownTypes.TranslationCatalog(compilation) != null)
			{
				RegisterCodeFix(context, IdentifierName("TranslationCatalog"), root, literal);
			}
			if (WellKnownTypes.GettextCatalog(compilation) != null)
			{
				RegisterCodeFix(context, IdentifierName("GettextCatalog"), root, literal);
			}
			if (WellKnownTypes.AddinLocalizer(compilation) != null)
			{
				var memberAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("AddinManager"), IdentifierName("CurrentLocalizer"));
				RegisterCodeFix(context, memberAccess, root, literal);
			}
			if (WellKnownTypes.MonoUnixCatalog(compilation) != null)
			{
				RegisterCodeFix(context, IdentifierName("Catalog"), root, literal);
			}
		}

		static void RegisterCodeFix (CodeFixContext context, ExpressionSyntax name, SyntaxNode root, LiteralExpressionSyntax literal)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: title,
					createChangedDocument: c => LocalizeAsync(name, context.Document, root, literal, c),
					equivalenceKey: title),
				context.Diagnostics.First());
		}

		static private Task<Document> LocalizeAsync(ExpressionSyntax catalog, Document document, SyntaxNode root, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
	    {
			var getTextGetString = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, catalog, IdentifierName("GetString"));
			var fullInvocation = InvocationExpression(getTextGetString, ArgumentList(SingletonSeparatedList(Argument(literal))));

			var newRoot = root.ReplaceNode(literal, fullInvocation);
			return Task.FromResult (document.WithSyntaxRoot(newRoot));
	    }
	}
}
