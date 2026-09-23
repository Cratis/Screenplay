// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// With no key Chronicle keys on the event source identity (ProjectionFactory.cs:1009-1017), and its generator omits an explicit
// 'key $eventSourceId' because it is the same declaration (Generator.cs:168-169).
public class a_from_block_without_a_key : given.a_projection_block_binder
{
    CompilationResult<SemanticCompilation> _explicit;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "from OrderShipped\n  label = carrier");
        _explicit = BindProjection("projection Orders => OrderView", "from OrderShipped key $eventSourceId\n  label = carrier");
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_use_the_scoped_shape() => Projection.Transitions.ShouldBeEmpty();
    [Fact] void should_key_on_the_event_source_identity() => Scope.From.Single().Key.ShouldEqual(SemanticProjectionKey.EventSourceIdentity);
    [Fact] void should_carry_no_parent_key_at_the_projection_level() => Scope.From.Single().ParentKey.ShouldBeNull();
    [Fact] void should_bind_an_explicit_event_source_key_identically() => _explicit.Value!.Model.Revision.ShouldEqual(_result.Value!.Model.Revision);
}
