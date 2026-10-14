// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PublicEventsCorpus;

public class when_loading_captures_from_events_v9_source : Specification
{
    SemanticCompilation _compilation;

    void Because()
    {
        var form = PublicEventsCorpus.CapturesFromEventsSourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("CaptureEvents", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _compilation = result.Value!;
        if (string.Equals(Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_CAPTURES_FROM_EVENTS_CORPUS"), "1", StringComparison.Ordinal))
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/PublicEvents/CapturesFromEvents/v9/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v9.json"), SemanticModelSerializer.Serialize(_compilation.Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_compilation.Model.Revision}\n");
            Assert.Fail("Capture-from-event corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_CAPTURES_FROM_EVENTS_CORPUS.");
        }
    }

    [Fact] void should_select_v9() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    [Fact] void should_pin_the_revision() => _compilation.Model.Revision.ShouldEqual(PublicEventsCorpus.CapturesFromEventsV9.SemanticRevision);
    [Fact] void should_pin_the_canonical_bytes() => SemanticModelSerializer.Serialize(_compilation.Model).SequenceEqual(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes).ShouldBeTrue();
    [Fact] void should_round_trip_the_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan())).SequenceEqual(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes).ShouldBeTrue();

    [Fact]
    void should_execute_added_transition_overlay_and_unchanged_outcomes()
    {
        var plan = SemanticExecutionPlan.Compile(_compilation.Model).Plan!;
        foreach (var expectation in PublicEventsCorpus.CapturesFromEventsV9.SpecificationExpectations)
        {
            var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
            run.Passed.ShouldBeTrue();
            run.Execution.Kind.ShouldEqual(expectation.Outcome);
            run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
        }
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
