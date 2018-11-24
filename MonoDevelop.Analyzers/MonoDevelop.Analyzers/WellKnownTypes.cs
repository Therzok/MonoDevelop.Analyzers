using System;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Analyzers
{
	static class WellKnownTypes
	{
		public static INamedTypeSymbol TranslationCatalog(Compilation compilation)
			=> compilation.GetTypeByMetadataName("Xamarin.Components.Ide.TranslationCatalog");

		public static INamedTypeSymbol GettextCatalog(Compilation compilation)
			=> compilation.GetTypeByMetadataName("MonoDevelop.Core.GettextCatalog");

		public static INamedTypeSymbol AddinLocalizer(Compilation compilation)
			=> compilation.GetTypeByMetadataName("Mono.Addins.Localization.IAddinLocalizer");
	}
}
