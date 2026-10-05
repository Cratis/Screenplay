// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_reaction_cannot_run_in_the_reference : given.a_v6_scenario
{
    const string Source =
        """
        concept TicketId : Uuid
        module Support
          feature Tickets
            slice StateChange OpenTicket
              command OpenTicket
                ticketId TicketId identifier
                title String
                validate
                  title not empty message "A ticket needs a title"
                produces TicketOpened
                  for ticketId
                  title = title
              event TicketOpened
                title String
            slice Automation Escalation
              reaction Notifier
                when TicketOpened
                  ```csharp
                    return [];
                    ```
              reaction Echo
                when TicketEchoed
                  title
                  produces TicketEchoed
                    title = title
              reaction Reopener
                when TicketClosed
                  title
                  invokes OpenTicket
                    ticketId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                    title = ""
              event TicketEchoed
                title String
              event TicketClosed
                title String
              specification NotifyingThroughCode
                when append TicketOpened
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  title = "Broken"
                then TicketOpened
                  title = "Broken"
              specification EchoingForever
                when append TicketEchoed
                  title = "again"
                then TicketEchoed
                  title = "again"
              specification ReopeningWithoutATitle
                when append TicketClosed
                  title = ""
                then error "A ticket needs a title"
        """;

    SemanticSpecificationRun _opaque;
    SemanticSpecificationRun _endless;
    SemanticSpecificationRun _rejected;

    void Establish() => Compile(Source);

    void Because()
    {
        _opaque = Run("NotifyingThroughCode");
        _endless = Run("EchoingForever");
        _rejected = Run("ReopeningWithoutATitle");
    }

    [Fact] void should_leave_an_opaque_body_to_a_target() => ((SemanticUnsupported)_opaque.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
    [Fact] void should_stop_reactions_that_do_not_settle() => ((SemanticUnsupported)_endless.Execution).Details.ShouldContain("do not settle");
    [Fact] void should_reject_the_scenario_with_the_invoked_command() => _rejected.Passed.ShouldBeTrue();
}
