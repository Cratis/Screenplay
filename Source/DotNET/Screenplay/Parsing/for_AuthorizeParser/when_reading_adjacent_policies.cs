// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_AuthorizeParser;

public class when_reading_adjacent_policies
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n";

    [Theory]
    [InlineData("        authorize Staff Managers\n", "        authorize Staff and Managers\n")]
    [InlineData("        authorize (Staff or Managers) Auditors\n", "        authorize (Staff or Managers) and Auditors\n")]
    [InlineData("        authorize Staff and Managers\n          Auditors\n", "        authorize Staff and Managers\n          and Auditors\n")]
    [InlineData("        authorize Staff\n          Managers // both\n", "        authorize Staff\n          and Managers // both\n")]
    void should_report_one_information_diagnostic_at_the_authorize_line_and_parse_as_the_explicit_form(string implicitForm, string explicitForm)
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.Parse(Prefix + implicitForm);
        var reported = parsed.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.ImplicitAuthorizationConjunction).ToArray();
        reported.Length.ShouldEqual(1);
        reported[0].Severity.ShouldEqual(DiagnosticSeverity.Information);
        reported[0].Location.Line.ShouldEqual(5);
        var explicitParsed = compiler.Parse(Prefix + explicitForm);
        explicitParsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ImplicitAuthorizationConjunction).ShouldBeFalse();
        SyntaxJson.StructurallyEqual(parsed.Value, explicitParsed.Value).ShouldBeTrue();
    }

    [Theory]
    [InlineData("        authorize Staff\n")]
    [InlineData("        authorize Staff and Managers\n")]
    [InlineData("        authorize Staff or Managers\n")]
    [InlineData("        authorize Staff\n          or Managers\n")]
    void should_not_report_explicit_operators(string source) =>
        new ScreenplayCompiler().Parse(Prefix + source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ImplicitAuthorizationConjunction).ShouldBeFalse();
}
