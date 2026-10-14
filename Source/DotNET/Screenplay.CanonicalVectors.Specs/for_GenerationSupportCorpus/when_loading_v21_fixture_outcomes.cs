// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_GenerationSupportCorpus;

public class when_loading_v21_fixture_outcomes : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. GenerationFixturesCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_GENERATION_FIXTURES_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/GenerationFixtures/v21/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v21.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Generation fixture corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_GENERATION_FIXTURES_CORPUS.");
        }
    }

    [Fact]
    void should_pin_only_the_new_fixture_model_bytes()
    {
        foreach (var form in _forms)
        {
            form.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
            form.Model.Revision.ShouldEqual(GenerationFixturesCorpus.V21.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(GenerationFixturesCorpus.V21.EsmBytes).ShouldBeTrue();
        }
    }

    [Fact]
    void should_match_all_clock_checksum_numeric_implementation_and_freshness_outcomes()
    {
        GenerationFixturesCorpus.V21.SpecificationExpectations.Length.ShouldEqual(10);
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in GenerationFixturesCorpus.V21.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldBeTrue();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected) rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("GenerationFixtures", SemanticDocumentSet.Create([.. documents], catalog));
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
