// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_deriving_state_from_mixed_facts;

public class and_a_trailing_other_key_clears_entry_effects : given.a_lifecycle_compilation
{
    (string State, ImmutableArray<SemanticStateEntry> Effects) _result;

    void Because() => _result = SemanticLifecycleRunner.Derive(Plan, Machine, [.. Facts, Fact("InvoiceSent"), Fact("InvoicePaid") with { Destination = SemanticValue.Text("other") }], Key);

    [Fact] void should_keep_the_matching_state() => _result.State.ShouldEqual("Sent");
    [Fact] void should_clear_the_previous_entry_effects() => _result.Effects.ShouldBeEmpty();
}
