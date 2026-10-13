// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_GeneratedValuesCorpus;

public class when_loading_v15_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. GeneratedValuesCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_GENERATED_VALUES_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/GeneratedValues/v15/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v15.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Generated values corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_GENERATED_VALUES_CORPUS.");
        }
    }

    [Fact] void should_select_v15() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V15 && form.Model.SemanticVersion == SemanticVersion.V15).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = GeneratedValuesCorpus.V15;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }

        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_initial_facts_reuse_and_skips_in_every_source_form()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in GeneratedValuesCorpus.V15.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                Assert.True(run.Passed, string.Join('\n', run.Failures));
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected) rejected.Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("GeneratedValues", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();
        return result.Value!;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
