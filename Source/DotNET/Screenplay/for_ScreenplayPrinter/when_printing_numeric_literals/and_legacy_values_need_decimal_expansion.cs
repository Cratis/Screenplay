// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_numeric_literals;

public class and_legacy_values_need_decimal_expansion : given.a_printer
{
    [Theory]
    [InlineData("9223372036854774784")]
    [InlineData("-9223372036854775808")]
    [InlineData("9223372036854775808")]
    [InlineData("-9223372036854777856")]
    [InlineData("100000000000000000000")]
    [InlineData("-100000000000000000000")]
    [InlineData("1000000000000000000000")]
    [InlineData("-1000000000000000000000")]
    [InlineData("0.0000001")]
    [InlineData("-0.0000001")]
    [InlineData("0.00000000000000000001234567890123456")]
    [InlineData("-0.00000000000000000001234567890123456")]
    void should_preserve_payload_and_stream_literals_and_values(string token)
    {
        var source = $$"""
            specification Numeric
              given E
                stream Account.Profile
                  streamId = {{token}}
                amount = {{token}}
              when append E
                stream Account.Profile
                  streamId = {{token}}
                amount = {{token}}
              then E
                stream Account.Profile
                  streamId = {{token}}
                amount = {{token}}
            """;
        var original = _compiler.CompileSpecification(source);
        original.Success.ShouldBeTrue();
        var printed = _printer.Print(original.Value!);
        var reparsed = _compiler.CompileSpecification(printed);
        reparsed.Success.ShouldBeTrue();
        var before = original.Value!;
        var after = reparsed.Value!;
        var originalValues = new[]
        {
            before.Given.Single().Values.Single().Source,
            before.Given.Single().Stream!.StreamId!.Source,
            before.WhenAppended!.Values.Single().Source,
            before.WhenAppended.Stream!.StreamId!.Source,
            before.ThenEvents.Single().Values.Single().Source,
            before.ThenEvents.Single().Stream!.StreamId!.Source
        };
        var reparsedValues = new[]
        {
            after.Given.Single().Values.Single().Source,
            after.Given.Single().Stream!.StreamId!.Source,
            after.WhenAppended!.Values.Single().Source,
            after.WhenAppended.Stream!.StreamId!.Source,
            after.ThenEvents.Single().Values.Single().Source,
            after.ThenEvents.Single().Stream!.StreamId!.Source
        };
        foreach (var (expected, actual) in originalValues.Zip(reparsedValues))
        {
            expected.ShouldBeOfExactType<LiteralExpressionSyntax>();
            actual.ShouldBeOfExactType<LiteralExpressionSyntax>();
            ((LiteralExpressionSyntax)actual).Value.ShouldEqual(((LiteralExpressionSyntax)expected).Value);
        }
        _printer.Print(after).ShouldEqual(printed);
    }
}
