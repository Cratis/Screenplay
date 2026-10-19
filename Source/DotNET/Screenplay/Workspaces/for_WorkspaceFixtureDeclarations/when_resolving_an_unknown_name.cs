// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations;

public class when_resolving_an_unknown_name : given.a_fixture_model
{
    WorkspaceSyntaxEntry? _result;

    void Because() => _result = WorkspaceFixtureDeclarations.Resolve("Missing", Command("RegisterProject"), Index);

    [Fact] void should_not_resolve() => _result.ShouldBeNull();
}
