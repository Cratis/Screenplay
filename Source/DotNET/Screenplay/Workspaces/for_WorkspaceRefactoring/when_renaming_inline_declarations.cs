// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_inline_declarations
{
    const string Source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces event Renamed\n          name String = name\n";

    [Fact]
    void should_assign_the_same_addresses_and_identities_as_a_standalone_event()
    {
        var inline = Create(Source);
        var standalone = Create(Source.Replace("produces event Renamed\n          name String = name", "produces Renamed\n          for projectId\n          name = name\n      event Renamed\n        name String", StringComparison.Ordinal));
        var inlineIndex = WorkspaceSyntaxIndex.Create(inline);
        var standaloneIndex = WorkspaceSyntaxIndex.Create(standalone);
        var declaration = inlineIndex.Entries.Single(entry => entry.Node is EventSyntax);
        declaration.Address.ShouldEqual(standaloneIndex.Entries.Single(entry => entry.Node is EventSyntax).Address);
        declaration.SemanticId.ShouldEqual(standaloneIndex.Entries.Single(entry => entry.Node is EventSyntax).SemanticId);
        declaration.EventContractId.ShouldEqual(standaloneIndex.Entries.Single(entry => entry.Node is EventSyntax).EventContractId);
        declaration.SemanticId.ShouldNotBeNull();
        declaration.EventContractId.ShouldNotBeNull();
        var property = inlineIndex.Entries.Single(entry => entry.Node is PropertySyntax && entry.Parent == declaration.Handle);
        property.Address.ShouldEqual(SemanticAddress.ForProperty(declaration.Address!, "name"));
        property.SemanticId.ShouldNotBeNull();
        property.SemanticId.ShouldEqual(standalone.IdentityCatalog.ResolveSemantic(property.Address!));
    }

    [Theory]
    [InlineData(typeof(EventSyntax), "Renamed")]
    [InlineData(typeof(SliceSyntax), "Rename")]
    void should_preserve_event_and_property_identities_when_renaming(Type kind, string name)
    {
        var workspace = Create(Source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => entry.Node.GetType() == kind);
        var declaration = index.Entries.Single(entry => entry.Node is EventSyntax);
        var property = index.Entries.Single(entry => entry.Node is PropertySyntax && entry.Parent == declaration.Handle);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = name,
            NewName = $"{name}Again"
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        var after = WorkspaceSyntaxIndex.Create(result.Workspace);
        var renamedEvent = after.Entries.Single(entry => entry.Node is EventSyntax);
        renamedEvent.SemanticId.ShouldEqual(declaration.SemanticId);
        renamedEvent.EventContractId.ShouldEqual(declaration.EventContractId);
        after.Entries.Single(entry => entry.Node is PropertySyntax && entry.Parent == renamedEvent.Handle).SemanticId.ShouldEqual(property.SemanticId);
        if (kind == typeof(EventSyntax))
        {
            after.Entries.Select(entry => entry.Node).OfType<ProducesSyntax>().Single().Event.ShouldEqual("RenamedAgain");
        }
    }

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Projects",
        [WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
}
