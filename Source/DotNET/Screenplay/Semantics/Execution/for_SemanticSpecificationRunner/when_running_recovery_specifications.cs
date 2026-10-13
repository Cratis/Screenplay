// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_recovery_specifications : given.a_v6_scenario
{
    const string Source =
        """
        module Billing
          feature Payments
            slice Automation Recovery
              event Approved
                id String
              event Claimed
                id String
              event Finished
                id String
              event OtherObserved
                id String
              command Claim
                id String identifier
                produces Claimed
                  for id
                  id = id
              command Refuse
                id String identifier
                validate
                  require id != "refuse"
                    message "refused"
              command DoNothing
              reaction Claimer
                when Approved
                  id
                  invokes Claim
                    id = id
                where id == "a"
              reaction Other
                when Approved
                  id
                  produces OtherObserved
                    id = id
              reaction Finisher
                when Claimed
                  id
                  produces Finished
                    id = id
              reaction Refuser
                when Finished
                  id
                  invokes Refuse
                    id = id
                where id == "refuse"
              specification RedeliveryOnlyNamesOneReaction
                given Approved
                  id = "a"
                when redelivered Approved to Claimer
                then Claimed
                  id = "a"
                then Finished
                  id = "a"
              specification FilteredRedelivery
                given Approved
                  id = "ignored"
                when redelivered Approved to Claimer
                then no events
              specification RefusedRedelivery
                given Finished
                  id = "refuse"
                when redelivered Finished to Refuser
                then error "refused"
              specification CommandDoesAppend
                when Claim
                  id = "a"
                then no events
              specification CommandDoesNotAppend
                when DoNothing
                then no events
              specification AppendDoesCascade
                when append Approved
                  id = "a"
                then no events
              specification AppendDoesNotCascade
                when append OtherObserved
                  id = "a"
                then no events
        """;

    SemanticSpecificationRun _redelivery;
    SemanticSpecificationRun _filtered;
    SemanticSpecificationRun _refused;
    SemanticSpecificationRun _commandFacts;
    SemanticSpecificationRun _emptyCommand;
    SemanticSpecificationRun _appendFacts;
    SemanticSpecificationRun _emptyAppend;

    void Establish() => Compile(Source);

    void Because()
    {
        _redelivery = Run("RedeliveryOnlyNamesOneReaction");
        _filtered = Run("FilteredRedelivery");
        _refused = Run("RefusedRedelivery");
        _commandFacts = Run("CommandDoesAppend");
        _emptyCommand = Run("CommandDoesNotAppend");
        _appendFacts = Run("AppendDoesCascade");
        _emptyAppend = Run("AppendDoesNotCascade");
    }

    [Fact] void should_select_the_new_version() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V17);
    [Fact] void should_redeliver_only_to_the_named_reaction_and_settle_its_cascade() => _redelivery.Passed.ShouldBeTrue();
    [Fact] void should_not_append_the_given_fact_again() => _redelivery.Execution.World.Facts.Length.ShouldEqual(3);
    [Fact] void should_pass_no_events_for_a_filtered_redelivery() => _filtered.Passed.ShouldBeTrue();
    [Fact] void should_keep_the_given_fact_without_running_its_other_observers() => _filtered.Execution.World.Facts.Length.ShouldEqual(1);
    [Fact] void should_assert_an_unhandled_invocation_refusal() => _refused.Passed.ShouldBeTrue();
    [Fact] void should_fail_no_events_for_command_and_cascade_facts() => _commandFacts.Failures.ShouldContain("Expected 0 fact(s), got 2.");
    [Fact] void should_pass_no_events_for_an_empty_command() => _emptyCommand.Passed.ShouldBeTrue();
    [Fact] void should_fail_no_events_for_facts_following_an_append() => _appendFacts.Failures.ShouldContain("Expected 0 fact(s), got 3.");
    [Fact] void should_pass_no_events_for_an_append_without_following_facts() => _emptyAppend.Passed.ShouldBeTrue();
}
