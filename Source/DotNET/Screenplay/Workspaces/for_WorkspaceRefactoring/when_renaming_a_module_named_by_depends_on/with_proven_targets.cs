// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_module_named_by_depends_on;

public class with_proven_targets
{
    [Theory]
    [InlineData("Timesheets", "Time", "depends on Time.Approval")]
    [InlineData("Approval", "Review", "depends on Timesheets.Review")]
    [InlineData("Runs", "Execution", "depends on Execution")]
    public void should_repair_proven_module_feature_and_sibling_targets(string oldName, string newName, string expected)
    {
        const string source = "module Payroll\n  depends on Timesheets\n  feature Handover\n    depends on Timesheets.Approval\n    depends on Runs\n  feature Runs\nmodule Timesheets\n  feature Approval\n";
        var result = Rename(source, oldName, newName);
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.Workspace!.Documents.Single().Text.ShouldContain(expected);
    }

    internal static WorkspaceAuthoringResult Rename(string source, string oldName, string newName)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Example", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Example")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => entry.Node is ModuleSyntax or FeatureSyntax && entry.Address?.Name == oldName);

        return workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = oldName,
            NewName = newName
        });
    }
}
