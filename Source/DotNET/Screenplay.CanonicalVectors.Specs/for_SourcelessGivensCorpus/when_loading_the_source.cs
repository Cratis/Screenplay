// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SourcelessGivensCorpus;

public class when_loading_the_source : Specification
{
    ExecutableSemanticModel[] _models;

    void Because()
    {
        _models = [.. SourcelessGivensCorpus.SourceForms.Select(Compile)];

        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_SOURCELESS_GIVENS_CORPUS") == "1")
        {
            var root = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/SourcelessGivens/expected");
            Directory.CreateDirectory(root);
            File.WriteAllBytes(Path.Combine(root, "esm.json"), SemanticModelSerializer.Serialize(_models[0]));
            File.WriteAllText(Path.Combine(root, "semantic-revision.txt"), $"{_models[0].Revision}\n");
            Assert.Fail("Sourceless givens corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_SOURCELESS_GIVENS_CORPUS.");
        }
    }

    [Fact] void should_compile_all_four_forms() => _models.Length.ShouldEqual(4);
    [Fact] void should_stay_within_the_frozen_language() => _models.All(model => !model.SemanticVersion.IsAtLeast(SemanticVersion.V29)).ShouldBeTrue();

    [Fact]
    void should_pin_every_model_and_its_round_trip()
    {
        var vector = SourcelessGivensCorpus.V2;
        foreach (var model in _models)
        {
            model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_attribute_sourceless_givens_to_the_event_source_the_specification_acts_on()
    {
        var vector = SourcelessGivensCorpus.V2;
        vector.SpecificationExpectations.Length.ShouldEqual(7);
        foreach (var model in _models)
        {
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            plan.Specifications.Count.ShouldEqual(7);
            foreach (var expectation in vector.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                Assert.True(run.Execution.Kind == expectation.Outcome, $"{expectation.Name}: {run.Execution.Kind} {string.Join('\n', run.Failures)}");
                run.Passed.ShouldEqual(expectation.Passed);
                switch (run.Execution)
                {
                    case SemanticRejected rejected:
                        rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                        rejected.Details.ShouldEqual(expectation.RejectionMessage);
                        break;
                    case SemanticUnsupported unsupported:
                        unsupported.Capability.ShouldEqual(expectation.UnsupportedCapability!.Value);
                        break;
                }
            }
        }
    }

    static ExecutableSemanticModel Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("SourcelessGivens", SemanticDocumentSet.Create([.. documents], catalog));
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
