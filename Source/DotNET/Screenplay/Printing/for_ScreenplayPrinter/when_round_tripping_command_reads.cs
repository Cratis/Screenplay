// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_command_reads : Specification
{
    [Fact]
    void should_preserve_the_mode_through_ast_json_and_canonical_source()
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse("reaction batches atomic\ncommand reads protected\nmodule M\n  feature F\n    slice StateChange S\n      command Submit\n        value String");
        parsed.Success.ShouldBeTrue();
        var json = SyntaxJson.Serialize(parsed.Value!);
        json.GetProperty("commandReadMode").GetString().ShouldEqual("Protected");
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(json);
        restored.CommandReadMode.ShouldEqual(CommandReadMode.Protected);
        var printer = new ScreenplayPrinter();
        var printed = printer.Print(restored);
        printed.ShouldContain("command reads protected");
        var reparsed = compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.Serialize(reparsed.Value!).GetRawText().ShouldEqual(json.GetRawText());
    }

    [Fact]
    void should_omit_the_mode_from_legacy_source_and_json()
    {
        var application = new ScreenplayCompiler().Parse("module M").Value!;
        SyntaxJson.Serialize(application).TryGetProperty("commandReadMode", out _).ShouldBeFalse();
        new ScreenplayPrinter().Print(application).Contains("command reads", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_an_unknown_programmatic_mode() =>
        Catch.Exception(() => new ScreenplayPrinter().Print(new ScreenplayCompiler().Parse(string.Empty).Value! with { CommandReadMode = (CommandReadMode)99 })).ShouldBeOfExactType<UnsupportedSyntaxForPrinting>();
}
