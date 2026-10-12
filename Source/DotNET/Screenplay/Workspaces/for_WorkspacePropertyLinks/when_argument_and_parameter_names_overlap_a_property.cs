// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_argument_and_parameter_names_overlap_a_property : given.a_model
{
    WorkspaceSyntaxEntry[] _entries;
    AuthoringIndex _index;
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _incoming;

    void Establish()
    {
        var snapshot = new AuthoringSnapshot(new Dictionary<string, string> { ["model.play"] = Source });
        var entries = WorkspacePropertyLinks.Entries([snapshot.Compilation.Value!]).ToArray();
        var screen = entries.Single(entry => entry.Node is ScreenSyntax);
        SyntaxNode[] arguments =
        [
            new BehaviorParameterSyntax("name", null, new(100, 1, "model.play")),
            new BehaviorArgumentSyntax("name", "constant", new(101, 1, "model.play")),
            new InteractionArgumentSyntax("name", "constant", new(102, 1, "model.play")),
            new BehaviorArgumentSyntax("other", "NAME", new(103, 1, "model.play")),
            new ObjectMemberSyntax("name", new LiteralExpressionSyntax("constant", new(104, 1, "model.play")), new(104, 1, "model.play"))
        ];
        _entries = [.. entries, .. arguments.Select((node, index) => new WorkspaceSyntaxEntry
        {
            Handle = screen.Handle with { Path = $"{screen.Handle.Path}/arguments/{index}" },
            Parent = screen.Handle,
            Node = node
        })];
        _index = snapshot.Index;
    }

    void Because()
    {
        _links = new(_entries, _index);
        _incoming = [.. _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Where(link => link.Reference.Location.Line >= 100)];
    }

    [Fact] void should_exclude_only_the_parameter_declaration() => _incoming.Select(link => link.Reference.Location.Line).ShouldContainOnly(101, 102, 103, 104);
    [Fact] void should_still_scan_the_argument_value() => _incoming.Single(link => link.Reference.Location.Line == 103).Reference.Name.ShouldEqual("name");
    [Fact] void should_report_unclassified_argument_labels_and_values() => _incoming.All(link => link.Reference.Role == "unclassified").ShouldBeTrue();
    [Fact] void should_keep_the_genuine_read_incomplete() => _links.Complete(_links.Find("Projects.Registration.Lookup.Project.name").Single()).ShouldBeFalse();
}
