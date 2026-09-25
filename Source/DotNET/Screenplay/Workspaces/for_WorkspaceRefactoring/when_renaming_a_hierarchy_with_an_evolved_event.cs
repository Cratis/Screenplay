// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_hierarchy_with_an_evolved_event
{
    [Theory]
    [InlineData(typeof(ModuleSyntax), "Projects")]
    [InlineData(typeof(FeatureSyntax), "Registration")]
    [InlineData(typeof(SliceSyntax), "RegisterProject")]
    [InlineData(typeof(EventSyntax), "Registered")]
    public void should_preserve_every_generation_property_identity(Type kind, string name)
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n      event Registered generation 2\n        current String\n";
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var before = workspace.IdentityCatalog;
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => entry.Node.GetType() == kind && entry.Address?.Name == name);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = before.Revision,
            Target = target.Handle,
            ExpectedName = name,
            NewName = $"{name}Renamed"
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.Workspace.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(before.EventContracts.Single().Id);
        result.Workspace.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
        result.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(id => id.ToString())
            .SequenceEqual(before.Semantics.Select(assignment => assignment.Id).OrderBy(id => id.ToString())).ShouldBeTrue();
        var properties = result.Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Property).ToArray();
        properties.Length.ShouldEqual(2);
        properties.All(assignment => assignment.Address.Parts[^3].Kind == SemanticAddressPartKind.Generation).ShouldBeTrue();
    }
}
