// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_only_reached_behavior_matters : given.a_v6_scenario
{
    const string Source =
        """
        policy SignedIn
          require authenticated
        module Billing
          feature Flow
            slice StateChange Finish
              command Finish
                id String identifier
                produces Finished
                  for id
                  at = $context.occurred
              command Allocate
                id String identifier
                produces Allocated
              command Protected
                id String identifier
                authorize SignedIn
                produces ProtectedFact
                  for id
              event Finished
                at DateTime
              event Allocated
              event ProtectedFact
            slice StateView OpaqueView
              readmodel Details
                id String
              query DetailsById => Details optional
                by id String
              reducer Derived => Details
                on Reduced
                  ```csharp
                    return current;
                    ```
            slice Automation Flow
              readmodel Marker
                id String
              query MarkerById => Marker optional
                by id String
              reaction Invoke
                when Started
                  invokes Finish
                    id = "destination"
              reaction AllocateOnDemand
                when AllocationRequested
                  invokes Allocate
                    id = "not-an-allocation"
              reaction Protect
                when ProtectedRequested
                  produces PriorAccepted
                  invokes Protected
                    id = "protected"
              reaction Audit
                when Other
                  produces Audited
                    user = $context.causedBy.userName
              reaction Opaque
                when Unrelated
                  ```csharp
                    return [];
                    ```
              reaction ExcludedOpaque
                when Skipped
                  allowed
                  ```csharp
                    return [];
                    ```
                where allowed == true
              event Reduced
              event Started
              event AllocationRequested
              event ProtectedRequested
              event PriorAccepted
              event Other
              event Audited
                user String
              event Unrelated
              event Skipped
                allowed Bool
              specification ReachedInvocation
                given clock "2026-10-02T09:00:00Z"
                when append Started
                then Finished
                  for "destination"
                  at = "2026-10-02T09:00:00Z"
              specification MissingAllocation
                when append AllocationRequested
                then Allocated
              specification DeniedInvocation
                when append ProtectedRequested
                then denied
              specification ReachedAudit
                given clock "2026-10-02T09:00:00Z"
                when append Other
                then Audited
                  user = "invented"
              specification ExcludedBody
                given clock "2026-10-02T09:00:00Z"
                given readmodel Marker
                  id = "unchanged"
                when append Skipped
                  allowed = false
                then readmodel Marker
                  id = "unchanged"
              specification ReachedReducer
                given clock "2026-10-02T09:00:00Z"
                when append Reduced
                then Reduced
        """;

    SemanticSpecificationRun _invoked;
    SemanticSpecificationRun _allocated;
    SemanticSpecificationRun _denied;
    SemanticSpecificationRun _audit;
    SemanticSpecificationRun _excluded;
    SemanticSpecificationRun _reducer;

    void Establish() => Compile(Source);

    void Because()
    {
        _invoked = Run("ReachedInvocation");
        _allocated = Run("MissingAllocation");
        _denied = Run("DeniedInvocation");
        _audit = Run("ReachedAudit");
        _excluded = Run("ExcludedBody");
        _reducer = Run("ReachedReducer");
    }

    [Fact] void should_not_scan_unrelated_causation_or_opaque_reactions() => _invoked.Passed.ShouldBeTrue();
    [Fact] void should_preserve_the_explicit_destination() => ((SemanticAccepted)_invoked.Execution).Facts[1].Destination.ShouldEqual(SemanticValue.Text("destination"));
    [Fact] void should_propagate_occurrence_to_invoked_facts() => ((SemanticAccepted)_invoked.Execution).Facts.Select(fact => fact.Occurred).Distinct().Count().ShouldEqual(1);
    [Fact] void should_mark_the_invoking_reaction_as_causation() => ((SemanticAccepted)_invoked.Execution).Facts[1].ReactionOrigin.ShouldEqual(_plan.Reactions.Single(reaction => reaction.Name == "Invoke").Id);
    [Fact] void should_not_treat_an_identifier_as_an_allocated_destination() => ((SemanticUnsupported)_allocated.Execution).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
    [Fact] void should_not_inherit_a_caller_for_an_invoked_command() => ((SemanticRejected)_denied.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_retain_facts_accepted_before_a_later_denial() => _denied.Execution.World.Facts.Length.ShouldEqual(2);
    [Fact] void should_not_append_the_denied_commands_fact() => _denied.Execution.World.Facts.Any(fact => fact.EventContract == _plan.Events.Values.Single(value => value.Name == "ProtectedFact").Id).ShouldBeFalse();
    [Fact] void should_report_reached_audit_identity_as_unsupported() => _audit.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_execute_a_body_excluded_by_where() => _excluded.Passed.ShouldBeTrue();
    [Fact] void should_refuse_a_reached_reducer_even_without_a_state_assertion() => ((SemanticUnsupported)_reducer.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_not_accept_a_fact_whose_projection_is_opaque() => _reducer.Execution.World.Facts.ShouldBeEmpty();
}
