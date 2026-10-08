// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_numeric_literals;

public class and_legacy_stream_keys_exceed_int64 : given.a_printer
{
    [Theory]
    [InlineData("100000000000000000000")]
    [InlineData("-100000000000000000000")]
    [InlineData("1000000000000000000000")]
    [InlineData("-1000000000000000000000")]
    void should_preserve_printed_literal_stream_keys_but_refuse_the_double_mode_bound(string token)
    {
        var source = $$"""
            concept Key : Int
            eventsource Account
              stream Profile
                streamId Key
            module M
              feature F
                slice StateChange S
                  command C
                    stream Account.Profile
                      streamId = {{token}}
                    produces E
                  event E
                  specification Numeric
                    when C
                    then E
                      stream Account.Profile
                        streamId = {{token}}
            """;
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Success.ShouldBeFalse();
        roundtrip.Original.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray().ShouldEqual(["PLAY0504", "PLAY0549"]);
        roundtrip.Reparsed.Success.ShouldBeFalse();
        roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray().ShouldEqual(["PLAY0504", "PLAY0549"]);
        var before = roundtrip.Original.Value!.Modules.Single().Features.Single().Slices.Single();
        var after = roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();
        var commandKey = after.Commands.Single().Stream!.StreamId!.Source;
        var specificationKey = after.Specifications.Single().ThenEvents.Single().Stream!.StreamId!.Source;
        commandKey.ShouldBeOfExactType<LiteralExpressionSyntax>();
        specificationKey.ShouldBeOfExactType<LiteralExpressionSyntax>();
        ((LiteralExpressionSyntax)commandKey).Value.ShouldEqual(((LiteralExpressionSyntax)before.Commands.Single().Stream!.StreamId!.Source).Value);
        ((LiteralExpressionSyntax)specificationKey).Value.ShouldEqual(((LiteralExpressionSyntax)before.Specifications.Single().ThenEvents.Single().Stream!.StreamId!.Source).Value);
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
