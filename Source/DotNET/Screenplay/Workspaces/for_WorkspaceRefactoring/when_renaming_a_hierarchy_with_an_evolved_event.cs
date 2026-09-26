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
        var application = ApplicationIdentity.Create("Projects");
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(application));
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
        var renamedModule = kind == typeof(ModuleSyntax) ? $"{name}Renamed" : "Projects";
        var renamedFeature = kind == typeof(FeatureSyntax) ? $"{name}Renamed" : "Registration";
        var renamedSlice = kind == typeof(SliceSyntax) ? $"{name}Renamed" : "RegisterProject";
        var renamedEvent = kind == typeof(EventSyntax) ? $"{name}Renamed" : "Registered";
        AssertPropertiesAtAddress(before, result.Workspace.IdentityCatalog, application, renamedModule, renamedFeature, renamedSlice, renamedEvent);
    }

    [Fact]
    public void should_preserve_each_generation_property_identity_when_moving_the_slice()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n      event Registered generation 2\n        current String\n  feature Import\n";
        var application = ApplicationIdentity.Create("Projects");
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(application));
        var before = workspace.IdentityCatalog;
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var slice = index.Entries.Single(entry => entry.Node is SliceSyntax);
        var destination = index.Entries.Single(entry => entry.Node is FeatureSyntax feature && feature.Name == "Import");
        var originalSlice = SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject");
        var movedSlice = SemanticAddress.ForSlice(application, "Projects", "Import", "RegisterProject");
        var originalEvent = SemanticAddress.ForEventContract(originalSlice, "Registered");
        var movedEvent = SemanticAddress.ForEventContract(movedSlice, "Registered");
        var propertyRenames = new[] { (1u, "old"), (2u, "current") }.Select(pair => new SemanticIdentityRename(
            SemanticAddress.ForEventProperty(originalEvent, new(pair.Item1), pair.Item2),
            SemanticAddress.ForEventProperty(movedEvent, new(pair.Item1), pair.Item2)));
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = before.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [new MoveWorkspaceNode(slice.Handle, slice.Node, destination.Handle, destination.Node, "slices")],
            SemanticRenames = [new(originalSlice, movedSlice), new(originalEvent, movedEvent), .. propertyRenames],
            EventRenames = [new(originalEvent, movedEvent)]
        });

        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        AssertPropertiesAtAddress(before, result.Workspace.IdentityCatalog, application, "Projects", "Import", "RegisterProject", "Registered");
    }

    static void AssertPropertiesAtAddress(
        SemanticIdentityCatalog before,
        SemanticIdentityCatalog after,
        ApplicationIdentity application,
        string module,
        string feature,
        string slice,
        string @event)
    {
        var originalEvent = SemanticAddress.ForEventContract(
            SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject"), "Registered");
        var relocatedEvent = SemanticAddress.ForEventContract(
            SemanticAddress.ForSlice(application, module, feature, slice), @event);
        foreach (var (revision, property) in new[] { (1u, "old"), (2u, "current") })
        {
            var original = SemanticAddress.ForEventProperty(originalEvent, new(revision), property);
            var relocated = SemanticAddress.ForEventProperty(relocatedEvent, new(revision), property);
            after.ResolveSemantic(relocated).ShouldEqual(before.ResolveSemantic(original));
        }

        after.Semantics.Count(assignment => assignment.Address.Kind == SemanticKind.Property).ShouldEqual(2);
    }
}
