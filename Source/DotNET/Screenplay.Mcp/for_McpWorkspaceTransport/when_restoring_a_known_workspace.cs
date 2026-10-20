// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaceTransport;

public class when_restoring_a_known_workspace : Specification
{
    ScreenplayWorkspace _known = null!;
    ScreenplayWorkspace _changed = null!;
    ScreenplayWorkspace _restored = null!;
    ScreenplayWorkspace _restoredChange = null!;
    ScreenplayWorkspace _restoredWithoutKnown = null!;

    void Establish()
    {
        _known = Create("module Orders\n  feature Fulfilment\n");
        _changed = Create("module Sales\n  feature Fulfilment\n");
    }

    void Because()
    {
        _restored = McpWorkspaceTransport.Restore(Encoding.UTF8.GetString(McpWorkspaceTransport.ExportBytes(_known)), _known);
        _restoredChange = McpWorkspaceTransport.Restore(Encoding.UTF8.GetString(McpWorkspaceTransport.ExportBytes(_changed)), _known);
        _restoredWithoutKnown = McpWorkspaceTransport.Restore(Encoding.UTF8.GetString(McpWorkspaceTransport.ExportBytes(_known)));
    }

    [Fact] void should_reuse_the_known_workspace_for_its_exact_export() => _restored.ShouldBeSame(_known);
    [Fact] void should_not_reuse_the_known_workspace_for_a_changed_export() => ReferenceEquals(_restoredChange, _known).ShouldBeFalse();
    [Fact] void should_restore_the_changed_revision() => _restoredChange.Revision.ShouldEqual(_changed.Revision);
    [Fact] void should_restore_the_same_revision_without_the_known_workspace() => _restoredWithoutKnown.Revision.ShouldEqual(_known.Revision);
    [Fact] void should_restore_the_same_export_without_the_known_workspace() => McpWorkspaceTransport.ExportBytes(_restoredWithoutKnown).SequenceEqual(McpWorkspaceTransport.ExportBytes(_known)).ShouldBeTrue();

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create(
        "Acme",
        [WorkspaceDocument.Create("orders", PortablePlayPath.Parse("orders.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
}
