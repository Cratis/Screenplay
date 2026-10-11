// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_suggesting_enum_member_numbers : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ImmutableArray<Diagnostic> _findings;

    void Because()
    {
        _result = new ScreenplayCompiler().Compile("concept Status : Enum\n  draft\n  issued\ntype Detail\n  status Status[] optional\ntype Envelope\n  detail Detail\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        envelope Envelope\n      command C\n        produces event Changed\n          status Status = \"draft\"");
        _findings = ModelCompleteness.Check(_result, new([CompletenessCheck.EnumPinning]));
    }

    [Fact] void should_not_hint_in_ordinary_compilation() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).ShouldBeFalse();
    [Fact] void should_not_hint_when_the_check_is_not_selected() => ModelCompleteness.Check(_result, CompletenessChecks.None).ShouldBeEmpty();
    [Fact] void should_hint_once_for_a_nested_and_inline_stored_enum() => _findings.Length.ShouldEqual(1);
    [Fact] void should_use_information_severity() => _findings.Single().Severity.ShouldEqual(DiagnosticSeverity.Information);
    [Fact] void should_point_to_the_enum() => _findings.Single().Location.Line.ShouldEqual(1);
    [Fact] void should_suggest_the_opt_in_refactoring() => _findings.Single().Message.ShouldContain("Number every member by its current position");
    [Fact] void should_include_the_hint_when_all_checks_are_selected() => ModelCompleteness.Check(_result, CompletenessChecks.All).Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).ShouldEqual(1);

    [Theory]
    [InlineData("enum-pinning")]
    [InlineData("PLAY0671")]
    void should_select_the_check_by_name_or_code(string selection)
    {
        CompletenessChecks.TryParse(selection, out var checks).ShouldBeTrue();
        checks.Selected.ShouldContainOnly(CompletenessCheck.EnumPinning);
        ModelCompleteness.Check(_result, checks).SequenceEqual(_findings).ShouldBeTrue();
    }

    [Theory]
    [InlineData("concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        status Status")]
    [InlineData("concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateView S\n      readmodel Row\n        status Status")]
    [InlineData("concept Status : Enum\n  draft = 0\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status")]
    [InlineData("concept Status : Enum\n  draft = 0\n  issued\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status")]
    [InlineData("concept Status : Enum\n  draft = nope\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status")]
    [InlineData("concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status\nconcept")]
    void should_not_hint_for_unstored_numbered_or_invalid_enums(string source) => ModelCompleteness.Check(new ScreenplayCompiler().Compile(source), new([CompletenessCheck.EnumPinning])).ShouldBeEmpty();
}
