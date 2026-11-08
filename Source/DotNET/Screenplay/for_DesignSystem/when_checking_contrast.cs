// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_DesignSystem.given;

namespace Cratis.Screenplay.for_DesignSystem;

public class when_checking_contrast : given.a_compiler
{
    const string Pale = "blue  = \"#1D4ED8\"";
    const string PaleReplacement = "blue  = \"#93C5FD\"";

    IEnumerable<Diagnostic> _below;
    IEnumerable<Diagnostic> _disabledOnly;
    IEnumerable<Diagnostic> _fixed;

    void Because()
    {
        var below = a_design_system_document.Source.Replace(Pale, PaleReplacement);
        _below = Contrast(below);

        // Ink on pale blue passes for the variant itself; only the disabled state pairs pale blue with white, and disabled is excluded.
        var disabledOnly = below.Replace("color.on-primary = palette.white", "color.on-primary = palette.ink")
            .Replace("                  text       = color.text\n                  background = color.surface", "                  text       = color.primary\n                  background = color.surface");
        _disabledOnly = Contrast(disabledOnly);
        _fixed = Contrast(a_design_system_document.Source);
    }

    [Fact] void should_warn_in_each_mode_the_pair_is_below_the_rule() => (_below.Any(diagnostic => diagnostic.Message.Contains("(light)", StringComparison.Ordinal)), _below.Any(diagnostic => diagnostic.Message.Contains("(dark)", StringComparison.Ordinal))).ShouldEqual((true, true));
    [Fact] void should_report_warnings() => _below.ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    [Fact] void should_name_the_pair_and_the_required_ratio() => _below.First().Message.ShouldContain("below the required 4.5");
    [Fact] void should_not_check_the_disabled_state() => _disabledOnly.ShouldBeEmpty();
    [Fact] void should_report_nothing_once_the_theme_fixes_the_pair() => _fixed.ShouldBeEmpty();

    IEnumerable<Diagnostic> Contrast(string source) => _compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.ContrastBelowRule);
}
