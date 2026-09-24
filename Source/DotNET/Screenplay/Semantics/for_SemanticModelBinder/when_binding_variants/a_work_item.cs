// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_variants;

// Chronicle VariantReclassifier.cs:28-72 and ModelBoundProjectionBuilder.cs:144-166 (Decision: 0001).
public class a_work_item : given.a_semantic_binder
{
    protected const string Source =
        """
        concept WorkItemId : Uuid
        module Work
          feature Tracking
            slice StateChange Changes
              event IssueCreated
                issueId WorkItemId
                title String
              event IssueStarted
                issueId WorkItemId
                title String
              event TitleChanged
                issueId WorkItemId
                title String
              event BuildCompleted
                issueId WorkItemId
                buildStatus String
            slice StateView Items
              readmodel BacklogItem
                issueId WorkItemId
                title String?
              readmodel DevelopmentItem
                issueId WorkItemId
                title String?
                buildStatus String?
              query BacklogById => BacklogItem?
                by issueId WorkItemId
              query DevelopmentById => DevelopmentItem?
                by issueId WorkItemId
              projection WorkItem
                from TitleChanged key issueId
                  title = title
                variant BacklogItem
                  enters on IssueCreated key issueId
                variant DevelopmentItem
                  enters on IssueStarted key issueId
                  from BuildCompleted key issueId
                    buildStatus = buildStatus
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticProjection[] _projections;

    void Because()
    {
        _result = Bind(Source);
        _projections = _result.Value?.Model.Application.Modules.Single().Features.Single().Slices
            .SelectMany(slice => slice.Projections).ToArray() ?? [];
    }

    [Fact] void should_bind_both_variants() => string.Join("; ", _result.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldEqual(string.Empty);
    [Fact] void should_use_independent_projection_ids() => _projections.Select(projection => projection.Id).Distinct().Count().ShouldEqual(2);
    [Fact] void should_use_independent_read_models() => _projections.Select(projection => projection.ReadModel).Distinct().Count().ShouldEqual(2);
    [Fact] void should_keep_only_own_entry_as_create_or_update() => _projections.All(projection => projection.Scope!.From.Length == 1).ShouldBeTrue();
    [Fact] void should_remove_each_sibling_on_its_entering_event() => _projections.All(projection => projection.Scope!.Removals.Length == 1).ShouldBeTrue();
    [Fact] void should_join_global_handlers_to_the_variant_key() => _projections.All(projection => projection.Scope!.Joins.Any(join => join.Key is not null)).ShouldBeTrue();
    [Fact] void should_keep_variant_local_handlers_update_only() => _projections.Single(projection => projection.Name.EndsWith(":DevelopmentItem", StringComparison.Ordinal)).Scope!.Joins.Length.ShouldEqual(2);
}
