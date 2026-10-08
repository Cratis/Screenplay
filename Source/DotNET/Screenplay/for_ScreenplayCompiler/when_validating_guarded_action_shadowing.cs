// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_guarded_action_shadowing
{
    [Theory]
    [InlineData("item.status == \"open\"", "item.status == \"open\"", true)]
    [InlineData("item.status == \"open\"", "item.status == \"open\" and item.ready == true", true)]
    [InlineData("item.status == \"open\" or item.status == \"failed\"", "item.status == \"failed\" or item.status == \"open\"", true)]
    [InlineData("item.status == \"open\" and item.ready == true", "item.status == \"open\"", false)]
    [InlineData("item.status == \"open\"", "item.status == \"open\" or item.status == \"failed\"", false)]
    [InlineData("item.ready == true", "item.status == \"open\"", false)]
    [InlineData("item.status == \"open\"", "item.status == \"failed\"", false)]
    public void should_warn_only_for_provable_shadowing(string earlier, string later, bool shadowed)
    {
        var result = Compile($"when {earlier} execute Retry\nwhen {later} execute Retry\notherwise execute Retry");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableActionAlternative).ShouldEqual(shadowed ? 1 : 0);
        result.Diagnostics.All(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableActionAlternative).ShouldBeTrue();
    }

    [Fact]
    public void should_prove_coverage_by_several_earlier_alternatives()
    {
        var result = Compile("when item.status == \"open\" execute Retry\nwhen item.status == \"failed\" execute Retry\nwhen item.status == \"open\" or item.status == \"failed\" execute Retry");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnreachableActionAlternative);
    }

    [Fact]
    public void should_skip_expansions_above_the_disjunct_cap()
    {
        var condition = string.Join(" and ", Enumerable.Range(0, 7).Select(index => $"(item.status == \"a{index}\" or item.status == \"b{index}\")"));
        Compile($"when item.status == \"a0\" or item.status == \"b0\" execute Retry\nwhen {condition} execute Retry").Diagnostics.ShouldBeEmpty();
    }

    static CompilationResult<Syntax.ApplicationSyntax> Compile(string alternatives)
    {
        const string Source = "module Work\n  feature Items\n    slice StateView Details\n      readmodel Item\n        status String\n        ready Boolean\n      query Details => Item\n      command Retry\n      screen Details\n        data Item via query Details\n        action \"Again\"\n          ";
        return new ScreenplayCompiler().Compile(Source + alternatives.Replace("\n", "\n          ", StringComparison.Ordinal));
    }
}
