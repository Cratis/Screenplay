// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PurposeCompleteness;

public class when_checking_nested_personal_data : Specification
{
    ImmutableArray<Diagnostic> _findings;
    ImmutableArray<Diagnostic> _ordinary;

    void Because()
    {
        var compilation = new ScreenplayCompiler().Compile("""
            concept HealthNote : String pii
              pii special health
              pii criminal
            type Details
              note HealthNote
            purpose Care
            purpose Unused
              basis consent
            module Services
              purpose Care
              feature Intake
                feature Records
                  slice StateChange Record
                    command Record
                      details Details
            """);
        _ordinary = [.. compilation.Diagnostics];
        _findings = ModelCompleteness.Check(compilation, new([CompletenessCheck.Purposes]));
    }

    [Fact] void should_not_run_completeness_during_compilation() => _ordinary.ShouldBeEmpty();
    [Fact] void should_inherit_module_purpose_coverage() => _findings.Any(diagnostic => diagnostic.Code == DiagnosticCodes.PersonalDataWithoutPurpose).ShouldBeFalse();
    [Fact] void should_prompt_for_a_basis() => _findings.Count(diagnostic => diagnostic.Code == DiagnosticCodes.PurposeWithoutBasis).ShouldEqual(1);
    [Fact] void should_prompt_for_a_special_condition() => _findings.Count(diagnostic => diagnostic.Code == DiagnosticCodes.SpecialDataWithoutCondition).ShouldEqual(1);
    [Fact] void should_prompt_for_criminal_authorization() => _findings.Count(diagnostic => diagnostic.Code == DiagnosticCodes.CriminalDataWithoutAuthorization).ShouldEqual(1);
    [Fact] void should_prompt_for_an_unused_purpose() => _findings.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnusedPurpose).ShouldEqual(1);
}
