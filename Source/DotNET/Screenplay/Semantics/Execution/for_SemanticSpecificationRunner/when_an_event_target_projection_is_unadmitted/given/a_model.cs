// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_an_event_target_projection_is_unadmitted.given;

public class a_model : Specification
{
    public const string Source = """
        module M
          feature F
            slice StateChange Actions
              command Quiet
                id String identifier
              command Record
                id String identifier
                produces Recorded
                  for id
              event Recorded
              specification Unrelated
                when Quiet
                  id = "one"
                then no events
              specification Recording
                when Record
                  id = "one"
                then Recorded
            slice Translate Publication
              direction outbound
              public event Published
                at DateTime
              projection Publisher => Published
                from Recorded
                  at = $eventContext.occurred
              specification Observed
                when append Recorded
                  for "one"
                then no events
        """;

    protected SemanticCompilation _compilation;
    protected SemanticSpecificationRun _result;

    void Establish()
    {
        const string StableKey = "deferred-publication";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DeferredPublication"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("DeferredPublication", SemanticDocumentSet.Create([document], catalog));
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
