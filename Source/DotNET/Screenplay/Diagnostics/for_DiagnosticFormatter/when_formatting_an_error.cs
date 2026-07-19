// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics.for_DiagnosticFormatter;

public class when_formatting_an_error : Specification
{
    const string Source = "module Invoicing\n  feature Invoices\n    slice Wrong Register";

    DiagnosticFormatter _formatter;
    Diagnostic _diagnostic;
    string _plain;
    string _colored;

    void Establish()
    {
        _formatter = new();
        _diagnostic = Diagnostic.Error("Unknown slice type 'Wrong'", new(3, 5));
    }

    void Because()
    {
        _plain = _formatter.Format("invoicing.play", _diagnostic, Source, useColors: false);
        _colored = _formatter.Format("invoicing.play", _diagnostic, Source, useColors: true);
    }

    [Fact] void should_format_the_header_with_location() => _plain.ShouldContain("invoicing.play(3,5): error: Unknown slice type 'Wrong'");
    [Fact] void should_include_the_offending_line() => _plain.ShouldContain("    3 |     slice Wrong Register");
    [Fact] void should_point_the_caret_at_the_column() => _plain.ShouldContain("      |     ^");
    [Fact] void should_use_ansi_colors_when_asked() => _colored.ShouldContain("\e[31m");
    [Fact] void should_not_use_ansi_colors_by_default() => _plain.Contains('\e').ShouldBeFalse();
}
