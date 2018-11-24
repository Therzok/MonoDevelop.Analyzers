﻿using Microsoft.CodeAnalysis;
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
		public void TestEllipsis()
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
		public void TestEllipsisFix()
		{
			var test = @"class A {
const string A = ""Test..."";
}";
			var fixedTest = @"class A {
const string A = ""Test…"";
}";
			VerifyCSharpFix(test, fixedTest);
		}

		[Test]
		public void TestMultiplication()
		{
			var test = @"class A {
const string A = ""1x2"";
const string B = ""1 x2"";
const string C = ""1ax2"";
}";
			VerifyCSharpDiagnostic(test, new DiagnosticResult[]
			{
				new DiagnosticResult {
					Id = AnalyzerIds.MultiplicationAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 2, 19),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: multiplication"
				},
				new DiagnosticResult {
					Id = AnalyzerIds.MultiplicationAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 3, 20),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: multiplication"
				},
			});
		}

		[Test]
		public void TestEnDash()
		{
			var test = @"class A {
const string B = ""June-July"";
const string C = ""June - July"";
}";
			VerifyCSharpDiagnostic(test, new DiagnosticResult[]
			{
				new DiagnosticResult {
					Id = AnalyzerIds.EnDashAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 2, 22),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: endash"
				},
				new DiagnosticResult {
					Id = AnalyzerIds.EnDashAnalyzerId,
					Locations = new DiagnosticResultLocation[] {
						new DiagnosticResultLocation ("Test0.cs", 3, 23),
					},
					Severity = DiagnosticSeverity.Info,
					Message = "Vaclav typography rules: endash"
				},
			});
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