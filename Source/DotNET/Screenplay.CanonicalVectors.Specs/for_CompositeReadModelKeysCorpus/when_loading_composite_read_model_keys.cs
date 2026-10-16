// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_loading_composite_read_model_keys : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. CompositeReadModelKeysCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_COMPOSITE_READ_MODEL_KEYS_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/CompositeReadModelKeys/v27/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v27.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Composite read-model key corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_COMPOSITE_READ_MODEL_KEYS_CORPUS.");
        }
    }

    [Fact] void should_select_the_admitting_version() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V27 && form.Model.SemanticVersion == SemanticVersion.V27).ShouldBeTrue();
    [Fact] void should_exercise_five_equivalent_source_forms() => _forms.Length.ShouldEqual(5);

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = CompositeReadModelKeysCorpus.V27;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_each_query_projection_removal_and_validation_outcome()
    {
        var expectations = CompositeReadModelKeysCorpus.V27.SpecificationExpectations;
        expectations.Length.ShouldEqual(7);
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in expectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                Assert.True(run.Passed, string.Join('\n', run.Failures));
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                    rejected.Details.ShouldEqual(expectation.RejectionMessage);
                    rejected.ValidationFailures.Length.ShouldEqual(1);
                }
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var result = CompileSource(form);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();

        return result.Value!;
    }

    internal static CompilationResult<SemanticCompilation> CompileSource(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));

        return new SemanticModelCompiler().Compile("CompositeReadModelKeys", SemanticDocumentSet.Create([.. documents], catalog));
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
