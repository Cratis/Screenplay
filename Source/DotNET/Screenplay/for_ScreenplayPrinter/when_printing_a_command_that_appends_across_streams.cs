// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_command_that_appends_across_streams : given.a_printer
{
    // One decision, events landing on different event sources - the shape a fan-out handler has, and the
    // one a document could not draw at all. Several 'produces', each saying where it lands.
    const string Source =
        """
        module Requests
          feature Activation
            slice StateChange Activate
              command Activate
                requestId Uuid identifier
                contractId Uuid
                candidateId Uuid

                produces RequestActivated
                  requestId = requestId

                produces ContractPolicyActivated
                  for contractId
                  contractId = contractId

                produces CandidateEngaged
                  for candidateId
                  candidateId = candidateId

              event RequestActivated
                requestId Uuid

              event ContractPolicyActivated
                contractId Uuid

              event CandidateEngaged
                candidateId Uuid
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_keep_every_production() => Command.Produces.Count().ShouldEqual(3);

    // The event on the command's own stream says nothing about where it lands, which is the default.
    [Fact] void should_leave_the_implicit_stream_unstated() => Produces("RequestActivated").For.ShouldBeNull();
    [Fact] void should_state_the_contract_stream() => ((PathExpressionSyntax)Produces("ContractPolicyActivated").For!).Path.ShouldEqual("contractId");
    [Fact] void should_state_the_candidate_stream() => ((PathExpressionSyntax)Produces("CandidateEngaged").For!).Path.ShouldEqual("candidateId");
    [Fact] void should_print_the_target_before_the_mappings() =>
        _roundtrip.Printed.ShouldContain("produces ContractPolicyActivated\n          for contractId\n          contractId = contractId");

    ProducesSyntax Produces(string @event) => Command.Produces.Single(_ => _.Event == @event);

    CommandSyntax Command =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
