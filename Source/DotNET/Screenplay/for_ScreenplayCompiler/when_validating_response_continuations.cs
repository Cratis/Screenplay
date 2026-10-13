// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_response_continuations : given.a_compiler
{
    const string Commands = "module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        returns\n          receipt = value\n      command D\n        value String\n        returns\n          receipt = value\n      screen Entry\n        on submit\n";

    [Theory]
    [InlineData("notify error receipt")]
    [InlineData("confirm receipt")]
    [InlineData("set selected to receipt")]
    [InlineData("navigate to Entry\n                with id from receipt")]
    void should_reject_returned_names_only_in_failure_value_operands(string action)
    {
        var result = _compiler.Compile(Commands + "          execute C\n            on failure\n              " + action);
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ResponseNameUsedOnFailure);
        diagnostic.Message.ShouldEqual("'receipt' is returned by 'C' only on success");
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact]
    void should_restore_failure_scope_after_a_nested_success()
    {
        var result = _compiler.Compile(Commands + "          execute C\n            on failure\n              execute D\n                on success\n                  notify info receipt\n              notify error receipt");
        result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.ResponseNameUsedOnFailure).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(19);
    }

    [Fact]
    void should_keep_outer_success_available_in_inner_failure()
    {
        var result = _compiler.Compile(Commands + "          execute C\n            on success\n              execute D\n                on failure\n                  notify error receipt");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("notify error \"receipt\"")]
    [InlineData("confirm $strings.receipt")]
    [InlineData("set receipt to item.receipt")]
    [InlineData("navigate to Entry\n                with id from receipt + other")]
    void should_leave_literals_paths_and_opaque_text_unchanged(string action)
    {
        var result = _compiler.Compile(Commands + "          execute C\n            on failure\n              " + action);
        result.Diagnostics.ShouldBeEmpty();
    }
}
