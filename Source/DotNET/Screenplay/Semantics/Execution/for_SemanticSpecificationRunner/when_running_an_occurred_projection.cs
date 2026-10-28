// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_an_occurred_projection : for_SemanticModelBinder.given.a_semantic_binder
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_require_an_explicit_clock_for_an_appended_occurrence(bool clocked)
    {
        var clock = clocked ? "given clock \"2026-10-02T09:00:00.1234567Z\"" : "";
        var source = $$"""
            module M
              feature F
                slice StateChange Actions
                  command Record
                    id String identifier
                    produces Recorded
                      for id
                  event Recorded
                  specification Timestamp
                    {{clock}}
                    when append Recorded
                      for "one"
                    then readmodel Timeline
                      id = "one"
                      at = "2026-10-02T09:00:00.1234567Z"
                slice StateView History
                  readmodel Timeline
                    id String key
                    at DateTime optional
                  projection Build => Timeline
                    from Recorded
                      id = $eventSourceId
                      at = $eventContext.occurred
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Occurrence"));
        const string key = "occurred-projection";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "occurred.play", source);
        var compilation = new SemanticModelCompiler().Compile("Occurrence", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var specification = compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).Single();
        var run = new SemanticSpecificationRunner().Run(compilation.Value, specification.Id);
        run.Passed.ShouldEqual(clocked);
        if (clocked)
        {
            run.Execution.ShouldBeOfExactType<SemanticAccepted>();
            run.Failures.ShouldBeEmpty();
        }
        else
        {
            // A projection that cannot complete keeps the existing unsupported outcome; no new outcome category is introduced.
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            ((SemanticUnsupported)run.Execution).Details.ShouldContain("occurrence time");
            run.Execution.World.ReadModels.ShouldBeEmpty();
        }
    }
}
