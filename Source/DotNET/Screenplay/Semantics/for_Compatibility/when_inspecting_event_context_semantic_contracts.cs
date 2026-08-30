// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_Compatibility;

public class when_inspecting_event_context_semantic_contracts : Specification
{
    Type[] _commandConstructor = null!;
    Type[] _factConstructor = null!;
    Type[] _specificationCommandConstructor = null!;
    Type[] _specificationEventConstructor = null!;

    void Because()
    {
        _commandConstructor = Parameters<SemanticCommand>();
        _factConstructor = Parameters<SemanticFact>();
        _specificationCommandConstructor = Parameters<SemanticSpecificationCommand>();
        _specificationEventConstructor = Parameters<SemanticSpecificationEvent>();
    }

    [Fact] void should_keep_the_command_constructor_unchanged() => _commandConstructor.ShouldEqual(typeof(SemanticId), typeof(string), typeof(ImmutableArray<SemanticProperty>), typeof(ImmutableArray<SemanticValidationRule>), typeof(ImmutableArray<SemanticProducedEvent>));
    [Fact] void should_add_command_destination_as_an_init_property() => typeof(SemanticCommand).GetProperty(nameof(SemanticCommand.Destination)).ShouldNotBeNull();
    [Fact] void should_keep_the_fact_constructor_unchanged() => _factConstructor.ShouldEqual(typeof(SemanticId), typeof(SemanticValue), typeof(ImmutableArray<SemanticPropertyValue>));
    [Fact] void should_add_fact_context_as_an_init_property() => typeof(SemanticFact).GetProperty(nameof(SemanticFact.Context)).ShouldNotBeNull();
    [Fact] void should_keep_the_specification_event_constructor_unchanged() => _specificationEventConstructor.ShouldEqual(typeof(SemanticId), typeof(ImmutableArray<SemanticPropertyValue>));
    [Fact] void should_add_specification_event_source_as_an_init_property() => typeof(SemanticSpecificationEvent).GetProperty(nameof(SemanticSpecificationEvent.EventSource)).ShouldNotBeNull();
    [Fact] void should_keep_the_specification_command_constructor_unchanged() => _specificationCommandConstructor.ShouldEqual(typeof(SemanticId), typeof(ImmutableArray<SemanticPropertyValue>));
    [Fact] void should_add_specification_command_source_as_an_init_property() => typeof(SemanticSpecificationCommand).GetProperty(nameof(SemanticSpecificationCommand.EventSource)).ShouldNotBeNull();
    [Fact] void should_append_the_event_context_expression_kind() => ((int)SemanticExpressionKind.EventContext).ShouldEqual(2);
    [Fact] void should_define_unknown_context_values_as_minus_one() => ((int)SemanticEventContextValueKind.Unknown).ShouldEqual(-1);
    [Fact] void should_define_event_source_identity_as_the_first_context_value() => ((int)SemanticEventContextValueKind.EventSourceIdentity).ShouldEqual(0);

    static Type[] Parameters<T>() => [.. typeof(T).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType)];
}
