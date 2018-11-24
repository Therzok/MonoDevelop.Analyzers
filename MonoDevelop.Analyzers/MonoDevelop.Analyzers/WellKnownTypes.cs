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

		public static INamedTypeSymbol MonoUnixCatalog(Compilation compilation)
			=> compilation.GetTypeByMetadataName("Mono.Unix.Catalog");

		public static INamedTypeSymbol AddinLocalizer(Compilation compilation)
			=> compilation.GetTypeByMetadataName("Mono.Addins.AddinLocalizer");

		public static INamedTypeSymbol IAddinLocalizer(Compilation compilation)
			=> compilation.GetTypeByMetadataName("Mono.Addins.Localization.IAddinLocalizer");

		// Deprecated
		//public static INamedTypeSymbol MonoPosixCatalog(Compilation compilation)
			//=> compilation.GetTypeByMetadataName("Mono.Posix.Catalog");
	}
}
