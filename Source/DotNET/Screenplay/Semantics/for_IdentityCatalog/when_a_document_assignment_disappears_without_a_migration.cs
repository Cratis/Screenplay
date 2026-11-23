// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_a_document_assignment_disappears_without_a_migration : Specification
{
    SemanticIdentityCatalog _previous;
    InvalidSemanticContract _exception;

    void Establish() => _previous = SemanticIdentityCatalog.Create(
        ApplicationIdentity.Create("Projects"),
        [new("original", DocumentId.Create("original"), SemanticIdentityOrigin.Persisted)],
        [],
        []);

    void Because() => _exception = (InvalidSemanticContract)Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
        _previous,
        _previous.Revision,
        [],
        [],
        [],
        [],
        [],
        []));

    [Fact] void should_preserve_the_document_continuity_error() => _exception.Message.ShouldEqual("A base catalog assignment is stale and has no explicit one-to-one rename or retirement.");
    [Fact] void should_not_report_typed_address_issues() => _exception.IdentityMigrationIssues.ShouldBeEmpty();
}
