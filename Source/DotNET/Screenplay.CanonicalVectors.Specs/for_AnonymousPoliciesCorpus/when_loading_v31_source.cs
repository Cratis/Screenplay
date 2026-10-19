// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_AnonymousPoliciesCorpus;

public class when_loading_v31_source : Specification
{
    ExecutableSemanticModel[] _models;

    void Because()
    {
        _models = [.. AnonymousPoliciesCorpus.SourceForms.Select(Compile)];
        foreach (var model in _models)
        {
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            plan.Specifications.Count.ShouldEqual(2);
            foreach (var specification in plan.Specifications.Values)
            {
                var run = new SemanticSpecificationRunner().Run(plan, specification.Id);
                Assert.True(run.Passed, $"{specification.Name}: {string.Join('\n', run.Failures)}");
            }
        }

        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_ANONYMOUS_POLICIES_CORPUS_V31") == "1")
        {
            var root = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/AnonymousPolicies/v31/expected");
            Directory.CreateDirectory(root);
            File.WriteAllBytes(Path.Combine(root, "esm-v31.json"), SemanticModelSerializer.Serialize(_models[0]));
            File.WriteAllText(Path.Combine(root, "semantic-revision.txt"), $"{_models[0].Revision}\n");
            Assert.Fail("Anonymous policies corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_ANONYMOUS_POLICIES_CORPUS_V31.");
        }
    }

    [Fact] void should_select_v31_in_all_four_forms() => _models.Length.ShouldEqual(4);
    [Fact] void should_use_the_admitted_version() => _models.All(model => model.SemanticVersion == SemanticVersion.V31).ShouldBeTrue();
    [Fact] void should_write_the_anonymous_condition() => System.Text.Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_models[0])).ShouldContain("{\"kind\":\"anonymous\"}");

    [Fact]
    void should_pin_every_model_and_its_round_trip()
    {
        var vector = AnonymousPoliciesCorpus.V31;
        foreach (var model in _models)
        {
            model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_pin_normalized_outcomes_and_worlds()
    {
        var vector = AnonymousPoliciesCorpus.V31;
        vector.SpecificationExpectations.Length.ShouldEqual(2);
        foreach (var model in _models)
        {
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            foreach (var expectation in vector.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                    rejected.Details.ShouldEqual(expectation.RejectionMessage);
                }
            }
        }
    }

    static ExecutableSemanticModel Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("AnonymousPolicies", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
