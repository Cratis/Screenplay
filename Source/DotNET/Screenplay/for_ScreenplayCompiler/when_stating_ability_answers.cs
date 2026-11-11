// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_stating_ability_answers : given.a_compiler
{
    const string Valid = """
        domain Acme.Ship

        concept ParcelId : Uuid

        system Carrier
          ability BookPickup once
            parcelId ParcelId
            returns
              trackingNumber String
            outcome NoCapacity

        module Shipping
          feature Pickup
            slice Automation Booking
              readmodel ParcelView
                parcelId ParcelId
              event OrderPacked
                parcelId ParcelId
              reaction Booking
                runs as system
                when OrderPacked
                  parcelId
                  invokes Carrier.BookPickup
                    parcelId = parcelId
              specification Booked
                given Carrier.BookPickup returns
                  trackingNumber = "TRK-1"
                given Carrier.BookPickup answers NoCapacity
                given Carrier.BookPickup fails "down"
                when append OrderPacked
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        """;

    SpecificationAbilityAnswerSyntax[] _answers;

    IEnumerable<string> Codes(string source) => _compiler.Compile(source).Diagnostics.Select(diagnostic => diagnostic.Code);

    void Because() => _answers = [.. _compiler.Compile(Valid).Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().GivenAbilityAnswers];

    [Fact] void should_read_the_answers_in_order() => _answers.Select(answer => answer.Answer).ShouldEqual(["returns", "answers", "fails"]);
    [Fact] void should_read_the_returned_values() => _answers[0].Values.Single().Property.ShouldEqual("trackingNumber");
    [Fact] void should_read_the_outcome() => _answers[1].Outcome.ShouldEqual("NoCapacity");
    [Fact] void should_read_the_failure_reason() => _answers[2].Reason.ShouldEqual("down");
    [Fact] void should_accept_valid_answers() => Codes(Valid).ShouldBeEmpty();
    [Fact] void should_refuse_an_unknown_ability() => Codes(Valid.Replace("given Carrier.BookPickup answers", "given Carrier.Missing answers")).ShouldContain(DiagnosticCodes.UnknownInvocationTarget);
    [Fact] void should_refuse_an_undeclared_outcome() => Codes(Valid.Replace("answers NoCapacity", "answers Nope")).ShouldContain(DiagnosticCodes.UnknownInvocationOutcome);
    [Fact] void should_refuse_an_undeclared_returned_value() => Codes(Valid.Replace("trackingNumber = \"TRK-1\"", "other = \"TRK-1\"")).ShouldContain(DiagnosticCodes.InvalidAbilityAnswer);
    [Fact] void should_refuse_an_answer_without_an_outcome() => Codes(Valid.Replace("answers NoCapacity", "answers")).ShouldContain(DiagnosticCodes.InvalidSpecificationInvocation);
}
