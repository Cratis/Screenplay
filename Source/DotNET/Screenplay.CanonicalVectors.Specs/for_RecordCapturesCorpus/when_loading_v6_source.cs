// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_RecordCapturesCorpus;

public class when_loading_v6_source : Specification
{
    SemanticCompilation _compilation;

    void Because()
    {
        var form = RecordCapturesCorpus.SourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("RecordCaptures", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        _compilation = result.Value!;
        if (string.Equals(Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_RECORD_CAPTURES_CORPUS"), "1", StringComparison.Ordinal))
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/RecordCaptures/v6/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v6.json"), SemanticModelSerializer.Serialize(_compilation.Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_compilation.Model.Revision}\n");
            Assert.Fail("Record-capture corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_RECORD_CAPTURES_CORPUS.");
        }
    }

    [Fact] void should_retain_v6() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_pin_the_revision() => _compilation.Model.Revision.ShouldEqual(RecordCapturesCorpus.V6.SemanticRevision);
    [Fact] void should_pin_the_canonical_bytes() => SemanticModelSerializer.Serialize(_compilation.Model).SequenceEqual(RecordCapturesCorpus.V6.EsmBytes).ShouldBeTrue();
    [Fact] void should_round_trip_the_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(RecordCapturesCorpus.V6.EsmBytes.AsSpan())).SequenceEqual(RecordCapturesCorpus.V6.EsmBytes).ShouldBeTrue();

    [Fact]
    void should_execute_first_observation_and_modification_outcomes_before_and_after_canonical_reading()
    {
        var vector = RecordCapturesCorpus.V6;
        vector.SpecificationExpectations.Length.ShouldEqual(4);
        foreach (var model in new[] { _compilation.Model, SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan()) })
        {
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            plan.Specifications.Count.ShouldEqual(vector.SpecificationExpectations.Length);
            foreach (var expectation in vector.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                Assert.True(run.Passed, $"{expectation.Name}: {string.Join(';', run.Failures)}");
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            }
        }
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
