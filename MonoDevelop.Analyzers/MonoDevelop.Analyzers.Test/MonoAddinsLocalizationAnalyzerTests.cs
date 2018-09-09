using Microsoft.CodeAnalysis.Diagnostics;

namespace MonoDevelop.Analyzers.Test
{
    public class MonoAddinsLocalizationAnalyzer : BaseLocalizationAnalyzerTests
    {
        protected override (int, int)[] GetLocations()
        {
            return new[] { (45, 24),  };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MonoAddinsConcatenationDiagnosticAnalyzer();
        }

        //protected override CodeFixProvider GetCSharpCodeFixProvider()
        //{
        //    return new MonoDevelopAnalyzersCodeFixProvider();
        //}
    }
}
