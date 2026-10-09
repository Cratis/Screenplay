// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_list_query_shapes : given.a_semantic_binder
{
    const string Source =
        """
        concept WorkItemId : Uuid
        concept CommentId : Uuid
        module Workspaces
          feature Boards
            slice StateChange CreateWorkItem
              command CreateWorkItem
                workItemId WorkItemId identifier
                title String
                produces WorkItemCreated
                  for workItemId
                  title = title
              event WorkItemCreated
                title String

            slice StateChange AddComment
              command AddComment
                commentId CommentId identifier
                workItemId WorkItemId
                text String
                produces CommentAdded
                  for commentId
                  workItemId = workItemId
                  text = text
              event CommentAdded
                workItemId WorkItemId
                text String

            slice StateView WorkItemList
              readmodel WorkItemSummary
                workItemId WorkItemId
                title String
              query AllWorkItems => observable WorkItemSummary[]
              query WorkItemById => WorkItemSummary?
                by workItemId WorkItemId
              projection WorkItemList => WorkItemSummary
                from WorkItemCreated
                  workItemId = $eventSourceId
                  title = title

            slice StateView WorkItemComments
              readmodel CommentView
                commentId CommentId
                workItemId WorkItemId
                text String
              query CommentsForWorkItem => observable CommentView[]
                by workItemId WorkItemId
              projection WorkItemComments => CommentView
                from CommentAdded
                  commentId = $eventSourceId
                  workItemId = workItemId
                  text = text
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_observable_unkeyed_list_query() => AllWorkItems.Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
    [Fact] void should_bind_the_observable_unkeyed_delivery() => AllWorkItems.Delivery.ShouldEqual(SemanticQueryDelivery.Live);
    [Fact] void should_leave_the_unkeyed_list_argument_empty() => AllWorkItems.Argument.ShouldBeNull();
    [Fact] void should_leave_the_unkeyed_list_key_property_empty() => AllWorkItems.KeyProperty.ShouldBeNull();
    [Fact] void should_bind_the_observable_keyed_list_query() => CommentsForWorkItem.Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
    [Fact] void should_bind_the_observable_keyed_delivery() => CommentsForWorkItem.Delivery.ShouldEqual(SemanticQueryDelivery.Live);
    [Fact] void should_bind_the_keyed_list_argument() => CommentsForWorkItem.Argument!.Name.ShouldEqual("workItemId");
    [Fact] void should_bind_the_keyed_list_key_property() => CommentsForWorkItem.KeyProperty.ShouldEqual(CommentReadModel.Properties.Single(_ => _.Name == "workItemId").Id);
    [Fact] void should_keep_the_keyed_list_read_model_identifier_separate_from_the_query_key() => CommentReadModel.Properties.Single(_ => _.Name == "commentId").IsIdentifier.ShouldBeTrue();

    SemanticKeyedQuery AllWorkItems => Queries.Single(_ => _.Name == "AllWorkItems");
    SemanticKeyedQuery CommentsForWorkItem => Queries.Single(_ => _.Name == "CommentsForWorkItem");
    SemanticReadModel CommentReadModel => Slices.SelectMany(_ => _.ReadModels).Single(_ => _.Name == "CommentView");
    IEnumerable<SemanticKeyedQuery> Queries => Slices.SelectMany(_ => _.Queries);
    IEnumerable<SemanticSlice> Slices => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices;
}
