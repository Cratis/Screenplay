// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations;

public class when_resolving_generations : given.a_fixture_model
{
    WorkspaceSyntaxEntry? _latest;
    WorkspaceSyntaxEntry? _latestOfAnotherScope;
    WorkspaceSyntaxEntry? _onlyGenerationOfAnotherScope;

    void Because()
    {
        _latest = WorkspaceFixtureDeclarations.Resolve("ProjectRegistered", Command("RegisterProject"), Index);
        _latestOfAnotherScope = WorkspaceFixtureDeclarations.Resolve("Archive.Registration.ArchiveProject.ProjectArchived", Command("RegisterProject"), Index);
        _onlyGenerationOfAnotherScope = WorkspaceFixtureDeclarations.Resolve("Archive.Registration.ArchiveProject.ProjectRegistered", Command("RegisterProject"), Index);
    }

    [Fact] void should_resolve_to_the_latest_generation() => ((EventSyntax)_latest!.Node).Generation.ShouldEqual(2u);
    [Fact] void should_resolve_to_the_latest_generation_of_another_scope() => ((EventSyntax)_latestOfAnotherScope!.Node).Generation.ShouldEqual(3u);
    [Fact] void should_resolve_an_older_generation_that_is_the_latest_in_its_own_scope() => _onlyGenerationOfAnotherScope.ShouldEqual(Event("Archive", "ProjectRegistered", 1));
}
