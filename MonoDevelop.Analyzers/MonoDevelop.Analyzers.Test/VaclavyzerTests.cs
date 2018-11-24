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
	public class VaclavyzerTests : CodeFixVerifier
	{

		//No diagnostics expected to show up
		[Test]
		public void TestMethod1()
		{
			var test = @"";

			VerifyCSharpDiagnostic(test);
		}

		//Diagnostic and CodeFix both triggered and checked for
		[Test]
		public void TestMethod2()
		{
			var test = @"class A {
const string A = ""Test..."";
const string B = ""Test ..."";
const string C = ""...Test"";
}";
			VerifyCSharpDiagnostic(test, new DiagnosticResult[]
			{
				new DiagnosticResult {
					Id = AnalyzerIds.EllipsisAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 2, 22),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: ellipsis"
				},
				new DiagnosticResult {
					Id = AnalyzerIds.EllipsisAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 3, 23),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: ellipsis"
				},
				new DiagnosticResult {
					Id = AnalyzerIds.EllipsisAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 4, 18),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: ellipsis"
				},
			});
		}

		[Test]
		public void TestMethod3()
		{
			var test = @"class A {
const string A = ""Test..."";
}";
			var fixedTest = @"class A {
const string A = ""Test…"";
}";
			VerifyCSharpFix(test, fixedTest);
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new VaclavyzerCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new Vaclavyzer();
		}
	}
}