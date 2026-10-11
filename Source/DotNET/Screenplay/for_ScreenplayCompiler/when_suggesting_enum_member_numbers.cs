// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_suggesting_enum_member_numbers : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile("concept Status : Enum\n  draft\n  issued\ntype Detail\n  status Status[] optional\ntype Envelope\n  detail Detail\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        envelope Envelope\n      command C\n        produces event Changed\n          status Status = \"draft\"");

    [Fact] void should_hint_once_for_a_nested_and_inline_stored_enum() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).ShouldEqual(1);
    [Fact] void should_use_information_severity() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).Severity.ShouldEqual(DiagnosticSeverity.Information);
    [Fact] void should_point_to_the_enum() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).Location.Line.ShouldEqual(1);
    [Fact] void should_suggest_the_opt_in_refactoring() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).Message.ShouldContain("Number every member by its current position");

    [Theory]
    [InlineData("concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        status Status")]
    [InlineData("concept Status : Enum\n  draft\nmodule M\n  feature F\n    slice StateView S\n      readmodel Row\n        status Status")]
    [InlineData("concept Status : Enum\n  draft = 0\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status")]
    [InlineData("concept Status : Enum\n  draft = 0\n  issued\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status")]
    [InlineData("concept Status : Enum\n  draft = nope\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status")]
    void should_not_hint_for_unstored_numbered_or_invalid_enums(string source) => new ScreenplayCompiler().Compile(source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnpinnedStoredEnumeration).ShouldBeFalse();
}
