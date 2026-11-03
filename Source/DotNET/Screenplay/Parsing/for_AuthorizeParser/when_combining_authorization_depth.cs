// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Parsing.for_AuthorizeParser;

public class when_combining_authorization_depth
{
    [Theory]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(130)]
    [InlineData(258)]
    public void should_bound_authorize_lines_on_one_command(int count)
    {
        var source = "module Portal\n  feature Work\n    slice StateChange Save\n      command Save\n" + string.Join('\n', Enumerable.Repeat("        authorize Access", count));
        var result = new ScreenplayCompiler().Parse(source, "command.play");
        var gate = result.Value!.Modules.First().Features.First().Slices.First().Commands.First().Authorize;
        (gate is null).ShouldEqual(count > 128);
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0860").ToArray();
        diagnostics.Length.ShouldEqual(count > 128 ? 1 : 0);
        if (count > 128)
        {
            diagnostics[0].Location.Path.ShouldEqual("command.play");
            diagnostics[0].Location.Line.ShouldEqual(133);
        }
    }

    [Theory]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("query")]
    public void should_retain_a_refused_gate_after_later_lines(string owner)
    {
        var prefix = owner == "module" ? "module Portal\n" : owner == "feature" ? "module Portal\n  feature Work\n" : "module Portal\n  feature Work\n    slice StateView Browse\n      query Browse => Items\n";
        var indent = owner == "module" ? "  " : owner == "feature" ? "    " : "        ";
        var source = prefix + string.Join('\n', Enumerable.Repeat(indent + "authorize Access", 130)) + "\nmodule Other\n  authorize Access";
        var result = new ScreenplayCompiler().Parse(source, "owners.play");
        var module = result.Value!.Modules.First();
        var gate = owner == "module" ? module.Authorize : owner == "feature" ? module.Features.First().Authorize : module.Features.First().Slices.First().Queries.First().Authorize;
        gate.ShouldBeNull();
        result.Value.Modules.ElementAt(1).Authorize.ShouldNotBeNull();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0860").ShouldEqual(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void should_budget_a_deep_line_combined_with_more_gates(int count)
    {
        var chain = string.Join(" and ", Enumerable.Repeat("Access", 127));
        var source = "module Portal\n  authorize " + chain + "\n" + string.Join('\n', Enumerable.Repeat("  authorize Access", count));
        var result = new ScreenplayCompiler().Parse(source, "module.play");
        (result.Value!.Modules.First().Authorize is null).ShouldEqual(count == 2);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0860").ShouldEqual(count == 2 ? 1 : 0);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(128)]
    public void should_ignore_identical_deep_gates_from_additional_files(int depth)
    {
        var compiler = new ScreenplayCompiler();
        var source = "module Portal\n  authorize " + string.Join(" and ", Enumerable.Repeat("Access", depth));
        var result = PlayFolderMerge.Merge([compiler.Parse(source, "first.play"), compiler.Parse(source, "second.play")]);
        result.Value!.Modules.First().Authorize.ShouldNotBeNull();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0394").ShouldEqual(1);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0860").ShouldEqual(0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void should_not_restore_a_refused_gate_from_an_additional_file(bool feature, bool singleLine)
    {
        var compiler = new ScreenplayCompiler();
        var prefix = feature ? "module Area\n  feature Portal\n" : "module Portal\n";
        var indent = feature ? "    " : "  ";
        var refused = singleLine
            ? indent + "authorize " + string.Join(" and ", Enumerable.Repeat("Access", 129))
            : string.Join('\n', Enumerable.Repeat(indent + "authorize Access", 129));
        var result = PlayFolderMerge.Merge([
            compiler.Parse(prefix + refused, "a.play"),
            compiler.Parse(prefix + indent + "authorize Access", "b.play")]);
        var module = result.Value!.Modules.First();
        (feature ? module.Features.First().Authorize : module.Authorize).ShouldBeNull();
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0860");
        diagnostic.Location.Path.ShouldEqual("a.play");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void should_bound_gates_merged_from_additional_files(int count)
    {
        var compiler = new ScreenplayCompiler();
        var chain = string.Join(" and ", Enumerable.Repeat("Access", 127));
        var documents = Enumerable.Range(0, count).Select(index => compiler.Parse($"module Portal\n  authorize Extra{index}", $"{index + 1}.play"))
            .Prepend(compiler.Parse("module Portal\n  authorize " + chain, "0.play")).ToArray();
        var result = PlayFolderMerge.Merge(documents);
        (result.Value!.Modules.First().Authorize is null).ShouldEqual(count == 2);
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0860").ToArray();
        diagnostics.Length.ShouldEqual(count == 2 ? 1 : 0);
        if (count == 2)
        {
            diagnostics[0].Location.Path.ShouldEqual("2.play");
            diagnostics[0].Location.Line.ShouldEqual(2);
            diagnostics[0].Location.Column.ShouldEqual(3);
        }
    }
}
