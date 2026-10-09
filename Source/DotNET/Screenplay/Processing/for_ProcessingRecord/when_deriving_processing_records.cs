// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Processing.for_ProcessingRecord;

public class when_deriving_processing_records : Specification
{
    ProcessingRecord _report;
    ProcessingRecordRow _ledger;

    void Because()
    {
        var application = new ScreenplayCompiler().Compile("""
            concept Note : String pii secret
              pii special health
              pii criminal
            concept ApiKey : String secret
              secret scope global
            type Inner
              note Note
            type Details
              inner Inner
              key ApiKey
            purpose Billing
              basis contract
            purpose Ledger
              basis legalObligation "Accounting law"
              erasure exception legalObligation
              retention "Five years"
              recipient "Tax authority"
              transfer "Country" safeguard "Clauses"
            purpose Unused
              basis consent
            module M
              purpose Billing
              feature F
                purpose Ledger
                slice StateChange Record
                  command Record
                    details Details
            """);
        application.Success.ShouldBeTrue();
        _report = ProcessingRecord.Create(application.Value!, "Example controller", "contact@example.test");
        _ledger = _report.Rows.Single(row => row.Purpose == "Ledger");
    }

    [Fact] void should_return_one_row_per_declared_purpose() => _report.Rows.Count.ShouldEqual(3);
    [Fact] void should_derive_personal_categories_not_operational_secrets() => _ledger.Categories.ShouldContainOnly("Note");
    [Fact] void should_derive_special_categories() => _ledger.SpecialCategories.ShouldContainOnly("health");
    [Fact] void should_identify_criminal_data() => _ledger.CriminalData.ShouldBeTrue();
    [Fact] void should_prompt_for_assessment_without_asserting_scale() => _ledger.DpiaPrompt!.ShouldContain("Processing scale is not declared");
    [Fact] void should_keep_the_pii_only_mapping_for_combined_markers() => _ledger.SecurityMeasures.ShouldContainOnly("Declared [PII]: per-subject encryption and crypto-shredding", "Declared [Encrypted] scope global and [NotAudited]");
    [Fact] void should_find_the_erasure_conflict() => _ledger.Findings.Single().ShouldContain("deleting the subject key");
    [Fact] void should_not_find_an_erasure_conflict_without_an_exception() => _report.Rows.Single(row => row.Purpose == "Billing").Findings.ShouldBeEmpty();
    [Fact] void should_expose_qualified_covered_slices() => _ledger.CoveredSlices.ShouldContainOnly("M.F.Record");
    [Fact] void should_preserve_declared_transfers() => _ledger.Transfers.Single().Safeguard.ShouldEqual("Clauses");
    [Fact] void should_keep_unused_purposes_without_inventing_data() => _report.Rows.Single(row => row.Purpose == "Unused").Categories.ShouldBeEmpty();
    [Fact] void should_use_only_supplied_controller_information() => _report.ControllerName.ShouldEqual("Example controller");
    [Fact] void should_include_the_notice() => _report.Notice.ShouldEqual("Generated from declarations in this model. Not legal advice.");
}
