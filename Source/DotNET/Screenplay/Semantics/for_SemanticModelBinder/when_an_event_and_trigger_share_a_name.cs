// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_an_event_and_trigger_share_a_name : given.a_semantic_binder
{
    const string Source = """
        trigger Changed
          id String
          triggerValue String
        module M
          feature F
            slice Automation S
              event Changed
                id Uuid
                eventValue String
              event Recorded
                value String
              reaction R
                when Changed
                  id
                  eventValue
                  produces Recorded
                    for id
                    value = eventValue
        """;

    CompilationResult<ApplicationSyntax> _authoring;
    CompilationResult<SemanticCompilation> _result;

    void Because()
    {
        _authoring = new ScreenplayCompiler().Compile(Source);
        _result = Bind(Source);
    }

    [Fact] void should_validate_without_diagnostics() => _authoring.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_event_occurrence() => Trigger.Kind.ShouldEqual(SemanticReactionTriggerKind.Event);
    [Fact] void should_read_the_destination_from_the_event() => ((SemanticResolvedExpression)Trigger.Produces.Single().Destination!).Root.ShouldEqual(SemanticExpressionRootKind.Event);
    [Fact] void should_use_the_event_destination_type() => Trigger.Produces.Single().DestinationType.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid));
    [Fact] void should_map_the_event_value() => ((SemanticResolvedExpression)Trigger.Produces.Single().Mappings.Single().Source).Root.ShouldEqual(SemanticExpressionRootKind.Event);

    SemanticReactionTrigger Trigger => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single();
}
