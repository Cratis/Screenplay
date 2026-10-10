// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadsParser;

public class when_parsing_a_by_block : Specification
{
    readonly ScreenplayCompiler _compiler = new();
    const string Prefix = "module M\n  feature F\n    slice Automation S\n";

    [Fact]
    void should_parse_in_a_command()
    {
        var result = _compiler.Parse(Prefix + "      command C\n        reads View as row\n          by\n            resourceId = resourceId\n            period = month");
        result.Diagnostics.ShouldBeEmpty();
        var read = result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Reads!.Single();
        read.Alias.ShouldEqual("row");
        read.By.ShouldBeNull();
        read.ByParts.Select(part => part.Property).ShouldEqual("resourceId", "period");
        ((PathExpressionSyntax)read.ByParts.Last().Source).Path.ShouldEqual("month");
    }

    [Fact]
    void should_parse_under_a_reaction_trigger()
    {
        var result = _compiler.Parse(Prefix + "      reaction R\n        when E\n          reads View\n            by\n              resourceId = resourceId\n              period = month");
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single().Reads!.Single().ByParts.Count().ShouldEqual(2);
    }

    [Fact]
    void should_refuse_other_children() => _compiler.Parse(Prefix + "      command C\n        reads View\n          where invalid").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ReadsWithChildren);
}
