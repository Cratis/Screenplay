// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_validation_severities : given.a_printer
{
    const string Source =
        """
        concept Label : String
          validate
            not empty severity information message "Required"
            max 10 severity warning
            min 2 severity error
        module Sales
          feature Orders
            slice StateChange Submit
              command Submit
                label Label
                validate
                  label not empty severity warning message "Missing"
                  label max 10 severity information
                  label min 2
                  require label == "okay"
                    severity information
                    message "Not okay"
                  require label != "bad"
                    severity warning
                  require label != "other"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_omit_default_severity() => _roundtrip.Printed.ShouldNotContain("severity error");
    [Fact] void should_preserve_concept_severities() => ConceptRules.Select(_ => _.Severity).SequenceEqual([ValidationSeverity.Information, ValidationSeverity.Warning, ValidationSeverity.Error]).ShouldBeTrue();
    [Fact] void should_preserve_command_severities() => CommandRules.Select(_ => _.Severity).SequenceEqual([ValidationSeverity.Warning, ValidationSeverity.Information, ValidationSeverity.Error]).ShouldBeTrue();
    [Fact] void should_preserve_requirement_severities() => Requirements.Select(_ => _.Severity).SequenceEqual([ValidationSeverity.Information, ValidationSeverity.Warning, ValidationSeverity.Error]).ShouldBeTrue();
    [Fact] void should_print_severity_before_message() => _roundtrip.Printed.ShouldContain("severity warning message \"Missing\"");

    IEnumerable<ValidationRuleSyntax> ConceptRules => _roundtrip.Reparsed.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules;
    DeclarativeValidateSyntax CommandValidation => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.OfType<DeclarativeValidateSyntax>().Single();
    IEnumerable<ValidationRuleSyntax> CommandRules => CommandValidation.Rules;
    IEnumerable<RequirementSyntax> Requirements => CommandValidation.Requirements!;
}
