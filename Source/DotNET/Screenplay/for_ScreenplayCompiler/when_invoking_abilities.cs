// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_invoking_abilities : given.a_compiler
{
    const string Systems = """
        domain Acme.Ship

        concept ParcelId : Uuid

        system Carrier
          ability BookPickup once
            parcelId ParcelId
            returns
              trackingNumber String
            outcome NoCapacity
          ability Ask once
            answers later
            parcelId ParcelId
            outcome approved
            outcome rejected

        system Chat
          ability Post
            channel String
            text String

        """;

    string Slice(string reaction) => Systems + """
        module Shipping
          feature Pickup
            slice Automation Booking
              readmodel ParcelView
                parcelId ParcelId
              event OrderPacked
                parcelId ParcelId
              event Booked
                trackingNumber String
              event Unavailable
                parcelId ParcelId
              reaction Booking
                runs as system
                when OrderPacked
                  parcelId
        """ + "\n" + reaction;

    IEnumerable<string> Codes(string reaction) => _compiler.Compile(Slice(reaction)).Diagnostics.Select(diagnostic => diagnostic.Code);

    const string Valid = """
                  invokes Carrier.BookPickup
                    parcelId = parcelId
                    on returned
                      produces Booked
                        trackingNumber = $result.trackingNumber
                    on NoCapacity
                      produces Unavailable
                        parcelId = parcelId
        """;

    InvokesSyntax _invocation;

    void Because() => _invocation = _compiler.Compile(Slice(Valid)).Value!.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single().Invokes.Single();

    [Fact] void should_read_the_target() => _invocation.Command.ShouldEqual("Carrier.BookPickup");
    [Fact] void should_read_the_branches_in_order() => _invocation.Branches.Select(branch => branch.Selector).ShouldEqual(["returned", "NoCapacity"]);
    [Fact] void should_accept_a_valid_invocation() => Codes(Valid).ShouldBeEmpty();
    [Fact] void should_refuse_an_unknown_ability() => Codes(Valid.Replace("BookPickup", "Missing")).ShouldContain(DiagnosticCodes.UnknownInvocationTarget);
    [Fact] void should_refuse_an_unknown_outcome() => Codes(Valid.Replace("on NoCapacity", "on Nope")).ShouldContain(DiagnosticCodes.UnknownInvocationOutcome);
    [Fact] void should_refuse_a_duplicate_branch() => Codes(Valid.Replace("on NoCapacity", "on returned")).ShouldContain(DiagnosticCodes.DuplicateInvocationBranch);
    [Fact] void should_refuse_a_missing_input() => Codes(Valid.Replace("parcelId = parcelId", "parcelId = parcelId\n                    extra = parcelId")).ShouldContain(DiagnosticCodes.UnknownAbilityInput);
    [Fact] void should_refuse_an_unmapped_input() => Codes("                  invokes Chat.Post\n                    channel = \"c\"").ShouldContain(DiagnosticCodes.MissingAbilityInput);
    [Fact] void should_refuse_a_mismatched_literal() => Codes(Valid.Replace("parcelId = parcelId", "parcelId = 5")).ShouldContain(DiagnosticCodes.IncompatibleAbilityInputMapping);
    [Fact] void should_refuse_a_result_outside_its_branch() => Codes(Valid.Replace(new string(' ', 16) + "parcelId = parcelId", new string(' ', 16) + "parcelId = $result.trackingNumber")).ShouldContain(DiagnosticCodes.InvocationValueOutsideBranch);
    [Fact] void should_refuse_an_answer_that_comes_later_in_a_reaction() => Codes("                  invokes Carrier.Ask\n                    parcelId = parcelId").ShouldContain(DiagnosticCodes.AnswersLaterInReaction);
}
