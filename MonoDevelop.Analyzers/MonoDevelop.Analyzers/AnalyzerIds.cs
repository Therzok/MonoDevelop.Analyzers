using System;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Analyzers
{
    class AnalyzerIds
    {
        public const string GettextCrawlerDiagnosticId = "MD0001";
        public const string GtkDestroyDiagnosticId = "MD0002";
		public const string StringBuilderAppendConcatId = "MD0003";
		public const string StringBuilderAppendSubstringId = "MD0004";
		public const string GtkLocalizationAnalyzerId = "MD0005";
		public const string EllipsisAnalyzerId = "MD0006";
		public const string MultiplicationAnalyzerId = "MD0007";
		public const string EnDashAnalyzerId = "MD0008";
	}
}
