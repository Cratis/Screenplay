// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_mixed_enum_member_numbers : Specification
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = [.. new[] { "draft\n  issued = 4", "draft = 1\n  issued" }.Select(body => new ScreenplayCompiler().Parse($"concept Status : Enum\n  {body}"))];

    [Fact] void should_refuse_both_numbering_orders() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_the_distinct_mixed_numbering_error() => _results.All(result => result.Diagnostics.Single().Code == DiagnosticCodes.MixedEnumerationNumbering).ShouldBeTrue();
}
