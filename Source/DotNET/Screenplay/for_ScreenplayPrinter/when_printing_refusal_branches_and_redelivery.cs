// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_refusal_branches_and_redelivery : given.a_printer
{
    const string Source =
        """
        module Billing
          feature Payments
            slice Automation Claiming
              event Approved
                id String
              event Refused
                reason String
                constraint String
                message String
              command Claim
                id String
              reaction Claimer
                when Approved
                  id
                  invokes Claim
                    id = id
                    on refused by constraint OneClaim
                      produces Refused
                        for id
                        reason = $refusal.reason
                        constraint = $refusal.constraint
                        message = $refusal.message
                    on refused by validation
                      acknowledge
                    on refused
                      acknowledge
                    on refused by authorization
                      acknowledge
              specification Redelivering
                given Approved
                  for "a"
                  id = "a"
                when redelivered Approved to Claimer
                  for "a"
                  id = "a"
                then no events
        """;

    RoundTripResult _roundtrip;
    ApplicationSyntax _restored;

    void Because()
    {
        _roundtrip = RoundTrip(Source);
        _restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_roundtrip.Original!.Value!));
    }

    SliceSyntax Slice => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();
    InvokesSyntax Invocation => Slice.Reactions.Single().Triggers.Single().Invokes!.Single();

    [Fact] void should_keep_branch_order() => Invocation.OnRefused.Select(branch => branch.Selector).ShouldContainOnly("constraint", "validation", "any", "authorization");
    [Fact] void should_keep_the_named_constraint() => Invocation.OnRefused.First().Constraint.ShouldEqual("OneClaim");
    [Fact] void should_keep_the_production_mapping_members() => Invocation.OnRefused.First().Produces.Single().Mappings.Select(mapping => ((RefusalExpressionSyntax)mapping.Source).Member).ShouldContainOnly("reason", "constraint", "message");
    [Fact] void should_keep_acknowledgement() => Invocation.OnRefused.Skip(1).All(branch => branch.Acknowledge && !branch.Produces.Any()).ShouldBeTrue();
    [Fact] void should_keep_the_redelivery_reaction() => Slice.Specifications.Single().WhenRedelivered!.Reaction.ShouldEqual("Claimer");
    [Fact] void should_keep_the_redelivery_source() => ((LiteralExpressionSyntax)Slice.Specifications.Single().WhenRedelivered!.For!).Value.ShouldEqual("a");
    [Fact] void should_keep_the_redelivery_values() => Slice.Specifications.Single().WhenRedelivered!.Values.Single().Property.ShouldEqual("id");
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_round_trip_syntax_json() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _restored).ShouldBeTrue();
}
