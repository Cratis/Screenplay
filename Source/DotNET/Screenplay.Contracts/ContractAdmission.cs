// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Contracts;

static class ContractAdmission
{
    internal const string Slice = "module M\n  feature F\n    slice StateChange S\n";
    internal const string Event = "      event Recorded\n        value String\n";
    internal const string Command = "      command Record\n        value String\n        produces Recorded\n          value = value\n";
    internal const string ReadModel = "      readmodel View\n        value String\n";
    internal const string KeyedQuery = "      query ByValue => View optional\n        by value String\n";
    internal const string Projection = "      projection View => View\n        from Recorded\n          value = value\n";

    // The binder chooses a version from bound syntax, not from a requested target. The probe's resulting
    // schema version is the minimum version. Conditional wording is the one explicit construct-level table;
    // its accepted and refused examples are bound in specs and generation, not merely asserted in the golden.
    internal static readonly Probe[] Probes =
    [
        new("authentication", "authentication\n  provider GitHub\n", DiagnosticCodes.ReportOnlySemanticSyntax),
        new("behavior", "behavior B\n  parameter command\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("concept", "concept Value : String\n", Condition: "No compliance attributes or concept require rules; opaque validations require ESM v3.", Refused: "concept Value : String @pii\n  pii reason \"Personal data\"\n"),
        new("domain", "domain Example\n", DiagnosticCodes.ReportOnlySemanticSyntax),
        new("eventsource", "concept Id : Uuid\neventsource Record\n  identifier Id\n", DiagnosticCodes.UnsupportedSemanticSyntax),
        new("example", Slice + Event + Command + "      example Input : Record\n        value = \"test\"\n      specification Example\n        when Input\n        then Recorded\n          value = \"test\"\n", Condition: "Typed examples must expand to assertions admitted by the target ESM version."),
        new("import", "import \"imported.play\"\n", Condition: "File imports compose source before binding; qualified imports are refused.", Refused: "import Example\n", Imported: "concept Value : String\n"),
        new("layout", "layout Shell\n  content\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("module", Slice),
        new("numbers", "numbers exact\n", DiagnosticCodes.UnsupportedSemanticSyntax),
        new("persona", "persona Person\n", DiagnosticCodes.ReportOnlySemanticSyntax),
        new("policy", "policy Allowed\n  require authenticated\n", Condition: "Declarative positive policies; opaque bodies require ESM v3 and negation requires ESM v7."),
        new("seed", Slice + Event + "seed\n  for \"test\"\n    Recorded\n      value = \"test\"\n", DiagnosticCodes.ReportOnlySemanticSyntax),
        new("system", "system External\n", DiagnosticCodes.UnsupportedSemanticSyntax),
        new("template", "layout Shell\n  content\ntemplate Shell\n", MetadataOnly: true, Baseline: "layout Shell\n  content\n"),
        new("theme", "theme Theme\n  compatible with Cratis.Components\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("trigger", "trigger Changed\n  value String\n"),
        new("type", "type Value\n  value String\n", Condition: "Properties use types admitted by the target ESM version."),
        new("ui", "layout Shell\n  content\nui profile Web\n  target platform web\n  layout Shell\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("feature", Slice),
        new("slice", Slice, Condition: "StateChange and StateView; Automation and Translate require ESM v6."),
        new("capture", Slice.Replace("StateChange", "Translate", StringComparison.Ordinal) + Event + "      capture Source\n        key id\n        append Recorded\n          value = $.value\n", Condition: "Capture expressions and correlation must have portable semantics."),
        new("command", Slice + Event + Command, Condition: "Event productions with portable rules; explicit destinations require ESM v2; generated values and responses require ESM v7; operations and stream routes are refused.", Refused: "system Outside\n" + Slice + "      operation External\n        uses Outside\n        execute\n          implementation\n            hint \"External work\"\n      command Run\n        produces External\n"),
        new("constraint", Slice + Event + "      constraint Unique\n        unique value on Recorded\n", Condition: "Declarative constraints; file-based constraints are not executable."),
        new("event", Slice + Event, Condition: "One generation in ESM v1-v3; complete event generation chains require ESM v4; payload types must be portable."),
        new("operation", "system Outside\n" + Slice + "      operation External\n        uses Outside\n        execute\n          implementation\n            hint \"External work\"\n", DiagnosticCodes.UnsupportedSemanticSyntax),
        new("projection", Slice + Event + ReadModel + Projection + KeyedQuery, Condition: "Declarative mappings with resolved keys, events and portable expressions."),
        new("query", Slice + Event + ReadModel + Projection + "      query ByValue => View optional\n        by value String\n", Condition: "One caller-supplied by argument returning an optional read model; collection, filter, scoped, observable and performer forms are refused.", Refused: Slice + ReadModel + "      query All => View[]\n"),
        new("reaction", Slice.Replace("StateChange", "Automation", StringComparison.Ordinal) + Event + "      reaction FollowUp\n        when Recorded\n          value\n          produces FollowedUp\n            value = value\n      event FollowedUp\n        value String\n", Condition: "Portable event/time/trigger reactions; refusal handling and redelivery are not admitted."),
        new("readmodel", Slice + Event + ReadModel + Projection + KeyedQuery, Condition: "Portable property types and exactly one unambiguous keyed query identifying instances."),
        new("reducer", Slice + Event + ReadModel + KeyedQuery + "      reducer View => View\n        on Recorded\n          ```csharp\n          return context.State;\n          ```\n", Condition: "Each transition has an implementation body.", Refused: Slice + Event + ReadModel + "      reducer View => View\n        on Recorded\n"),
        new("screen", Slice + "      screen Screen\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("specification", Slice + Event + Command + "      specification Records\n        when Record\n          value = \"test\"\n        then Recorded\n          value = \"test\"\n", Condition: "Steps must be admitted by the target ESM version; routes, operations, redelivery and then no events are refused."),
        new("dialog", "module M\n  dialog template Dialog\n    body\n    actions\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("form", Slice + Event + Command + "  form Input for Record\n    field value\n", DiagnosticCodes.DeferredSemanticSyntax),
        new("contribute", "layout Shell\n  navigation contributes Navigation\n  content\nmodule M\n  contribute to Navigation\n    navigate to Screen\n  feature F\n    slice StateView S\n      screen Screen\n", DiagnosticCodes.DeferredSemanticSyntax)
    ];

    internal static JsonArray Create(IEnumerable<string> keywords, JsonArray versions)
    {
        var result = new JsonArray();
        foreach (var keyword in keywords.Order(StringComparer.Ordinal))
        {
            var probe = Probes.SingleOrDefault(probe => probe.Keyword == keyword) ?? throw new InvalidScreenplayContract($"Construct '{keyword}' lacks an admission probe.");
            var workspace = Bind(probe.Source, probe.Imported);
            var disposition = workspace.Compilation.Diagnostics.FirstOrDefault(diagnostic => diagnostic.Code == probe.Diagnostic);
            if (probe.Diagnostic is not null && disposition is null) throw new InvalidScreenplayContract($"{keyword} no longer reports {probe.Diagnostic}; update its admission probe.");
            if (probe.Diagnostic is null && !workspace.Compilation.Success) throw new InvalidScreenplayContract($"{keyword} probe does not bind: {string.Join("; ", workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message))}");
            var refusal = probe.Refused is null ? null : Bind(probe.Refused).Compilation.Diagnostics.FirstOrDefault(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
            if (probe.Refused is not null && refusal is null) throw new InvalidScreenplayContract($"{keyword} conditional refusal no longer agrees with the binder.");
            if (probe.MetadataOnly && workspace.Compilation.Value!.Model.Revision != Bind(probe.Baseline!).Compilation.Value!.Model.Revision) throw new InvalidScreenplayContract($"{keyword} is no longer metadata-only; update its admission rule.");
            var minimum = workspace.Compilation.Value?.Model.SemanticVersion;
            var issue = ContractSources.Matches(disposition?.Message ?? refusal?.Message ?? string.Empty, @"\(#(\d+)\)").Select(number => $"https://github.com/Cratis/Screenplay/issues/{number}").FirstOrDefault();
            var admission = new JsonArray();
            foreach (var support in versions)
            {
                var version = support!["schemaVersion"]!.GetValue<int>();
                var supported = minimum is { } semantic && support["supportedPairs"]!.AsArray().Any(pair => SemanticVersion.Parse(pair!["semanticVersion"]!.GetValue<string>()).IsAtLeast(semantic));
                var refused = probe.MetadataOnly || probe.Diagnostic is not null || !supported;
                var status = probe.Condition is not null ? "conditional" : "admitted";
                if (refused) status = "refused";
                var diagnostic = probe.Condition is not null ? DiagnosticCodes.UnsupportedSemanticSyntax : null;
                if (refused) diagnostic = probe.MetadataOnly ? null : probe.Diagnostic ?? DiagnosticCodes.UnsupportedSemanticSyntax;
                var entry = new JsonObject
                {
                    ["esmVersion"] = version,
                    ["status"] = status,
                    ["diagnostic"] = diagnostic,
                    ["issue"] = issue
                };
                if (!refused && probe.Condition is not null) entry["condition"] = probe.Condition;
                admission.Add(entry);
            }
            result.Add(new JsonObject { ["name"] = keyword, ["parseStatus"] = "accepted", ["admission"] = admission });
        }

        return result;
    }

    internal static ScreenplayWorkspace Bind(string source, string? imported = null)
    {
        var parsed = new ScreenplayCompiler().Compile(source);
        if (parsed.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) throw new InvalidScreenplayContract($"Admission probe does not parse: {string.Join("; ", parsed.Diagnostics.Select(diagnostic => diagnostic.Message))}");

        var documents = new List<WorkspaceDocument> { WorkspaceDocument.Create("contract", PortablePlayPath.Parse("contract.play"), Encoding.UTF8.GetBytes(source)) };
        if (imported is not null) documents.Add(WorkspaceDocument.Create("imported", PortablePlayPath.Parse("imported.play"), Encoding.UTF8.GetBytes(imported)));

        return ScreenplayWorkspace.Create("Contract", [.. documents], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Contract")));
    }

    internal sealed record Probe(string Keyword, string Source, string? Diagnostic = null, string? Condition = null, string? Refused = null, bool MetadataOnly = false, string? Baseline = null, string? Imported = null);
}
