// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// '$causedBy.<member>' is resolved by Chronicle at runtime (Cratis/Chronicle#4119) and reads the identity that caused the event,
// so it binds to the same value as '$eventContext.causedBy.<member>'.
public class caused_by_values : given.a_projection_block_binder
{
    CompilationResult<SemanticCompilation> _eventContext;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "from OrderShipped\n  label = $causedBy.userName\n  customerName = $causedBy.subject");
        _eventContext = BindProjection("projection Orders => OrderView", "from OrderShipped\n  label = $eventContext.causedBy.userName\n  customerName = $eventContext.causedBy.subject");
    }

    [Fact] void should_bind() => Errors.ShouldBeEmpty();
    [Fact] void should_bind_to_the_event_context_value() => Sources(_result).ShouldEqual(Sources(_eventContext));
    [Fact] void should_read_the_user_name() => Sources(_result).ShouldContain(new SemanticProjectionEventContextValue("causedBy.userName"));

    static SemanticProjectionValue[] Sources(CompilationResult<SemanticCompilation> result) =>
        [.. result.Value!.Model.Application.Modules.Single().Features.Single().Slices.SelectMany(_ => _.Projections).Single().Scope!.From.Single().Mappings.Select(_ => _.Source!)];
}
