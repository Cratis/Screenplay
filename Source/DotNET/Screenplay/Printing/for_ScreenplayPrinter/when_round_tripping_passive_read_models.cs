// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_passive_read_models : Specification
{
    [Fact]
    void should_preserve_passivity_in_syntax_json_and_canonical_source()
    {
        var compiler = new ScreenplayCompiler();
        const string source = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n        id String\n        passive";
        var parsed = compiler.Parse(source);
        parsed.Success.ShouldBeTrue();
        var json = SyntaxJson.Serialize(parsed.Value!);
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(json);
        var printed = new ScreenplayPrinter().Print(restored);
        printed.ShouldContain("passive");
        var reparsed = compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        reparsed.Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single().IsPassive.ShouldBeTrue();
        new ScreenplayPrinter().Print(reparsed.Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_print_a_repeated_directive_only_once_before_properties()
    {
        var parsed = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView S\n      readmodel Row\n        passive\n        id String\n        passive");
        parsed.Success.ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        printed.Split('\n').Count(line => line.Trim() == "passive").ShouldEqual(1);
        (printed.IndexOf("passive", StringComparison.Ordinal) < printed.IndexOf("id String", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_not_emit_the_directive_for_active_models() => new ScreenplayPrinter().Print(new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateView S\n      readmodel Row\n        id String").Value!).Contains("passive", StringComparison.Ordinal).ShouldBeFalse();
}
