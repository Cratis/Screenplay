// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations;

public class when_resolving_a_bare_name : given.a_fixture_model
{
    WorkspaceSyntaxEntry? _result;

    void Because() => _result = WorkspaceFixtureDeclarations.Resolve("ProjectRegistered", Command("RegisterProject"), Index);

    [Fact] void should_resolve_to_the_declaration_in_the_innermost_scope() => _result.ShouldEqual(Event("Projects", "ProjectRegistered", 2));
}
