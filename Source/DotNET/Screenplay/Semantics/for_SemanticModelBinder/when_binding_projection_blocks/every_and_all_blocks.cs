// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// 'every' and 'all' share Chronicle's FromEveryDefinition; 'all' additionally sets SubscribesToAllEvents and always includes
// children (ProjectionDefinitionSyntaxVisitor.cs:70-79, 123-145). Below the projection level Chronicle keeps no subscription
// flag, so 'all' there behaves as 'every' and is reported.
public class every_and_all_blocks : given.a_projection_block_binder
{
    CompilationResult<SemanticCompilation> _all;
    CompilationResult<SemanticCompilation> _allInChildren;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "every\n  lastSeen = $eventContext.occurred\n  exclude children\nfrom OrderShipped");
        _all = BindProjection("projection Orders => OrderView", "all\n  count events");
        _allInChildren = BindProjection("projection Orders => OrderView", "children lines identified by lineNumber\n  all\n    count quantity");
    }

    [Fact] void should_bind_every() => _result.Success.ShouldBeTrue();
    [Fact] void should_exclude_children_for_every() => Scope.Every!.IncludeChildren.ShouldBeFalse();
    [Fact] void should_not_subscribe_every_to_all_events() => Scope.Every!.SubscribesToAllEvents.ShouldBeFalse();
    [Fact] void should_read_the_event_context() => Scope.Every!.Mappings.Single().Source.ShouldEqual(SemanticProjectionValue.EventContext("occurred"));
    [Fact] void should_subscribe_all_to_all_events() => AllEvery(_all).SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_count_as_an_increment() => AllEvery(_all).Mappings.Single().Operation.ShouldEqual(SemanticProjectionOperation.Increment);
    [Fact] void should_bind_all_in_children() => _allInChildren.Success.ShouldBeTrue();
    [Fact] void should_not_subscribe_all_in_children_to_all_events() =>
        AllEvery(_allInChildren, children: true).SubscribesToAllEvents.ShouldBeFalse();
    [Fact] void should_warn_about_all_in_children() => _allInChildren.Diagnostics.Any(_ => _.Code == DiagnosticCodes.PartiallyLoweredProjectionSyntax).ShouldBeTrue();

    static SemanticProjectionEvery AllEvery(CompilationResult<SemanticCompilation> result, bool children = false)
    {
        var scope = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Projections).Single().Scope!;
        return children ? scope.Children.Single().Scope.Every! : scope.Every!;
    }
}
