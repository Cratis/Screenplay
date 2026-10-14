// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_refusal_branches : given.a_v6_scenario
{
    const string Source =
        """
        policy Member
          require authenticated and role "member"
        module Billing
          feature Payments
            slice Automation Recovery
              event Approved
                id String
              event Claimed
                id String
              event Refused
                reason String
                constraint String
                message String
              event Finished
              event OtherObserved
              command Approve
                id String identifier
                produces Approved
                  for id
                  id = id
              command Claim
                id String identifier
                produces Claimed
                  for id
                  id = id
              constraint AlphaClaim
                unique event Claimed
                message " first "
              constraint BetaClaim
                unique event Claimed
                message "second"
              command Invalid
                id String identifier
                validate
                  require id != "a"
                    message " first "
                  require id != "a"
                    message "second"
              command Secure
                authorize Member
              command FollowUp
                id String identifier
                produces Finished
                  for id
              reaction ConstraintHandler
                when Approved
                  id
                  invokes Claim
                    id = id
                    on refused by constraint BetaClaim
                      produces Refused
                        reason = $refusal.reason
                        constraint = $refusal.constraint
                        message = $refusal.message
                    on refused
                      acknowledge
                  invokes FollowUp
                    id = "a"
              reaction Finisher
                when Refused
                  produces Finished
              reaction ValidationHandler
                when Approved
                  id
                  invokes Invalid
                    id = id
                    on refused by validation
                      produces Refused
                        reason = $refusal.reason
                        constraint = "none"
                        message = $refusal.message
                  invokes FollowUp
                    id = "a"
              reaction AuthorizationHandler
                when Approved
                  invokes Secure
                    on refused
                      acknowledge
                    on refused by authorization
                      acknowledge
                  invokes FollowUp
                    id = "a"
              reaction SystemHandler
                runs as system role "member"
                when Approved
                  invokes Secure
                    on refused by authorization
                      acknowledge
                  invokes FollowUp
                    id = "a"
              reaction Other
                when Approved
                  produces OtherObserved
              specification NamedConstraintMatchesAnyViolation
                given Claimed
                  for "a"
                  id = "a"
                given Approved
                  for "a"
                  id = "a"
                when redelivered Approved to ConstraintHandler
                then Refused
                  for "a"
                  reason = "constraint"
                  constraint = "AlphaClaim"
                  message = " first \nsecond"
                then Finished
                  for "a"
              specification ValidationMessagesAreVerbatim
                given Approved
                  for "a"
                  id = "a"
                when redelivered Approved to ValidationHandler
                then Refused
                  for "a"
                  reason = "validation"
                  constraint = "none"
                  message = " first \nsecond"
                then Finished
                  for "a"
              specification AuthorizationIsExplicit
                given Approved
                  for "a"
                  id = "a"
                when redelivered Approved to AuthorizationHandler
                then no events
              specification SystemIdentityAuthorizesTheInvocation
                given Approved
                  for "a"
                  id = "a"
                when redelivered Approved to SystemHandler
                then Finished
                  for "a"
              specification OtherReactionsContinue
                given Claimed
                  for "a"
                  id = "a"
                when append Approved
                  for "a"
                  id = "a"
                then events in any order
                then Refused
                  for "a"
                  reason = "constraint"
                  constraint = "AlphaClaim"
                  message = " first \nsecond"
                then Refused
                  for "a"
                  reason = "validation"
                  constraint = "none"
                  message = " first \nsecond"
                then Finished
                  for "a"
                then Finished
                  for "a"
                then Finished
                  for "a"
                then OtherObserved
                  for "a"
        """;

    SemanticSpecificationRun[] _runs;

    void Establish() => Compile(Source);
    void Because() => _runs = [.. new[] { "NamedConstraintMatchesAnyViolation", "ValidationMessagesAreVerbatim", "AuthorizationIsExplicit", "SystemIdentityAuthorizesTheInvocation", "OtherReactionsContinue" }.Select(Run)];

    [Fact] void should_select_v19() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V19);
    [Fact] void should_match_a_later_constraint_but_expose_the_first_constraint() => _runs[0].Failures.ShouldBeEmpty();
    [Fact] void should_join_validation_messages_verbatim() => _runs[1].Failures.ShouldBeEmpty();
    [Fact] void should_handle_authorization_and_skip_remaining_invocations_without_facts() => _runs[2].Failures.ShouldBeEmpty();
    [Fact] void should_use_system_roles_without_changing_audit_identity() => _runs[3].Failures.ShouldBeEmpty();
    [Fact] void should_continue_other_reactions_and_branch_cascades() => _runs[4].Failures.ShouldBeEmpty();
}
