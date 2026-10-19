// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations;

public class when_resolving_fixtures : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                name String
                produces ProjectRegistered
                  name = name
              event ProjectRegistered generation 1
                name String
              event ProjectRegistered generation 2
                name String
                title String
        module Archive
          feature Registration
            slice StateChange ArchiveProject
              event ProjectArchived generation 1
                name String
              event ProjectArchived generation 2
                name String
              event ProjectArchived generation 3
                name String
              event ProjectRegistered
                name String
        """;

    WorkspaceSyntaxIndex _index = null!;
    WorkspaceSyntaxEntry _use = null!;
    WorkspaceFixtureCandidates _first = null!;
    WorkspaceFixtureCandidates _second = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _index = WorkspaceSyntaxIndex.Create(workspace);
        _use = _index.Entries.First(entry => entry.Node is CommandSyntax);
    }

    void Because()
    {
        _first = WorkspaceFixtureCandidates.For(_index);
        _second = WorkspaceFixtureCandidates.For(_index);
    }

    [Fact] void should_compute_the_candidates_once_per_index() => _second.ShouldBeSame(_first);
    [Fact] void should_keep_the_latest_generation_per_scope() => Generations("ProjectRegistered").ShouldContainOnly(1u, 2u);
    [Fact] void should_resolve_to_the_latest_generation() => ((EventSyntax)WorkspaceFixtureDeclarations.Resolve("ProjectRegistered", _use, _index)!.Node).Generation.ShouldEqual(Latest("ProjectRegistered"));
    [Fact] void should_keep_the_latest_generation_of_another_scope() => Generations("ProjectArchived").ShouldContainOnly(Latest("ProjectArchived"));
    [Fact] void should_keep_one_candidate_per_scope() => _first.Entries.Count(entry => entry.Declaration.Name == "ProjectRegistered").ShouldEqual(2);
    [Fact] void should_keep_an_older_generation_that_is_the_latest_in_its_own_scope() => ((EventSyntax)WorkspaceFixtureDeclarations.Resolve("Archive.Registration.ArchiveProject.ProjectRegistered", _use, _index)!.Node).Generation.ShouldEqual(1u);
    [Fact] void should_resolve_across_scopes_by_qualified_name() => WorkspaceFixtureDeclarations.Resolve("Archive.Registration.ArchiveProject.ProjectArchived", _use, _index).ShouldNotBeNull();
    [Fact] void should_not_resolve_an_unknown_name() => WorkspaceFixtureDeclarations.Resolve("Missing", _use, _index).ShouldBeNull();

    uint Latest(string name) => _index.Entries.Select(entry => entry.Node).OfType<EventSyntax>().Where(@event => @event.Name == name).Max(@event => @event.Generation);

    IEnumerable<uint> Generations(string name) => _first.Entries.Select(entry => entry.Entry.Node).OfType<EventSyntax>().Where(@event => @event.Name == name).Select(@event => @event.Generation);
}
