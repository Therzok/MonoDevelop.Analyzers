using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using MonoDevelop.Analyzers;
using NUnit.Framework;

namespace MonoDevelop.Analyzers.Test
{
	[TestFixture]
	public class EmptyCatchAnalyzerTests : CodeFixVerifier
	{

		//No diagnostics expected to show up
		[Test]
		public void TestMethod1()
		{
			var test = @"using System;
class Object
{
	public void Trigger()
	{
		try {} catch (Exception e) when (e is object) { }
		try {} catch (Exception e) { Console.WriteLine (e); }
		try {} catch (OperationCanceledException e) { }
		try {} catch (System.Reflection.AmbiguousMatchException) { }
	}
}";

			VerifyCSharpDiagnostic(test);
		}

		[Test]
		public void TestMethod2()
		{
			var test = @"using System;
class Object
{
	public void Trigger()
	{
		try {} catch { }
		try {} catch (Exception e) { }
	}
}";
			var expected1 = new DiagnosticResult
			{
				Id = "MD0009",
				Message = "Empty general catch clause suppresses any error",
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 6, 10),
				}
			};
			var expected2 = new DiagnosticResult
			{
				Id = "MD0009",
				Message = "Empty general catch clause suppresses any error",
				Severity = DiagnosticSeverity.Error,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 7, 10),
				}
			};

			VerifyCSharpDiagnostic(test, expected1, expected2);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new EmptyGeneralCatchClauseAnalyzer();
		}
	}
}
