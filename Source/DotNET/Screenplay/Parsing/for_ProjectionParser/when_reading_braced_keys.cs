// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ProjectionParser;

public class when_reading_braced_keys
{
    const string Braced = "projection Totals => Totals\n  from PaymentAdded\n    key {\n      invoiceId = invoice\n      method = method\n    }\n    total = amount\n  remove with PaymentRemoved\n    key Composite {\n      invoiceId = invoice\n    }\n";
    const string Plain = "projection Totals => Totals\n  from PaymentAdded\n    key\n      invoiceId = invoice\n      method = method\n    total = amount\n  remove with PaymentRemoved\n    key Composite\n      invoiceId = invoice\n";

    [Fact]
    void should_report_one_information_diagnostic_per_braced_key_at_the_key_line()
    {
        var diagnostics = new ScreenplayCompiler().CompileProjection(Braced).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantCompositeKeyBraces).ToArray();
        diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual([3, 9]);
        diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Information).ShouldBeTrue();
    }

    [Fact]
    void should_not_report_keys_written_without_braces() =>
        new ScreenplayCompiler().CompileProjection(Plain).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantCompositeKeyBraces).ShouldBeFalse();

    [Fact]
    void should_parse_to_the_same_syntax_as_the_form_without_braces()
    {
        var compiler = new ScreenplayCompiler();
        var braced = compiler.CompileProjection(Braced);
        braced.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(braced.Value, compiler.CompileProjection(Plain).Value).ShouldBeTrue();
    }
}
