// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_deriving_state_from_mixed_facts;

public class and_only_matching_keys_change_state : given.a_lifecycle_compilation
{
    (string State, ImmutableArray<SemanticStateEntry> Effects) _result;

    void Because() => _result = SemanticLifecycleRunner.Derive(Plan, Machine, [.. Facts, Fact("InvoiceSent") with { Destination = SemanticValue.Text("other") }], Key);

    [Fact] void should_remain_in_the_initial_state() => _result.State.ShouldEqual("Draft");
    [Fact] void should_have_no_entry_effects() => _result.Effects.ShouldBeEmpty();
}
