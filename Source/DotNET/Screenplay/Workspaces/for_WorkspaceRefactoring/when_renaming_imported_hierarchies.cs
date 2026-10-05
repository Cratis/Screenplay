// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_imported_hierarchies
{
    public static TheoryData<string, string, WorkspaceAuthoringFormatting> Cases
    {
        get
        {
            var cases = new TheoryData<string, string, WorkspaceAuthoringFormatting>();
            foreach (var layout in new[] { "scoped", "native", "barrels", "transitive" })
            {
                foreach (var name in new[] { "M", "F" })
                {
                    foreach (var formatting in new[] { WorkspaceAuthoringFormatting.PreserveTrivia, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments })
                    {
                        cases.Add(layout, name, formatting);
                    }
                }
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void should_resolve_final_placements_and_preserve_every_assigned_identity(string layout, string name, WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace(layout);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        index.Diagnostics.ShouldBeEmpty();
        var target = index.Entries.First(entry => entry.Address?.Name == name && entry.Node is ModuleSyntax or FeatureSyntax);
        var before = WorkspaceImplementationInventory.Create(index).Entries.Single();
        var request = Request(workspace, target.Handle, name, formatting);
        workspace.ProposeRename(request with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
        workspace.ProposeRename(request with { ExpectedCatalogRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
        var result = workspace.ProposeRename(request);
        result.Accepted.ShouldBeTrue();
        var candidate = result.Workspace!;
        var after = WorkspaceImplementationInventory.Create(candidate).Entries.Single();
        after.Owner.Parts.Single(part => part.Kind == SemanticAddressPartKind.Module).Key.ShouldEqual(name == "M" ? "N" : "M");
        after.Owner.Parts.Single(part => part.Kind == SemanticAddressPartKind.Feature).Key.ShouldEqual(name == "F" ? "N" : "F");
        after.OwnerId.ShouldEqual(before.OwnerId);
        after.RequirementId.ShouldEqual(before.RequirementId);
        after.File.ShouldEqual(before.File);
        after.Hints.SequenceEqual(before.Hints).ShouldBeTrue();
        after.IdentityOrigin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        candidate.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(id => id.ToString()).ShouldEqual(
            workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).OrderBy(id => id.ToString()));
        var leaf = candidate.Documents.Single(document => document.Path.Value == "leaf.play");
        leaf.Text.ShouldContain("slice StateChange S");
        leaf.Text.Contains("module ", StringComparison.Ordinal).ShouldBeFalse();
        leaf.Text.Contains("feature ", StringComparison.Ordinal).ShouldBeFalse();
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            foreach (var original in workspace.Documents)
            {
                var expected = original.Text.Replace($"module {name}", "module N", StringComparison.Ordinal)
                    .Replace($"feature {name}", "feature N", StringComparison.Ordinal)
                    .Replace($"module  {name}", "module  N", StringComparison.Ordinal);
                candidate.Documents.Single(document => document.Id == original.Id).Bytes.ToArray().ShouldEqual(Encoding.UTF8.GetBytes(expected));
            }
        }

        WorkspaceImplementationInventory.Create(ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(candidate))).Entries.Single().RequirementId.ShouldEqual(before.RequirementId);
    }

    [Fact]
    public void should_refuse_candidate_placement_conflicts_with_a_typed_actionable_verdict()
    {
        var workspace = Workspace("scoped");
        var root = workspace.Documents.Single(document => document.Path.Value == "root.play");
        var syntax = new ScreenplayCompiler().Parse(root.Text + "  feature G\r\n    import \"leaf.play\"\r\n").Value!;
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(root.Id, syntax)]
        });
        result.Accepted.ShouldBeFalse();
        result.Workspace.ShouldBeNull();
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        result.Conflicts.Single().Message.ShouldContain("UnresolvedPlacement:");
    }

    static WorkspaceRenameRequest Request(ScreenplayWorkspace workspace, WorkspaceNodeHandle handle, string name, WorkspaceAuthoringFormatting formatting) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Target = handle,
        ExpectedName = name,
        NewName = "N",
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = formatting
    };

    static ScreenplayWorkspace Workspace(string layout)
    {
        WorkspaceDocument[] documents = layout switch
        {
            "scoped" => [Document("root.play", "module M // root\n  feature F // feature\n    import \"leaf.play\"\n")],
            "native" => [Document("root.play", "module M // root\n  import \"native.play\"\n"), Document("native.play", "// restated\nmodule  M // native\n  feature F // feature\n    import \"leaf.play\"\nmodule M // restated again\n")],
            "barrels" => [Document("root.play", "import \"module.play\"\n"), Document("module.play", "module M // root\n  import \"feature.play\"\n"), Document("feature.play", "feature F // feature\n  import \"leaf.play\"\n")],
            _ => [Document("root.play", "module M // root\n  feature F // feature\n    import \"barrel.play\"\n"), Document("barrel.play", "// transitive\nimport \"leaf.play\"\n")]
        };
        var application = ApplicationIdentity.Create("A");
        var provisional = ScreenplayWorkspace.Create("A", [.. documents, Document("leaf.play", "// payload\nslice StateChange S\n  command C\n    value String\n    handler\n      implementation\n        hint \"Keep\"\n        file C.cs\n")], SemanticIdentityCatalog.Empty(application));
        var assignments = WorkspaceSyntaxIndex.Create(provisional).Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!).Distinct()
            .Select(address => new SemanticIdentityAssignment(address, SemanticId.Create(address), SemanticIdentityOrigin.Persisted));
        return ScreenplayWorkspace.Create("A", provisional.Documents, SemanticIdentityCatalog.Create(application, [], [.. assignments], []));
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source.Replace("\n", "\r\n", StringComparison.Ordinal)));
}
