using System;
using TestHelper;
using NUnit.Framework;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoDevelop.Analyzers.CommonErrors;

namespace MonoDevelop.Analyzers.Test
{
	[TestFixture]
	public class DemoAnalyzer2Tests : CodeFixVerifier
	{
		[Test]
		public void TestDemo()
		{
			var source = @"	class MyClassIsNotSealed
{
	public void DoStuff()
	{

	}
}";
			VerifyCSharpDiagnostic(source, new DiagnosticResult
			{
				Id = "DEMO002",
				Locations = new DiagnosticResultLocation[] { new DiagnosticResultLocation("a", 1, 1) },
				Message = "thing"
				
			});
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new DemoAnalyzer2();
		}
	}
}
