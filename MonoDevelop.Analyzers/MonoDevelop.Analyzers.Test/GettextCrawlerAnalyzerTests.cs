using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using MonoDevelop.Analyzers;
using NUnit.Framework;
using System.Linq;

namespace MonoDevelop.Analyzers.Test
{
    [TestFixture]
    public class BaseLocalizationAnalyzerTests : CodeFixVerifier
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

namespace MonoDevelop.Core
{
	static class GettextCatalog
	{
		public static string GetString(string phrase) => phrase;
		public static string GetString(string phrase, object arg1) => string.Format(phrase, arg1);
		public static string GetStringPlural(string phrase) => phrase;
	}
}

namespace Mono.Addins.Localization
{
	public interface IAddinLocalizer { string GetString(string phrase); }
}

namespace testfsw
{
	using MonoDevelop.Core;
	class AddinLocalizer : Mono.Addins.Localization.IAddinLocalizer
	{
		public string GetString(string phrase) => phrase;
	}

	class MainClass
	{
		static void Main()
		{
			var localizer = new AddinLocalizer();
			string a = ""test"";

			// Fine
			GettextCatalog.GetString(""asdf"");
			GettextCatalog.GetString(@""bsdf"");
			GettextCatalog.GetString(@""csdf"" + ""csdf"");
			GettextCatalog.GetString(""dsdf"" + ""dsdf"");
			GettextCatalog.GetString(""{0} stuff"", ""thing"");
			localizer.GetString(""a"");
			GettextCatalog.GetString(""{0} stuff"" + ""a"", ""thing"");

			// Shows errors.
			GettextCatalog.GetString(a);
			GettextCatalog.GetString($""dsdf"");
			GettextCatalog.GetString($""{a}"");
			localizer.GetString(a);
			GettextCatalog.GetString(""asdf"" + ""asdf"" + a);
			GettextCatalog.GetStringPlural(a);
		}
	}
}";
            var diagnostics = GetLocations().Select(tuple =>
           {
               int line = tuple.Line;
               int col = tuple.Column;

               return new DiagnosticResult
               {
                   Id = "MD0001",
                   Message = "GetString calls should only use literal strings",
				   Severity = DiagnosticSeverity.Error,
                   Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", line, col),
                }
               };
           }).ToArray ();

            VerifyCSharpDiagnostic(test, diagnostics);

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

		(int Line, int Column)[] GetLocations()
		{
			return new[] { (43, 29), (44, 29), (45, 29), (46, 24), (47, 29), (48, 35) };
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new GettextCrawlerAnalyzer();
		}

		//protected override CodeFixProvider GetCSharpCodeFixProvider()
		//{
		//    return new MonoDevelopAnalyzersCodeFixProvider();
		//}
	}
}
