// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_reaction_with_consequences : given.a_printer
{
    // A reactor states what it sets off: events it appends, commands it hands on. The verbs differ on
    // purpose - an event is a fact it produces, a command is an intent it invokes.
    const string Source =
        """
        module Onboarding
          feature Invitations
            slice Automation Provisioning
              event InvitationAccepted
                invitationId Uuid
                workspaceId Uuid

              event WorkspaceProvisioned
                workspaceId Uuid

              command SendWelcomeMail
                workspaceId Uuid

              reactor Provisioner
                on InvitationAccepted
                  produces WorkspaceProvisioned
                    for workspaceId
                    workspaceId = workspaceId
                  invokes SendWelcomeMail
                    workspaceId = workspaceId
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_keep_the_produced_event() => Trigger.Produces!.Single().Event.ShouldEqual("WorkspaceProvisioned");
    [Fact] void should_keep_the_invoked_command() => Trigger.Invokes!.Single().Command.ShouldEqual("SendWelcomeMail");
    [Fact] void should_keep_the_command_mappings() => Trigger.Invokes!.Single().Mappings.Single().Property.ShouldEqual("workspaceId");

    // #33 - where the event lands, on an indented line rather than an argument on the header.
    [Fact] void should_keep_where_the_event_lands() =>
        ((PathExpressionSyntax)Trigger.Produces!.Single().For!).Path.ShouldEqual("workspaceId");
    [Fact] void should_print_the_target_indented() => _roundtrip.Printed.ShouldContain("for workspaceId");
    [Fact] void should_print_the_invocation() => _roundtrip.Printed.ShouldContain("invokes SendWelcomeMail");

    ReactorTriggerSyntax Trigger =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single()
            .Reactors.Single().Triggers.Single();
}
