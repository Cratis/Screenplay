// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations.given;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static class property_order_binding
{
    public const string View = """
            slice StateView Directory
              readmodel Entry
                tenantId String key
                entryId String key
                zebra String
                alpha String
              query Entries => Entry[]
                performer
                  file ./entries.cs
        """;

    public const string Lifecycle = "\nstatemachine DirectoryLifecycle\n  follows Entry\n  state Present initial final\n";

    public static string LifecycleOnlySource => an_invoice_closing_source.Golden + "\n" + View + Lifecycle;

    public static string Source => "time zone configurable default \"Europe/Oslo\"\n" + LifecycleOnlySource;

    public static string FutureYearSource => """
        concept HorizonReference : String
          generate pattern "{yyyy}-[AB]"
          validate
            matches "^3[0-9]{3}-[AB]$"
        """ + "\n" + Source;

    public static CompilationResult<SemanticCompilation> BindDefault(string? source = null, int utcAssessmentYear = 2026)
    {
        var input = Inputs(source ?? Source);
        return new SemanticModelBinder(utcAssessmentYear).Bind("Billing", input.Document, input.Documents);
    }

    public static SemanticBindingOptionsResult Bind(SemanticBindingOptions options, string? source = null, int utcAssessmentYear = 2026)
    {
        var input = Inputs(source ?? Source);
        return new SemanticModelBinder(utcAssessmentYear).Bind("Billing", input.Document, input.Documents, options);
    }

    public static Ui.ExecutableScreensResult AdmitScreens(string source, int utcAssessmentYear = 2026)
    {
        var input = Inputs(source);
        var bound = new SemanticModelBinder(utcAssessmentYear).Bind("Billing", input.Document, input.Documents);
        if (!bound.Success) throw new InvalidSemanticContract(string.Join('\n', bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return Ui.ExecutableScreens.Admit(input.Document, bound.Value!, utcAssessmentYear);
    }

    static (Syntax.ApplicationSyntax Document, SemanticDocumentSet Documents) Inputs(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var parsed = new ScreenplayCompiler().Parse(source);
        if (!parsed.Success) throw new InvalidSemanticContract(string.Join('\n', parsed.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return (parsed.Value!, SemanticDocumentSet.Create([document], catalog));
    }
}
#endif
