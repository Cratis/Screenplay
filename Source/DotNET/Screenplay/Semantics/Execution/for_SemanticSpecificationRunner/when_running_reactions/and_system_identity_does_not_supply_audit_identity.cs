// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_system_identity_does_not_supply_audit_identity : given.a_v6_scenario
{
    SemanticSpecificationRun _run;

    void Establish() => Compile(
        """
        module M
          feature F
            slice Automation Work
              command Complete
                actor String
              event Started
              reaction Worker
                runs as system
                when Started
                  invokes Complete
                    actor = $context.causedBy.userName
              specification Invoking
                given caller
                  authenticated
                given clock "2026-10-02T09:00:00Z"
                when append Started
                then Started
        """);

    void Because() => _run = Run("Invoking");

    [Fact] void should_refuse_to_invent_audit_identity() => ((SemanticUnsupported)_run.Execution).Details.ShouldContain("$context.causedBy");
}
