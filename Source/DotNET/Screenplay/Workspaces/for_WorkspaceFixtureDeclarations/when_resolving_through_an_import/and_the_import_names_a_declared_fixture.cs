// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations.when_resolving_through_an_import;

public class and_the_import_names_a_declared_fixture : given.a_fixture_model
{
    WorkspaceSyntaxEntry? _result;

    protected override string Source => $"import Archive.Registration.ArchiveProject.ProjectArchived\n{Model}";

    void Because() => _result = WorkspaceFixtureDeclarations.Resolve("ProjectArchived", Command("RegisterProject"), Index);

    [Fact] void should_resolve_to_the_declared_fixture() => _result.ShouldEqual(Event("Archive", "ProjectArchived", 3));
}
