// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ReleaseBaseline.given;

public class a_baseline_workspace : Specification
{
    protected const string Source = """
        concept Protected : String pii
          pii special health
          pii criminal
        concept Status : Enum
          draft = 1
          sent = 4
        type Details
          value Protected
          status Status
        module Records
          feature Recording
            slice StateChange Record
              event Recorded generation 1
                details Details[]
              event Recorded generation 2
                details Details[]
                note String?
        """;

    protected ScreenplayWorkspace Workspace = null!;
    protected ReleaseBaselineRead Absent = null!;
    protected ReleaseBaselineDocument Baseline = null!;
    protected static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-10-12T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records"));
        var document = WorkspaceDocument.Create(catalog.ResolveDocument("application"), "application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source));
        Workspace = ScreenplayWorkspace.Create("Records", [document], catalog);
        Assert.True(Workspace.Compilation.Success, string.Join('\n', Workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Absent = ReleaseBaselineStore.ReadBytes(null, Workspace.IdentityCatalog.Application);
        Baseline = ReleaseBaseline.Admit(Workspace, Absent, "v1.0.0", At).Document!;
    }

    protected ScreenplayWorkspace Changed(string source) => ScreenplayWorkspace.Create("Records", [WorkspaceDocument.Create(Workspace.Documents[0].Id, "application", Workspace.Documents[0].Path, Encoding.UTF8.GetBytes(source))], Workspace.IdentityCatalog);

    protected ReleaseBaselineRead Read(ReleaseBaselineDocument document) => ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(document), Workspace.IdentityCatalog.Application);
}
