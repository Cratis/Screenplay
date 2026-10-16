// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Diagnostics.for_DiagnosticCodes;

public class when_validating_argument_contracts : Specification
{
    readonly List<string> _failures = [];
    int _folders;
    int _boundFolders;
    int _vectors;
    int _diagnostics;

    void Because()
    {
        var root = Path.GetDirectoryName(DocumentationExamples.Root())!;
        var corpus = Path.Combine(root, "Source", "DotNET", "Screenplay.CanonicalCorpus", "Corpus");
        var folders = Directory.EnumerateDirectories(Path.Combine(root, "Samples"))
            .Concat(Directory.EnumerateDirectories(corpus, "source", SearchOption.AllDirectories)).ToArray();
        _folders = folders.Length;
        foreach (var folder in folders) CheckFolder(folder);
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance", "diagnostics.json")));
        foreach (var vector in vectors.RootElement.GetProperty("cases").EnumerateArray())
        {
            _vectors++;
            var source = string.Join('\n', vector.GetProperty("source").EnumerateArray().Select(line => line.GetString()));
            var compiler = new ScreenplayCompiler();
            Check(vector.TryGetProperty("validate", out var validate) && validate.GetBoolean() ? compiler.Compile(source).Diagnostics : compiler.Parse(source).Diagnostics);
        }
    }

    void CheckFolder(string folder)
    {
        var compilation = new PlayFileCompiler().CompileFolder(folder);
        Check(compilation.Result.Diagnostics);
        if (!compilation.Result.Success) return;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ArgumentContracts"));
        var sources = compilation.Sources.Select((source, index) =>
        {
            var key = $"document-{index}";
            return SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, source.File.RelativePath, source.Source);
        });
        var documents = SemanticDocumentSet.Create([.. sources], catalog);
        _boundFolders++;
        Check(new SemanticModelBinder().Bind("ArgumentContracts", compilation.Result.Value!, documents).Diagnostics);
    }

    void Check(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            _diagnostics++;
            var definitions = CompilerContractCatalog.Diagnostics.Single(fact => fact.Code == diagnostic.Code).Arguments;
            foreach (var required in definitions.Where(argument => !argument.Optional && !diagnostic.Arguments.ContainsKey(argument.Name))) _failures.Add($"{diagnostic.Code} missing {required.Name}");
            foreach (var (name, value) in diagnostic.Arguments)
            {
                var definition = definitions.SingleOrDefault(argument => argument.Name == name);
                if (definition is null || definition.Kind != value.Kind) _failures.Add($"{diagnostic.Code} undeclared or mistyped {name}");
                else if (value.Kind == "keyword" && !definition.Values.Contains(value.Value.GetString(), StringComparer.Ordinal)) _failures.Add($"{diagnostic.Code} {name} has unknown keyword {value.Value}");
            }
        }
    }

    [Fact] void should_cover_samples_and_canonical_source_folders() => _folders.ShouldBeGreaterThan(4);
    [Fact] void should_bind_assembled_samples_and_canonical_source_folders() => _boundFolders.ShouldBeGreaterThan(4);
    [Fact] void should_cover_the_shared_vectors() => _vectors.ShouldBeGreaterThan(100);
    [Fact] void should_examine_emitted_diagnostics() => _diagnostics.ShouldBeGreaterThan(100);
    [Fact] void should_hold_every_emission_to_its_declaration() => _failures.ShouldBeEmpty();
    [Fact] void should_explain_every_declared_argument() => CompilerContractCatalog.Diagnostics.SelectMany(fact => fact.Arguments).All(argument => !string.IsNullOrWhiteSpace(argument.Meaning)).ShouldBeTrue();
    [Fact] void should_declare_closed_keyword_sets_only_for_keywords() => CompilerContractCatalog.Diagnostics.SelectMany(fact => fact.Arguments).All(argument => argument.Kind == "keyword" == (argument.Values.Count > 0)).ShouldBeTrue();
}
