// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_an_application_context : Specification
{
    [Theory]
    [InlineData("context\n  description \"Zones the business reasons in.\"\n  businessTimeZone time zone\n  customerTimeZone time zone\n")]
    [InlineData("context\n  @description time zone\n")]
    [InlineData("context\n")]
    public void should_print_canonical_source_and_round_trip(string source)
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse(source);
        var printer = new ScreenplayPrinter();
        var printed = printer.Print(parsed.Value!);
        printed.TrimEnd().ShouldEqual(source.TrimEnd());
        var reparsed = compiler.Parse(printed);
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.Serialize(reparsed.Value!).GetRawText().ShouldEqual(SyntaxJson.Serialize(parsed.Value!).GetRawText());
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        printer.Print(restored).ShouldEqual(printed);
    }

    [Fact]
    void should_print_after_the_application_time_zone()
    {
        const string source = "time zone \"Europe/Oslo\"\n\ncontext\n  businessTimeZone time zone\n";
        var parsed = new ScreenplayCompiler().Parse(source);
        new ScreenplayPrinter().Print(parsed.Value!).TrimEnd().ShouldEqual(source.TrimEnd());
    }
}
