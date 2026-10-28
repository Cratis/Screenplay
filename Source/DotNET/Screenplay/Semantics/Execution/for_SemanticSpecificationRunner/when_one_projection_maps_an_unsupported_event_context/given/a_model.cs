// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_one_projection_maps_an_unsupported_event_context.given;

public class a_model : Specification
{
    public const string Source = """
        reaction batches atomic
        command reads protected
        module M
          feature F
            slice StateChange Actions
              command Quiet
                id String identifier
              command Record
                id String identifier
                produces Recorded
                  for id
              command Read
                id String identifier
                reads Timeline by id
              command Start
                id String identifier
                produces Started
                  for id
              event Recorded
              event Removed
              event Started
              specification Unrelated
                when Quiet
                  id = "one"
                then no events
              specification GivenEvent
                given Recorded
                  for "one"
                when Quiet
                  id = "one"
                then no events
              specification ProducedEvent
                when Record
                  id = "one"
                then Recorded
              specification AppendedEvent
                when append Recorded
                  for "one"
                then no events
              specification DecisionRead
                when Read
                  id = "one"
                then no events
              specification Query
                then query Find
                  arguments
                    id = "one"
              specification Assertion
                then readmodel Timeline
                  id = "one"
                  at = "2026-10-02T09:00:00Z"
              specification Absence
                then no readmodel Timeline for "one"
              specification GivenState
                given readmodel Timeline
                  id = "one"
                  at = "2026-10-02T09:00:00Z"
                when Quiet
                  id = "one"
                then no events
            slice StateView History
              readmodel Timeline
                id String key
                at String
              query Find => Timeline optional
                by id String
              projection BuildTimeline => Timeline
                from Recorded
                  id = $eventSourceId
                  at = $eventContext.hash
                remove with Removed
            slice Automation FollowUp
              reaction RecordAfterStart
                when Started
                  invokes Record
                    id = $eventSourceId
              specification Cascade
                when append Started
                  for "one"
                then no events
        """;

    protected SemanticCompilation _compilation;
    protected SemanticSpecificationRun _result;

    void Establish()
    {
        const string StableKey = "deferred-projection";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DeferredProjection"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("DeferredProjection", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _compilation = compilation.Value!;
    }

    protected SemanticSpecificationRun Run(string name)
    {
        var specification = _compilation.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(slice => slice.Specifications).Single(specification => specification.Name == name);
        return new SemanticSpecificationRunner().Run(_compilation, specification.Id);
    }
}
