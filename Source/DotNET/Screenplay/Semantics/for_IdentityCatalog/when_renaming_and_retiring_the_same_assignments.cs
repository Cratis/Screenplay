// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_renaming_and_retiring_the_same_assignments : Specification
{
    SemanticIdentityCatalog _previous;
    SemanticAddress _original;
    SemanticAddress _renamed;
    InvalidSemanticContract _exception;

    void Establish()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject");
        _original = SemanticAddress.ForEventContract(slice, "ProjectRegistered");
        _renamed = SemanticAddress.ForEventContract(slice, "ProjectCreated");
        _previous = SemanticIdentityCatalog.Create(
            application,
            [new("original", DocumentId.Create("original"), SemanticIdentityOrigin.Persisted)],
            [new(_original, SemanticId.Create(_original), SemanticIdentityOrigin.Persisted)],
            [new(_original, EventContractId.CreateLegacy(application, _original.Name), new(1), SemanticIdentityOrigin.Persisted)]);
    }

    void Because() => _exception = (InvalidSemanticContract)Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
        _previous,
        _previous.Revision,
        ["renamed"],
        [_renamed],
        [_renamed],
        [new("original", "renamed")],
        [new(_original, _renamed)],
        [new(_original, _renamed)],
        ["original"],
        [_original],
        [_original]));

    [Fact] void should_report_both_typed_retirement_issues_before_the_document_error() => _exception.IdentityMigrationIssues.Length.ShouldEqual(2);
    [Fact] void should_report_the_semantic_retirement_first() => _exception.IdentityMigrationIssues[0].Arguments.ShouldContainOnly("retiredSemanticAddresses");
    [Fact] void should_report_the_event_retirement_second() => _exception.IdentityMigrationIssues[1].Arguments.ShouldContainOnly("retiredEventAddresses");
    [Fact] void should_keep_the_original_address_on_each_issue() => _exception.IdentityMigrationIssues.All(issue => issue.Address.Equals(_original)).ShouldBeTrue();
    [Fact] void should_preserve_the_retirement_error_message() => _exception.Message.ShouldContain("An explicit identity retirement is stale, still current, or also renamed. Identity migration issues: ");
}
