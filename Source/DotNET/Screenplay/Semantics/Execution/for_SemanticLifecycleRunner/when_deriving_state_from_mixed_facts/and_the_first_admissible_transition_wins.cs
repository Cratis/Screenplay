// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_deriving_state_from_mixed_facts;

public class and_the_first_admissible_transition_wins : given.a_lifecycle_compilation
{
    (string State, ImmutableArray<SemanticStateEntry> Effects) _result;

    void Establish()
    {
        var sent = Machine.Transitions[0];
        // Competing cross-state declarations expose destination admission order.
        Machine = Machine with { Transitions = [sent with { From = "Paid", To = "Overdue" }, sent, sent with { To = "Paid" }] };
    }

    void Because() => _result = SemanticLifecycleRunner.Derive(Plan, Machine, [.. Facts, Fact("InvoiceSent")], Key);

    [Fact] void should_skip_the_unallowed_target_and_take_the_first_allowed_one() => _result.State.ShouldEqual("Sent");
    [Fact] void should_preserve_the_entry_effect_order() => _result.Effects.ToArray().ShouldEqual(Machine.States.Single(state => state.Name == "Sent").Entering.ToArray());
}
