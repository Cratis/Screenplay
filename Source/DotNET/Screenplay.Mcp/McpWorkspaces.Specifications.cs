// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object RunSpecifications(JsonElement arguments)
    {
        RefusePendingWorkspace();
        var persisted = new McpManagedFiles(Root).Read(McpState.FileName);
        var workspace = _workspace is not null ? Current() : persisted switch
        {
            null => OpenFromDisk(Root.ApplicationName),
            _ => McpState.Deserialize(persisted).Open(Root)
        };
        Root.Verify(workspace);
        var sourceRevision = McpSourceRevision.For(workspace.Documents);
        var expected = McpJson.OptionalString(arguments, "expectedSourceRevision");
        if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && expected is null)
        {
            throw new McpFailure("Subsequent source pages require expectedSourceRevision from the first page.", -32602);
        }

        if (expected is not null && expected != sourceRevision)
        {
            throw new McpFailure("StaleSourceRevision: source changed between specification pages; start a new run.") { FailureKind = "StaleSourceRevision" };
        }

        var report = McpSpecificationExecution.Run(workspace, McpJson.OptionalString(arguments, "specification"), McpJson.OptionalString(arguments, "scope"));
        var response = new
        {
            report.SourceRevision,
            report.Outcome,
            report.Discovered,
            report.Selected,
            report.Executed,
            report.Passed,
            report.Failed,
            report.Unsupported,
            executableDiagnostics = report.Diagnostics,
            report.PlanIssues,
            page = report.Outcome == "unbound" ? null : McpPaging.Page(report.Results, arguments, report.SourceRevision)
        };
        Root.Verify(workspace);
        new McpManagedFiles(Root).Verify(McpState.FileName, persisted);
        RefusePendingWorkspace();

        return McpJson.ToolResult(response, report.Outcome != "passed");
    }
}
