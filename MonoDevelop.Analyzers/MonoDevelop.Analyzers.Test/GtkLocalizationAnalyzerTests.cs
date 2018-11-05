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
    public class GtkLocalizationAnalyzerTests : CodeFixVerifier
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
namespace Gtk
{
	class Widget {}
	class Label : Widget
	{
		public string Text { get; set; }

		public void Trigger()
		{
			Text = ""label"";
		}
	}

	class GettextCatalog { public static string GetString(string x) => x; }
}";
            var expected1 = new DiagnosticResult
            {
                Id = AnalyzerIds.GtkLocalizationAnalyzerId,
                Message = "Localize strings that are user facing",
				Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 11, 11),
                }
            };

            VerifyCSharpDiagnostic(test, expected1);

			string fixedAssign = @"using System;
namespace Gtk
{
	class Widget {}
	class Label : Widget
	{
		public string Text { get; set; }

		public void Trigger()
		{
			Text = GettextCatalog.GetString(""label"");
		}
	}

	class GettextCatalog { public static string GetString(string x) => x; }
}";
			VerifyCSharpFix(test, fixedAssign);
		}

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new GtkLocalizationCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new GtkLocalizationAnalyzer();
        }
    }
}
