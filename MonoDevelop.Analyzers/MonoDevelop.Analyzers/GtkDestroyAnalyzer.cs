using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MonoDevelop.Analyzers
{
    // FIXME: Make it internal
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class GtkDestroyAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            AnalyzerIds.GtkDestroyDiagnosticId,
            "Do not override Gtk.Object.Destroy",
            "Override OnDestroyed rather than Destroy - the latter will not run from unmanaged destruction",
            Category.Gtk,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        const string gtkObjectTypeName = "Gtk.Object";
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext => {
                // Limit search to compilations which reference Gtk.
                var compilation = compilationContext.Compilation;
                var gtktype = compilation.GetTypeByMetadataName(gtkObjectTypeName);
                if (gtktype == null)
                    return;

                compilationContext.RegisterSymbolAction(operationContext => {
                    if (!(operationContext.Symbol is INamedTypeSymbol symbol))
                        return;

                    if (symbol.Name == "Widget" && symbol.ContainingNamespace.Name == "Gtk")
                        return;

                    if (!IsGtkObjectDerived(symbol, gtktype))
                        return;

                    var members = symbol.GetMembers("Destroy");
                    foreach (var member in members)
                    {
                        if (!member.IsOverride)
                            continue;

                        var loc = member.Locations.FirstOrDefault(x => x.IsInSource);
                        if (loc != null)
                            operationContext.ReportDiagnostic(Diagnostic.Create(descriptor, loc));
                    }
                }, SymbolKind.NamedType);
            });
        }

        static bool IsGtkObjectDerived(INamedTypeSymbol symbol, INamedTypeSymbol gtkType)
        {
            var type = symbol;
            while (type != null)
            {
                if (type == gtkType)
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}