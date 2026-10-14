// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_GenerationSupportCorpus;

public class when_loading_v21_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. GenerationSupportCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_GENERATION_SUPPORT_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/GenerationSupport/v21/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v21.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Generation support corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_GENERATION_SUPPORT_CORPUS.");
        }
    }

    [Fact] void should_select_v21() => _forms.All(form => form.Model.SemanticVersion == SemanticVersion.V21).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = GenerationSupportCorpus.V21;
        _forms.Length.ShouldBeGreaterThan(1);
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_validate_supplied_values_without_executing_a_generator()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in GenerationSupportCorpus.V21.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldBeTrue();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("GenerationSupport", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result.Value!;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
