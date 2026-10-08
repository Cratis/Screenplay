// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_combining_example_steps_with_routes : Specification
{
    ApplicationSyntax _authored;
    EffectiveSpecificationApplication _expanded;

    void Establish() => _authored = new ScreenplayCompiler().Compile(
        """
        eventsource Account
          identifier String
          stream Events
            streamId String
        module M
          feature F
            slice StateChange S
              event Happened
                amount Int
              command Act
                amount Int
                produces Happened
                  amount = amount
              example Fixture : Happened
                amount = 1
              specification Routed
                given Fixture amount = 2
                  for "account"
                  stream Account.Events
                    streamId = "partition"
                when Act
                  amount = 3
                then Fixture amount = 3
                  no stream
        """).Value!;

    void Because() => _expanded = SpecificationExamples.Expand(_authored);

    [Fact] void should_keep_the_authored_example_reference() => _authored.Modules.Single().Features.Single().Slices.Single().Specifications.Single().Given.Single().EventType.ShouldEqual("Fixture");
    [Fact] void should_expand_without_diagnostics() => _expanded.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_given_route() => _expanded.Specifications.Single().Effective.Given.Single().Stream!.Stream.ShouldEqual("Events");
    [Fact] void should_keep_the_stream_identity() => ((LiteralExpressionSyntax)_expanded.Specifications.Single().Effective.Given.Single().Stream!.StreamId!.Source).Value.ShouldEqual("partition");
    [Fact] void should_keep_the_inline_override() => ((LiteralExpressionSyntax)_expanded.Specifications.Single().Effective.Given.Single().Values.Single().Source).Value.ShouldEqual(2d);
    [Fact] void should_keep_the_no_stream_assertion() => _expanded.Specifications.Single().Effective.ThenEvents.Single().NoStream.ShouldNotBeNull();
}
