// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_Documentation;

public class when_starting_new_models : Specification
{
    readonly Dictionary<string, ApplicationSyntax> _examples = [];
    readonly List<Diagnostic> _errors = [];

    void Because()
    {
        foreach (var path in new[] { "screenplay/getting-started.md", "screenplay/mcp/create.md" })
        {
            var text = File.ReadAllText(Path.Combine(DocumentationExamples.Root(), path));
            var source = text.Split("```screenplay\n", StringSplitOptions.None)[1].Split("```", StringSplitOptions.None)[0];
            AddSource(path, source);
        }
        AddProposals();
    }

    void AddProposals()
    {
        var authoring = File.ReadAllText(Path.Combine(DocumentationExamples.Root(), "screenplay/mcp/authoring-tools.md"));
        var proposals = authoring.Split("```json\n", StringSplitOptions.None).Skip(1)
            .Select(block => block.Split("```", StringSplitOptions.None)[0]);
        foreach (var proposal in proposals)
        {
            using var json = JsonDocument.Parse(proposal);
            if (json.RootElement.ValueKind != JsonValueKind.Array) continue;
            foreach (var document in json.RootElement.EnumerateArray().Where(document => document.TryGetProperty("operation", out var operation) && operation.GetString() == "create-document"))
            {
                AddDocument(document);
            }
        }
    }

    void AddDocument(JsonElement document)
    {
        if (document.TryGetProperty("source", out var source)) AddSource("propose-source", source.GetString()!);
        if (document.TryGetProperty("node", out var node)) _examples.Add("propose-ast", (ApplicationSyntax)SyntaxJson.Deserialize(node));
    }

    void AddSource(string path, string source)
    {
        var result = new ScreenplayCompiler().Compile(source);
        _errors.AddRange(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        if (result.Value is { } application) _examples.Add(path, application);
    }

    [Fact] void should_compile_the_new_model_sources() => _errors.ShouldBeEmpty();
    [Fact] void should_find_both_quick_starts_and_both_proposal_paths() => _examples.Count.ShouldEqual(4);
    [Fact] void should_start_each_model_with_protected_reads() => _examples.Where(example => example.Value.CommandReadMode != CommandReadMode.Protected).Select(example => example.Key).ShouldBeEmpty();
}
