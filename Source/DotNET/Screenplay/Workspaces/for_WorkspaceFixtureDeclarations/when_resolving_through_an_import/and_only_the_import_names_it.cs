// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations.when_resolving_through_an_import;

public class and_only_the_import_names_it : given.a_fixture_model
{
    WorkspaceSyntaxEntry? _result;

    protected override string Source => $"import External.ProjectSuspended\n{Model}";

    void Because() => _result = WorkspaceFixtureDeclarations.Resolve("ProjectSuspended", Command("RegisterProject"), Index);

    [Fact] void should_not_resolve_to_a_fixture() => _result.ShouldBeNull();
}
