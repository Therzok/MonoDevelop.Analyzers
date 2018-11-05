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
    public abstract class BaseLocalizationAnalyzerTests : CodeFixVerifier
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
		}
	}
}";
            var diagnostics = GetLocations().Select(tuple =>
           {
               int line = tuple.line;
               int col = tuple.col;

               return new DiagnosticResult
               {
                   Id = "MD0001",
                   Message = "GetString calls should not use concatenation",
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

        protected abstract (int line, int col)[] GetLocations();

        //protected override CodeFixProvider GetCSharpCodeFixProvider()
        //{
        //    return new MonoDevelopAnalyzersCodeFixProvider();
        //}
    }
}
