// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_CaptureParser;

public class when_rejecting_host_language_when_clauses
{
    static IEnumerable<Diagnostic> Warnings(string when) =>
        new ScreenplayCompiler()
            .CompileCapture($"capture C\n  map\n    a = $.a\n  append Added\n    when {when}\n      a = $.a\n")
            .Diagnostics.Where(_ => _.Code == DiagnosticCodes.HostLanguageExpression);

    [Fact]
    public void should_warn_once_for_a_property_clause() =>
        Warnings("a && b").Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);

    [Theory]
    [InlineData("`a && b`")]
    [InlineData("`f(a) || b`")]
    [InlineData("a or b")]
    [InlineData("a")]
    public void should_not_warn_for_admitted_clauses(string when) => Warnings(when).ShouldBeEmpty();
}
