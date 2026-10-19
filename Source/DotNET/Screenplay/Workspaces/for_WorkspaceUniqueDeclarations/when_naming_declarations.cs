// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceUniqueDeclarations;

public class when_naming_declarations : Specification
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
              event ProjectRegistered
                name String
              event ProjectRenamed
                name String
        module Archive
          feature Registration
            slice StateChange ArchiveProject
              event ProjectRenamed
                name String
        """;

    WorkspaceSyntaxIndex _index = null!;
    WorkspaceUniqueDeclarations _first = null!;
    WorkspaceUniqueDeclarations _second = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _index = WorkspaceSyntaxIndex.Create(workspace);
    }

    void Because()
    {
        _first = WorkspaceUniqueDeclarations.For(_index);
        _second = WorkspaceUniqueDeclarations.For(_index);
    }

    [Fact] void should_compute_the_declarations_once_per_index() => _second.ShouldBeSame(_first);
    [Fact] void should_name_a_unique_declaration_as_a_scan_does() => _first.Named<EventSyntax>("ProjectRegistered").ShouldBeSame(Scanned<EventSyntax>("ProjectRegistered"));
    [Fact] void should_name_a_unique_declaration() => _first.Named<EventSyntax>("ProjectRegistered").ShouldNotBeNull();
    [Fact] void should_not_name_a_duplicated_declaration() => _first.Named<EventSyntax>("ProjectRenamed").ShouldBeNull();
    [Fact] void should_not_name_a_duplicated_declaration_as_a_scan_does() => Scanned<EventSyntax>("ProjectRenamed").ShouldBeNull();
    [Fact] void should_not_name_a_declaration_of_another_kind() => _first.Named<CommandSyntax>("ProjectRegistered").ShouldBeNull();
    [Fact] void should_not_name_an_unknown_declaration() => _first.Named<EventSyntax>("Missing").ShouldBeNull();
    [Fact] void should_not_name_without_a_name() => _first.Named<EventSyntax>(null).ShouldBeNull();

    T? Scanned<T>(string name)
        where T : SyntaxNode => _index.Entries.Select(entry => entry.Node).OfType<T>().Where(node => WorkspaceReferenceBindings.Name(node) == name).Take(2).ToArray() is [var match] ? match : null;
}
