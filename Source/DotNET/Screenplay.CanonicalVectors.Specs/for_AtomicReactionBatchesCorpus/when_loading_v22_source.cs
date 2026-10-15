// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_AtomicReactionBatchesCorpus;

public class when_loading_v22_source : Specification
{
    SemanticCompilation[] _atomic;
    SemanticCompilation[] _legacy;

    void Because()
    {
        _atomic = [.. AtomicReactionBatchesCorpus.SourceForms.Select(Compile)];
        _legacy = [.. AtomicReactionBatchesCorpus.LegacySourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_ATOMIC_REACTION_BATCHES_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/AtomicReactionBatches/v22/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v22.json"), SemanticModelSerializer.Serialize(_atomic[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_atomic[0].Model.Revision}\n");
            File.WriteAllBytes(Path.Combine(expected, "esm-legacy.json"), SemanticModelSerializer.Serialize(_legacy[0].Model));
            File.WriteAllText(Path.Combine(expected, "legacy-revision.txt"), $"{_legacy[0].Model.Revision}\n");
            Assert.Fail("Atomic reaction batch corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_ATOMIC_REACTION_BATCHES_CORPUS.");
        }
    }

    [Fact] void should_select_v22_only_with_the_mode() => _atomic.All(form => form.Model.SemanticVersion == SemanticVersion.V22).ShouldBeTrue();
    [Fact] void should_retain_v19_without_the_mode() => _legacy.All(form => form.Model.SemanticVersion == SemanticVersion.V19).ShouldBeTrue();

    [Fact]
    void should_pin_both_contracts_and_every_equivalent_source_form()
    {
        foreach (var (forms, vector) in new[] { (_atomic, AtomicReactionBatchesCorpus.V22), (_legacy, AtomicReactionBatchesCorpus.Legacy) })
        {
            foreach (var form in forms)
            {
                form.Model.Revision.ShouldEqual(vector.SemanticRevision);
                SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
            }
            SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
    }

    [Fact]
    void should_pin_outcomes_worlds_and_unchanged_projections()
    {
        foreach (var (forms, vector) in new[] { (_atomic, AtomicReactionBatchesCorpus.V22), (_legacy, AtomicReactionBatchesCorpus.Legacy) })
        {
            foreach (var form in forms)
            {
                var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
                foreach (var expectation in vector.SpecificationExpectations)
                {
                    var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                    Assert.True(run.Passed == expectation.Passed, $"{vector.Name}/{expectation.Name}: {string.Join('\n', run.Failures)}");
                    run.Execution.Kind.ShouldEqual(expectation.Outcome);
                    run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                    if (run.Execution is SemanticRejected rejected)
                    {
                        rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                        rejected.Details.ShouldEqual(expectation.RejectionMessage);
                    }
                    if (form.Model.Application.ReactionProductionMode is not null && expectation.RejectionMessage is not null)
                    {
                        run.Execution.World.ReadModels.ShouldBeEmpty();
                        run.Execution.World.PendingChildren.ShouldBeEmpty();
                    }
                }
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("AtomicReactionBatches", SemanticDocumentSet.Create([.. documents], catalog));
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
