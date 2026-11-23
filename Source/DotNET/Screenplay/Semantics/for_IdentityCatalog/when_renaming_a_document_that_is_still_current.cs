// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_renaming_a_document_that_is_still_current : Specification
{
    SemanticIdentityCatalog _previous;
    Exception _exception;

    void Establish() => _previous = SemanticIdentityCatalog.Create(
        ApplicationIdentity.Create("Projects"),
        [new("original", DocumentId.Create("original"), SemanticIdentityOrigin.Persisted)],
        [],
        []);

    void Because() => _exception = Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
        _previous,
        _previous.Revision,
        ["original", "renamed"],
        [],
        [],
        [new("original", "renamed")],
        [],
        []));

    [Fact] void should_preserve_the_document_rename_error() => _exception.Message.ShouldEqual("Document rename 'original' to 'renamed' is stale or invalid.");
    [Fact] void should_report_an_invalid_semantic_contract() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
