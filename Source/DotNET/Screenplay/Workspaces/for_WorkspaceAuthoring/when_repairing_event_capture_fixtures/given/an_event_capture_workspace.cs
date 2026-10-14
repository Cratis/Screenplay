// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_event_capture_fixtures.given;

public class an_event_capture_workspace : Specification
{
    protected ScreenplayWorkspace _workspace;
    protected WorkspaceSyntaxIndex _index;
    protected Diagnostic _diagnostic;

    protected void Open(string steps, bool severalSources = false)
    {
        const string Header = """
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Arrived from "store"
                    id String
                  event Updated from "store"
                    id String
                  event Local
                    id String
                  capture Feed
                    source events
                      from Arrived
            """;
        var source = Header + "\n" + (severalSources ? "          from Updated\n" : string.Empty) + "        key id\n        append Local\n          id = $.id\n      specification S\n" +
            string.Join('\n', steps.Split('\n').Select(line => "        " + line)) + "\n";
        _workspace = ScreenplayWorkspace.Create("Repair", [WorkspaceDocument.Create("repair", PortablePlayPath.Parse("repair.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Repair")));
        _index = WorkspaceSyntaxIndex.Create(_workspace);
        _diagnostic = _index.RepairableDiagnostics.First(diagnostic => diagnostic.Code == DiagnosticCodes.CaptureEventsRecordFixture);
    }

    protected WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
}
