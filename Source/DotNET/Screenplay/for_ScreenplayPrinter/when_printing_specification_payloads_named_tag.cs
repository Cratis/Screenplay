// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_specification_payloads_named_tag : given.a_printer
{
    [Theory]
    [InlineData("given E", " none")]
    [InlineData("given E", "\n  item \"payload\"")]
    [InlineData("given E", " empty")]
    [InlineData("given E", "\n  name = \"payload\"")]
    [InlineData("given E", " = \"payload\"")]
    [InlineData("when C", " none")]
    [InlineData("when C", "\n  item \"payload\"")]
    [InlineData("when C", " empty")]
    [InlineData("when C", "\n  name = \"payload\"")]
    [InlineData("when C", " = \"payload\"")]
    [InlineData("when append E", " none")]
    [InlineData("when append E", "\n  item \"payload\"")]
    [InlineData("when append E", " empty")]
    [InlineData("when append E", "\n  name = \"payload\"")]
    [InlineData("when append E", " = \"payload\"")]
    [InlineData("when redelivered E to R", " none")]
    [InlineData("when redelivered E to R", "\n  item \"payload\"")]
    [InlineData("when redelivered E to R", " empty")]
    [InlineData("when redelivered E to R", "\n  name = \"payload\"")]
    [InlineData("when redelivered E to R", " = \"payload\"")]
    [InlineData("then E", " none")]
    [InlineData("then E", "\n  item \"payload\"")]
    [InlineData("then E", " empty")]
    [InlineData("then E", "\n  name = \"payload\"")]
    [InlineData("then E", " = \"payload\"")]
    void should_preserve_the_payload_without_treating_it_as_a_tag_assertion(string step, string value) =>
        AssertRoundTrip($"specification S\n  {step}\n    @tag{value.Replace("\n", "\n    ", StringComparison.Ordinal)}");

    [Theory]
    [InlineData(" none")]
    [InlineData("\n  item \"payload\"")]
    [InlineData(" empty")]
    [InlineData("\n  name = \"payload\"")]
    [InlineData(" = \"payload\"")]
    void should_preserve_the_payload_in_a_typed_example(string value) =>
        AssertRoundTrip($"example Fixture : E\n  @tag{value.Replace("\n", "\n  ", StringComparison.Ordinal)}\nspecification S\n  then Fixture");

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
        printed.ShouldContain("@tag");
        _printer.Print(reparsed.Value!).ShouldEqual(printed);
    }
}
