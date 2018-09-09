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
	}
}
