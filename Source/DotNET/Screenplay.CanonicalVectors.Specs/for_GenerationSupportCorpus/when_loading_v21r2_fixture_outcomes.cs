// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_GenerationSupportCorpus;

public class when_loading_v21r2_fixture_outcomes : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. GenerationFixturesCorpus.SourceFormsV21R2.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_GENERATION_FIXTURES_V21R2_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/GenerationFixtures/v21r2/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v21.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Generation fixture v21r2 corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_GENERATION_FIXTURES_V21R2_CORPUS.");
        }
    }

    [Fact]
    void should_pin_the_new_revision_bytes_and_strict_round_trip()
    {
        var vector = GenerationFixturesCorpus.V21R2;
        _forms.Length.ShouldEqual(2);
        foreach (var form in _forms)
        {
            form.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_preserve_v21_semantic_bytes_and_revision()
    {
        GenerationFixturesCorpus.V21R2.EsmBytes.SequenceEqual(GenerationFixturesCorpus.V21.EsmBytes).ShouldBeTrue();
        GenerationFixturesCorpus.V21R2.SemanticRevision.ShouldEqual(GenerationFixturesCorpus.V21.SemanticRevision);
    }

    [Fact]
    void should_change_only_the_generation_body_in_each_source_form()
    {
        var original = GenerationFixturesCorpus.SourceForms;
        var revised = GenerationFixturesCorpus.SourceFormsV21R2;
        for (var index = 0; index < original.Length; index++)
        {
            var source = original[index].Documents.Single().Text;
            source.ShouldContain("return value;");
            revised[index].Documents.Single().Text.ShouldEqual(source.Replace("return value;", "return $\"{context.Tenant}:order_{context.Now:yyyyMMdd}-{Guid.NewGuid():N}\";", StringComparison.Ordinal));
            revised[index].Documents.Single().DisplayPath.ShouldEqual(original[index].Documents.Single().DisplayPath);
        }
    }

    [Fact]
    void should_match_all_clock_checksum_numeric_implementation_and_freshness_outcomes()
    {
        var vector = GenerationFixturesCorpus.V21R2;
        vector.SpecificationExpectations.Length.ShouldEqual(10);
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in vector.SpecificationExpectations)
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
