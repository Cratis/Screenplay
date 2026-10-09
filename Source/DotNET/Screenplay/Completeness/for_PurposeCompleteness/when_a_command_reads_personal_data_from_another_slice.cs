// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Processing;

namespace Cratis.Screenplay.Completeness.for_PurposeCompleteness;

public class when_a_command_reads_personal_data_from_another_slice : Specification
{
    ImmutableArray<Diagnostic> _findings;
    ProcessingRecordRow _billing;
    ProcessingRecordRow _unrelated;

    void Because()
    {
        var compilation = new ScreenplayCompiler().Compile("""
            concept PersonName : String pii
              pii special health
              pii criminal
            concept ApiKey : String secret
              secret scope global
            type Inner
              name PersonName
              key ApiKey
            type Details
              inner Inner
            purpose Billing
              basis contract
            purpose Unrelated
              basis consent
            module M
              feature F
                slice StateView Stored
                  readmodel Contact
                    details Details
                slice StateChange Record
                  purpose Billing
                  command Record
                    reads Contact
                slice StateChange Uncovered
                  command Export
                    reads Contact
                slice StateChange Other
                  purpose Unrelated
                  command Other
                    value String
            """);
        compilation.Diagnostics.ShouldBeEmpty();
        _findings = ModelCompleteness.Check(compilation, new([CompletenessCheck.Purposes]));
        var report = ProcessingRecord.Create(compilation.Value!);
        _billing = report.Rows.Single(row => row.Purpose == "Billing");
        _unrelated = report.Rows.Single(row => row.Purpose == "Unrelated");
    }

    [Fact] void should_find_uncovered_read_model_and_command_data() => _findings.Where(diagnostic => diagnostic.Code == DiagnosticCodes.PersonalDataWithoutPurpose).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(17, 24);
    [Fact] void should_prompt_for_a_special_condition_on_the_reading_purpose() => _findings.Count(diagnostic => diagnostic.Code == DiagnosticCodes.SpecialDataWithoutCondition).ShouldEqual(1);
    [Fact] void should_prompt_for_criminal_authorization_on_the_reading_purpose() => _findings.Count(diagnostic => diagnostic.Code == DiagnosticCodes.CriminalDataWithoutAuthorization).ShouldEqual(1);
    [Fact] void should_derive_the_read_model_personal_categories_through_composites() => _billing.Categories.ShouldContainOnly("PersonName");
    [Fact] void should_derive_the_special_category() => _billing.SpecialCategories.ShouldContainOnly("health");
    [Fact] void should_derive_criminal_data() => _billing.CriminalData.ShouldBeTrue();
    [Fact] void should_derive_both_protection_mappings() => _billing.SecurityMeasures.ShouldContainOnly("Declared [PII]: per-subject encryption and crypto-shredding", "Declared [Encrypted] scope global and [NotAudited]");
    [Fact] void should_not_attribute_other_slices_to_an_unrelated_purpose() => _unrelated.Categories.ShouldBeEmpty();
}
