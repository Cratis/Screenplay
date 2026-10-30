// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticCommandBodyResult;

public class when_creating_produced_events : Specification
{
    ImmutableArray<SemanticCommandBodyItem> _items;
    SemanticCommandBodyResult _result = null!;

    void Establish() => _items = [new(default, SemanticValue.Composite([]))];
    void Because() => _result = SemanticCommandBodyResult.ProducedEvents(_items);

    [Fact] void should_preserve_items() => _result.Items.ShouldEqual(_items);
    [Fact] void should_leave_payloads_unavailable() => _result.Payloads.IsDefault.ShouldBeTrue();
    [Fact] void should_have_no_diagnostic() => _result.Diagnostic.ShouldBeNull();
    [Fact] void should_leave_v1_items_unavailable() => SemanticCommandBodyResult.Produced([]).Items.IsDefault.ShouldBeTrue();
    [Fact] void should_keep_the_only_public_constructor() => typeof(SemanticCommandBodyResult).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType).ShouldEqual([typeof(ImmutableArray<SemanticValue>), typeof(string)]);
}
