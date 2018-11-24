using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using MonoDevelop.Analyzers;
using NUnit.Framework;

namespace MonoDevelop.Analyzers.Test
{
	//[TestFixture]
	public class StringBuilderAnalyzerTests : CodeFixVerifier
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
			var test = @"using System;
using System.Text;
class Object
{
	public void TestStringBuilder()
	{
		string a = ""def""
		var sb = new StringBuilder();
		sb.Append(""abc"");
		sb.Append(""abc"" + a);
		sb.Append(""abc"" + ""def"");
		sb.Append(""abc"".Substring(0));
		sb.Append(""abc"".Substring(0, 1));
		sb.Append(""abc"".Substring(0, 1) + a);
	}
}";
			var expected = new DiagnosticResult[] {
				new DiagnosticResult
				{
					Id = AnalyzerIds.StringBuilderAppendConcatId,
					Message = "Avoid concatenating non-constant strings by using multiple Append calls",
					Severity = DiagnosticSeverity.Warning,
					Locations = new[] {
						new DiagnosticResultLocation("Test0.cs", 10, 3),
					}
				},
				new DiagnosticResult
				{
					Id = AnalyzerIds.StringBuilderAppendSubstringId,
					Message = "Use offset overloads of StringBuilder for better performance by avoiding string allocations",
					Severity = DiagnosticSeverity.Warning,
					Locations = new[] {
						new DiagnosticResultLocation("Test0.cs", 12, 3),
					}
				},
				new DiagnosticResult
				{
					Id = AnalyzerIds.StringBuilderAppendSubstringId,
					Message = "Use offset overloads of StringBuilder for better performance by avoiding string allocations",
					Severity = DiagnosticSeverity.Warning,
					Locations = new[] {
						new DiagnosticResultLocation("Test0.cs", 13, 3),
					}
				},
				new DiagnosticResult
				{
					Id = AnalyzerIds.StringBuilderAppendConcatId,
					Message = "Avoid concatenating non-constant strings by using multiple Append calls",
					Severity = DiagnosticSeverity.Warning,
					Locations = new[] {
						new DiagnosticResultLocation("Test0.cs", 14, 3),
					}
				}
			};


			VerifyCSharpDiagnostic(test, expected);

			//        var fixtest = @"
			//using System;
			//using System.Collections.Generic;
			//using System.Linq;
			//using System.Text;
			//using System.Threading.Tasks;
			//using System.Diagnostics;

			//namespace ConsoleApplication1
			//{
			//    class TYPENAME
			//    {   
			//    }
			//}";
			//        VerifyCSharpFix(test, fixtest);
		}

		//protected override CodeFixProvider GetCSharpCodeFixProvider()
		//{
		//    return new MonoDevelopAnalyzersCodeFixProvider();
		//}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new StringBuilderAnalyzer();
		}
	}
}
