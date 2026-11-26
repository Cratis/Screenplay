// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_specification_payloads_named_tag : given.a_printer
{
    [Theory]
    [InlineData("given E", "@tag none")]
    [InlineData("given E", "@tag\n  item \"payload\"")]
    [InlineData("given E", "@tag empty")]
    [InlineData("given E", "@tag\n  name = \"payload\"")]
    [InlineData("given E", "tag = \"payload\"")]
    [InlineData("when C", "@tag none")]
    [InlineData("when C", "@tag\n  item \"payload\"")]
    [InlineData("when C", "@tag empty")]
    [InlineData("when C", "@tag\n  name = \"payload\"")]
    [InlineData("when C", "tag = \"payload\"")]
    [InlineData("when append E", "@tag none")]
    [InlineData("when append E", "@tag\n  item \"payload\"")]
    [InlineData("when append E", "@tag empty")]
    [InlineData("when append E", "@tag\n  name = \"payload\"")]
    [InlineData("when append E", "tag = \"payload\"")]
    [InlineData("when redelivered E to R", "@tag none")]
    [InlineData("when redelivered E to R", "@tag\n  item \"payload\"")]
    [InlineData("when redelivered E to R", "@tag empty")]
    [InlineData("when redelivered E to R", "@tag\n  name = \"payload\"")]
    [InlineData("when redelivered E to R", "tag = \"payload\"")]
    [InlineData("then E", "@tag none")]
    [InlineData("then E", "@tag\n  item \"payload\"")]
    [InlineData("then E", "@tag empty")]
    [InlineData("then E", "@tag\n  name = \"payload\"")]
    [InlineData("then E", "tag = \"payload\"")]
    void should_preserve_the_payload_without_treating_it_as_a_tag_assertion(string step, string value) =>
        AssertRoundTrip($"specification S\n  {step}\n    {value.Replace("\n", "\n    ", StringComparison.Ordinal)}");

    [Theory]
    [InlineData("@tag none")]
    [InlineData("@tag\n  item \"payload\"")]
    [InlineData("@tag empty")]
    [InlineData("@tag\n  name = \"payload\"")]
    [InlineData("tag = \"payload\"")]
    void should_preserve_the_payload_in_a_typed_example(string value) =>
        AssertRoundTrip($"example Fixture : E\n  {value.Replace("\n", "\n  ", StringComparison.Ordinal)}\nspecification S\n  then Fixture");

    [Fact]
    void should_keep_payloads_distinct_from_real_tag_assertions() => AssertRoundTrip(
        """
        specification S
          then E
            @tag none
            tag none
            tag "billing"
          then E
            @tag empty
            no tags
        """);

    void AssertRoundTrip(string source)
    {
        var original = _compiler.CompileSpecification(source);
        var printed = _printer.Print(original.Value!);
        var reparsed = _compiler.CompileSpecification(printed);
        original.Diagnostics.ShouldBeEmpty();
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(original.Value!, reparsed.Value!).ShouldBeTrue();
        printed.Contains("@tag", StringComparison.Ordinal).ShouldEqual(source.Contains("@tag", StringComparison.Ordinal));
        _printer.Print(reparsed.Value!).ShouldEqual(printed);
    }
}
