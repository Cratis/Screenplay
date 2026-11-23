// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_navigation_screen_targets_in_free_screen_outlets : given.a_compiler
{
    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    void should_accept_a_selected_page_in_an_adopted_free_screen(string scope) =>
        _compiler.Compile(Source(scope)).Success.ShouldBeTrue();

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    void should_preserve_legacy_feature_targets_in_an_adopted_free_screen(string scope) =>
        _compiler.Compile(Source(scope, selection: "")).Success.ShouldBeTrue();

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    void should_reject_a_missing_selected_screen(string scope)
    {
        var result = _compiler.Compile(Source(scope, selection: " screen Missing"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownNavigationScreen).ShouldEqual(1);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    void should_reject_an_ambiguous_selected_screen(string scope)
    {
        var result = _compiler.Compile(Source(scope, extra: "    slice StateView Duplicate\n      screen Projects\n"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousNavigationScreen).ShouldEqual(1);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    void should_reject_a_selected_screen_that_does_not_fit_page(string scope)
    {
        var result = _compiler.Compile(Source(scope, fits: "        fits dialog\n"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.NonPageNavigationScreen).ShouldEqual(1);
    }

    static string Source(string scope, string selection = " screen Projects", string fits = "", string extra = "")
    {
        var screen = $"screen Shared\n  outlet sidebar extends\n    item selected title \"Selected\" target Shop.Projects{selection}\n";
        return when_compiling_a_navigation_screen_target.Layout +
            (scope == "application" ? screen : "") +
            "module Shop\n" +
            (scope == "module" ? Indent(screen, 2) : "") +
            "  feature Projects\n" +
            (scope == "feature" ? Indent(screen, 4) : "") +
            "    slice StateView Listing\n      uses screen Shared\n      screen Projects\n" + fits + extra;
    }

    static string Indent(string source, int spaces) =>
        string.Join('\n', source.TrimEnd('\n').Split('\n').Select(line => new string(' ', spaces) + line)) + "\n";
}
