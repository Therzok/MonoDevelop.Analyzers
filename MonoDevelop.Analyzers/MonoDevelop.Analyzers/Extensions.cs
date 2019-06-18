using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.Analyzers
{
	static class Extensions
	{
		public static bool IsLiteralOperation(this IOperation value)
		{
			if (value.Kind == OperationKind.Literal)
				return true;

			return value is IBinaryOperation binOp && IsLiteralOperation(binOp.LeftOperand) && IsLiteralOperation(binOp.RightOperand);
		}

		public static INamedTypeSymbol GetContainingTypeOrThis(this ISymbol symbol)
		{
			if (symbol is INamedTypeSymbol type)
				return type;

			return symbol.ContainingType;
		}

		public static bool IsDerivedFromClass(this INamedTypeSymbol type, INamedTypeSymbol baseType)
		{
			//NR5 is returning true also for same type
			for (; type != null; type = type.BaseType)
			{
				if (type.Equals (baseType))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsCatalogType (this INamedTypeSymbol type, INamedTypeSymbol gettextCatalog = null, INamedTypeSymbol translationCatalog = null, INamedTypeSymbol monoUnixCatalog = null, INamedTypeSymbol addinLocalizerType = null, INamedTypeSymbol addinLocalizerInterface = null)
		{
			return (gettextCatalog != null && type.Equals (gettextCatalog)) ||
				(translationCatalog != null && type.Equals (translationCatalog)) ||
				(monoUnixCatalog != null && type.Equals(monoUnixCatalog)) ||
				(addinLocalizerType != null && type.Equals(addinLocalizerType)) ||
				(addinLocalizerInterface != null && type.AllInterfaces.Contains(addinLocalizerInterface));
		}
	}
}
