// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_a_document_key_is_supplied_again : given.a_workspace_with_assigned_events
{
    ScreenplayWorkspace _rehomed = null!;

    void Because() => _rehomed = Original.Rehome([Document("memory", "application.play", Concepts), Split[1]]);

    [Fact] void should_keep_the_assigned_document_id() => _rehomed.Documents.Single(document => document.StableKey == "memory").Id.ShouldEqual(Original.Documents.Single().Id);
    [Fact] void should_keep_the_document_assignment_origin() => _rehomed.IdentityCatalog.Documents.Single(assignment => assignment.Key == "memory").ShouldEqual(Original.IdentityCatalog.Documents.Single());
}
