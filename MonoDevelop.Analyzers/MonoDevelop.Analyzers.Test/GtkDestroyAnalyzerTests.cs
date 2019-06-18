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
    public class GtkDestroyAnalyzerTests : CodeFixVerifier
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
	class Object
	{
		public virtual void Destroy() { }
	}

	class Widget : Object
	{
		public override void Destroy()
		{
			base.Destroy();
		}
	}
}

class MyWidget : Gtk.Widget
{
	public override void Destroy()
	{
		base.Destroy();
	}
}

class MyOtherWidget : MyWidget
{
	public override void Destroy()
	{
		base.Destroy();
	}
}";
            var expected1 = new DiagnosticResult
            {
                Id = "MD0002",
                Message = "Override OnDestroyed rather than Destroy - the latter will not run from unmanaged destruction",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 20, 23),
                }
            };
            var expected2 = new DiagnosticResult
            {
                Id = "MD0002",
                Message = "Override OnDestroyed rather than Destroy - the latter will not run from unmanaged destruction",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 28, 23),
                }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);

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
            return new EmptyCatchAnalyzer();
        }
    }
}
