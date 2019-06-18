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
	using MonoDevelop.Core;

	class Widget {}
	class Label : Widget
	{
		public string Text { get; set; }

		public void Trigger()
		{
			Text = ""label"";
		}
	}
}

namespace MonoDevelop.Core
{
	class GettextCatalog { public static string GetString(string x) => x; }
}
";
            var expected1 = new DiagnosticResult
            {
                Id = AnalyzerIds.GtkLocalizationAnalyzerId,
                Message = "Localize strings that are user facing",
				Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 13, 11),
                }
            };

            VerifyCSharpDiagnostic(test, expected1);

			string fixedAssign = @"using System;
namespace Gtk
{
	using MonoDevelop.Core;

	class Widget {}
	class Label : Widget
	{
		public string Text { get; set; }

		public void Trigger()
		{
			Text = GettextCatalog.GetString(""label"");
		}
	}
}

namespace MonoDevelop.Core
{
	class GettextCatalog { public static string GetString(string x) => x; }
}
";
			VerifyCSharpFix(test, fixedAssign);
		}

		[Test]
		public void TestConstructor()
		{
			var test = @"using System;
namespace Gtk
{
	class Widget {}
	class CheckButton : Widget
	{
		public CheckButton (string label) {}

		public void Trigger()
		{
			new CheckButton (""label"");
		}
	}
}

namespace MonoDevelop.Core
{
	class GettextCatalog { public static string GetString(string x) => x; }
}

namespace Xamarin.Components.Ide
{
	class TranslationCatalog { public static string GetString(string x) => x; }
}

namespace Mono.Addins
{
	class AddinLocalizer { public static string GetString(string x) => x; }
	class AddinManager { public static IAddinLocalizer CurrentLocalizer { get; } = null; }
}
";
			VerifyCSharpDiagnostic(test, new DiagnosticResult
			{
				Id = AnalyzerIds.GtkLocalizationAnalyzerId,
				Message = "Localize strings that are user facing",
				Severity = DiagnosticSeverity.Warning,
				Locations = new[]
				{
					new DiagnosticResultLocation("Test0.cs", 11, 21),
				}
			});

			string fixedConstructor = @"using System;
namespace Gtk
{
	class Widget {}
	class CheckButton : Widget
	{
		public CheckButton (string label) {}

		public void Trigger()
		{
			new CheckButton (__REPLACE__.GetString(""label""));
		}
	}
}

namespace MonoDevelop.Core
{
	class GettextCatalog { public static string GetString(string x) => x; }
}

namespace Xamarin.Components.Ide
{
	class TranslationCatalog { public static string GetString(string x) => x; }
}

namespace Mono.Addins
{
	class AddinLocalizer { public static string GetString(string x) => x; }
	class AddinManager { public static IAddinLocalizer CurrentLocalizer { get; } = null; }
}
";
			VerifyCSharpFix(test, fixedConstructor.Replace("__REPLACE__", "TranslationCatalog"), 0);
			VerifyCSharpFix(test, fixedConstructor.Replace("__REPLACE__", "GettextCatalog"), 1);
			VerifyCSharpFix(test, fixedConstructor.Replace("__REPLACE__", "AddinManager.CurrentLocalizer"), 2);
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
