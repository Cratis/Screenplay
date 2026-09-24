// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_a_variant_projection : Specification
{
    const string Source =
        """
        concept IssueId : Uuid
        module Work
          feature Tracking
            slice StateChange Changes
              command CreateIssue
                issueId IssueId identifier
                produces IssueCreated
                  for issueId
                  issueId = issueId
              event IssueCreated
                issueId IssueId
              event IssueStarted
                issueId IssueId
              specification CreatingAnIssue
                when CreateIssue
                  issueId = "00000000-0000-0000-0000-000000000101"
                then IssueCreated
                  issueId = "00000000-0000-0000-0000-000000000101"
                then readmodel BacklogItem
                  issueId = "00000000-0000-0000-0000-000000000101"
            slice StateView Items
              readmodel BacklogItem
                issueId IssueId
              readmodel DevelopmentItem
                issueId IssueId
              query BacklogById => BacklogItem?
                by issueId IssueId
              query DevelopmentById => DevelopmentItem?
                by issueId IssueId
              projection WorkItem
                variant BacklogItem
                  enters on IssueCreated key issueId
                variant DevelopmentItem
                  enters on IssueStarted key issueId
        """;

    SemanticSpecificationRun _result;

    void Because()
    {
        const string StableKey = "variant-specification";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Work.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Work", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _result = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_project_the_entering_event() => _result.Passed.ShouldBeTrue();
}
