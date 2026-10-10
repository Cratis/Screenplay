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
    internal const string PersonaCallerContext = "policy Member\n  require authenticated and role \"member\"\npersona Accountant\n  policy Member\n" + Slice + Event + "      command Record\n        value String\n        authorize Member\n        produces Recorded\n          value = value\n";

    // Sources describe observable grammar forms, never expected admission, diagnostic or version facts.
    // Every published condition is a binding result for one of these sources, not a prose promise.
    internal static readonly Probe[] Probes =
    [
        new("authentication", "authentication\n  provider GitHub\n"),
        new("identity", "identity\n  department String from claim \"department\"\n", ["identity\n  department String from claim \"department\"\n" + Slice + Event + Command.Replace("value = value", "value = $identity.department", StringComparison.Ordinal)], Baseline: string.Empty),
        new("behavior", "behavior B\n  parameter command\n"),
        new("concept", "concept Value : String\n", ["concept Value : String\n  description \"Value intent\"\n", "concept Value : String @pii\n  pii reason \"Personal data\"\n", "concept Value : String\n  validate csharp\n    ```csharp\n    return true;\n    ```\n"]),
        new("direction", Slice.Replace("StateChange", "Translate", StringComparison.Ordinal) + "      direction inbound\n"),
        new("domain", "domain Example\n"),
        new("eventsource", "concept Id : Uuid\neventsource Record\n  identifier Id\n"),
        new("exposure", "layout Shell\n  content\nexposure for Shell\n  property header.title\n", Baseline: "layout Shell\n  content\n"),
        new("example", Slice + Event + Command + "      example Input : Record\n        value = \"test\"\n      specification Example\n        when Input\n        then Recorded\n          value = \"test\"\n"),
        new("instance", "module M\n  screen template Shell\n    body\nexposure for Shell\n  property header.title\ninstance Shell\n  set header.title = \"Home\"\n", Baseline: "module M\n  screen template Shell\n    body\nexposure for Shell\n  property header.title\n"),
        new("import", "import \"imported.play\"\n", ["import Example\n"], Imported: "concept Value : String\n"),
        new("layout", "layout Shell\n  content\n"),
        new("module", Slice),
        new("numbers", "numbers exact\n"),
        new("persona", "persona Person\n"),
        new("policy", "policy Allowed\n  require authenticated\n", ["policy Allowed\n  description \"Policy intent\"\n  require authenticated\n", "policy Allowed\n  require not authenticated\n", "policy Allowed\n  ```csharp\n  return true;\n  ```\n"]),
        new("public", Slice + "      public event Recorded\n        value String\n"),
        new("purpose", "purpose Billing\n  basis contract\n" + Slice.Replace("module M\n", "module M\n  purpose Billing\n", StringComparison.Ordinal), Baseline: Slice),
        new("seed", Slice + Event + "seed\n  for \"test\"\n    Recorded\n      value = \"test\"\n"),
        new("system", "system External\n"),
        new("template", "layout Shell\n  content\ntemplate Shell\n", Baseline: "layout Shell\n  content\n"),
        new("theme", "theme Theme\n  compatible with Cratis.Components\n"),
        new("trigger", "trigger Changed\n  value String\n"),
        new("type", "type Value\n  value String\n"),
        new("ui", "layout Shell\n  content\nui profile Web\n  target platform web\n  layout Shell\n"),
        new("feature", Slice),
        new("slice", Slice, [Slice.Replace("StateChange", "StateView", StringComparison.Ordinal), Slice.Replace("StateChange", "Automation", StringComparison.Ordinal), Slice.Replace("StateChange", "Translate", StringComparison.Ordinal)]),
        new(
            "capture",
            Slice.Replace("StateChange", "Translate", StringComparison.Ordinal) + Event + "      capture Source\n        key id\n        append Recorded\n          value = $.value\n",
            ["import Outside.Arrived from \"store\"\n" + Slice.Replace("StateChange", "Translate", StringComparison.Ordinal) + "      direction inbound\n" + Event + "      capture Source\n        source events\n          from Arrived\n        key id\n        append Recorded\n          value = $.value\n"]),
        new(
            "command",
            Slice + Event + Command,
            [
                Slice + Event + Command.Replace("value String", "value String\n        validate\n          value rule BeUnique", StringComparison.Ordinal),
                Slice + Event + Command.Replace("value String", "value String\n        id Uuid identifier", StringComparison.Ordinal).Replace("value = value", "for id\n          value = value", StringComparison.Ordinal),
                "concept Id : Uuid\n" + Slice + "      command Record\n        id Id generated\n",
                "concept Id : Uuid\n" + Slice + "      command Record\n        id Id\n        returns id\n",
                "system Outside\n" + Slice + "      operation External\n        uses Outside\n        execute\n          implementation\n            hint \"External work\"\n      command Run\n        produces External\n"
            ]),
        new("constraint", Slice + Event + "      constraint Unique\n        unique value on Recorded\n", [Slice + Event + "      constraint Unique\n        description \"Constraint intent\"\n        unique value on Recorded\n"]),
        new(
            "event",
            Slice + Event,
            [
                Slice + "      event Recorded generation 1\n        value String\n      event Recorded generation 2\n        value String\n",
                Slice + "      event Recorded generation 1\n        value String\n      event Recorded generation 2\n        value String\n" + Command + "      specification Historical\n        given Recorded\n          value = \"previous\"\n        when Record\n          value = \"test\"\n        then Recorded\n          value = \"test\"\n"
            ]),
        new("operation", "system Outside\n" + Slice + "      operation External\n        uses Outside\n        execute\n          implementation\n            hint \"External work\"\n"),
        new("projection", Slice + Event + ReadModel + Projection + KeyedQuery, [Slice + Event + ReadModel + Projection.Replace("      projection View => View\n", "      projection View => View\n        description \"Projection intent\"\n", StringComparison.Ordinal) + KeyedQuery]),
        new("query", Slice + Event + ReadModel + Projection + KeyedQuery, [Slice + ReadModel + "      query All => View[]\n"]),
        new("reaction", Slice.Replace("StateChange", "Automation", StringComparison.Ordinal) + Event + "      reaction FollowUp\n        when Recorded\n          value\n          produces FollowedUp\n            value = value\n      event FollowedUp\n        value String\n"),
        new("readmodel", Slice + Event + ReadModel + Projection + KeyedQuery, [Slice + ReadModel]),
        new("reducer", Slice + Event + ReadModel + KeyedQuery + "      reducer View => View\n        on Recorded\n          ```csharp\n          return context.State;\n          ```\n", [Slice + Event + ReadModel + "      reducer View => View\n        on Recorded\n"]),
        new("screen", Slice + "      screen Screen\n", [Slice + "      screen Screen\n        description \"Screen intent\"\n"]),
        new(
            "specification",
            Slice + Event + Command + "      specification Records\n        when Record\n          value = \"test\"\n        then Recorded\n          value = \"test\"\n",
            [
                PersonaCallerContext + "      specification RecordsAsPersona\n        given caller as Accountant\n        when Record\n          value = \"test\"\n        then Recorded\n          value = \"test\"\n",
                Slice + Event + Command + "      specification RecordsCases\n        parameter value String\n        case One value = \"one\"\n        case Two value = \"two\"\n        when Record value = case.value\n        then Recorded value = case.value\n"
            ],
            VariantBaselines: [PersonaCallerContext, null]),
        new("dialog", "module M\n  dialog template Dialog\n    body\n    actions\n"),
        new("form", Slice + Event + Command + "  form Input for Record\n    field value\n", [Slice + Event + Command + "  form Input for Record\n    description \"Form intent\"\n    field value\n"]),
        new("contribute", "layout Shell\n  navigation contributes Navigation\n  content\nmodule M\n  contribute to Navigation\n    navigate to Screen\n  feature F\n    slice StateView S\n      screen Screen\n")
    ];

    internal static JsonArray Create(IEnumerable<string> keywords, JsonArray versions)
    {
        var result = new JsonArray();
        foreach (var keyword in keywords.Order(StringComparer.Ordinal))
        {
            var probe = Probes.SingleOrDefault(probe => probe.Keyword == keyword) ?? throw new InvalidScreenplayContract($"Construct '{keyword}' lacks an admission probe.");
            var sources = new[] { probe.Source }.Concat(probe.Variants ?? []).ToArray();
            var bound = sources.Select((source, index) => Observe($"{keyword}[{index}]", source, index == 0 ? probe.Imported : null, index == 0 ? probe.Baseline : probe.VariantBaselines?.ElementAtOrDefault(index - 1))).ToArray();
            var cases = new JsonArray();
            for (var index = 0; index < sources.Length; index++)
            {
                cases.Add(new JsonObject
                {
                    ["source"] = sources[index],
                    ["minimumSemanticVersion"] = bound[index].Minimum?.ToString(),
                    ["diagnostic"] = bound[index].Diagnostic?.Code,
                    ["diagnostics"] = new JsonArray([.. bound[index].Diagnostics.Select(diagnostic => (JsonNode?)new JsonObject { ["code"] = diagnostic.Code, ["severity"] = diagnostic.Severity.ToString().ToLowerInvariant() })]),
                    ["admission"] = new JsonArray([.. versions.Select(support => (JsonNode?)Entry(bound[index], support!))])
                });
            }
            var admission = new JsonArray();
            foreach (var support in versions)
            {
                var entry = Entry(bound[0], support!);
                if (entry["status"]!.GetValue<string>() == "admitted" && bound.Skip(1).Any(observation => !Supports(observation, support!)))
                {
                    entry["status"] = "conditional";

                    // These indexes identify the exact bound sources that impose the condition.
                    var refused = bound.Select((observation, index) => (observation, index)).Where(pair => !Supports(pair.observation, support!)).ToArray();
                    entry["condition"] = new JsonArray([.. refused.Select(pair => (JsonNode?)JsonValue.Create(pair.index))]);
                    entry["diagnostic"] = refused.Select(pair => pair.observation.Diagnostic?.Code).FirstOrDefault(code => code is not null);
                }
                admission.Add(entry);
            }
            result.Add(new JsonObject { ["name"] = keyword, ["parseStatus"] = "accepted", ["probes"] = cases, ["admission"] = admission });
        }

        return result;
    }

    internal static Observation Observe(string probe, string source, string? imported = null, string? baseline = null)
    {
        var compilation = Bind(source, imported);
        EnsureValidProbe(probe, compilation);
        var baselineCompilation = baseline is null ? null : Bind(baseline);
        if (baselineCompilation is not null) EnsureValidProbe($"{probe} baseline", baselineCompilation);
        var diagnostics = compilation.Diagnostics.Where(diagnostic => baselineCompilation?.Diagnostics.Contains(diagnostic) != true).ToArray();
        var diagnostic = diagnostics.FirstOrDefault(IsDisposition);
        var metadataOnly = baselineCompilation is not null && compilation.Value?.Model.Revision == baselineCompilation.Value?.Model.Revision;

        return new(compilation.Value?.Model.SemanticVersion, diagnostic, metadataOnly, diagnostics);
    }

    internal static bool Supports(Observation observation, JsonNode support) => !observation.MetadataOnly && observation.Diagnostic is null && observation.Minimum is { } minimum && support["supportedPairs"]!.AsArray().Any(pair => SemanticVersion.Parse(pair!["semanticVersion"]!.GetValue<string>()).IsAtLeast(minimum));

    internal static CompilationResult<SemanticCompilation> Bind(string source, string? imported = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Contract"));
        var documents = new List<WorkspaceDocument>
        {
            WorkspaceDocument.Create("contract", PortablePlayPath.Parse("contract.play"), Encoding.UTF8.GetBytes(source))
        };
        if (imported is not null) documents.Add(WorkspaceDocument.Create("imported", PortablePlayPath.Parse("imported.play"), Encoding.UTF8.GetBytes(imported)));

        // Workspace compilation runs the same merged-source validators and timeline analysis as real models.
        return ScreenplayWorkspace.Create("Contract", [.. documents], catalog).Compilation;
    }

    static bool IsDisposition(Diagnostic diagnostic) => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax || diagnostic.Code == DiagnosticCodes.DeferredSemanticSyntax || diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax;

    static void EnsureValidProbe(string probe, CompilationResult<SemanticCompilation> compilation)
    {
        var errors = compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && !IsDisposition(diagnostic)).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidScreenplayContract($"Admission probe '{probe}' has unexpected source errors: {string.Join("; ", errors.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"))}");
        }
        if (compilation.Value is null && !compilation.Diagnostics.Any(IsDisposition))
        {
            throw new InvalidScreenplayContract($"Admission probe '{probe}' did not bind and has no admission disposition.");
        }
    }

    static JsonObject Entry(Observation observation, JsonNode support) => new()
    {
        ["esmVersion"] = support["schemaVersion"]!.DeepClone(),
        ["status"] = Supports(observation, support) ? "admitted" : "refused",
        ["diagnostic"] = observation.Diagnostic?.Code,
        ["issue"] = ContractSources.Matches(observation.Diagnostic?.Message ?? string.Empty, @"\(#(\d+)\)").Select(number => $"https://github.com/Cratis/Screenplay/issues/{number}").FirstOrDefault()
    };

    internal sealed record Probe(string Keyword, string Source, string[]? Variants = null, string? Baseline = null, string? Imported = null, string?[]? VariantBaselines = null);
    internal sealed record Observation(SemanticVersion? Minimum, Diagnostic? Diagnostic, bool MetadataOnly, Diagnostic[] Diagnostics);
}
