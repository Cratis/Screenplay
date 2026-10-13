// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    readonly ConditionalWeakTable<IMcpProposal, BaselinePlan> _baselinePlans = [];
    BaselinePlan? _planningBaseline;

    internal object WithReleaseBaseline(string tool, JsonElement arguments, Func<object> call)
    {
        if (!PlansProposal(tool))
        {
            return call();
        }

        var workspace = Current();
        _planningBaseline = ReadBaselinePlan(workspace.IdentityCatalog.Application);
        CheckBaselinePin(arguments, _planningBaseline.Read);
        try
        {
            return call();
        }
        finally
        {
            _planningBaseline = null;
        }
    }

    internal object ListReleaseBaseline(JsonElement arguments)
    {
        var workspace = Current();
        var baseline = ReadBaselinePlan(workspace.IdentityCatalog.Application).Read;
        CheckBaselinePin(arguments, baseline);
        if (baseline.Revision is not null) CheckContinuation(arguments, "expectedReleaseBaselineRevision", baseline.Revision);
        var check = baseline.Status == "valid" ? ReleaseBaseline.Check(workspace, baseline) : null;
        var items = baseline.Document?.Entries.Select(entry => DescribeBaselineEntry(entry, check));
        return McpJson.ToolResult(new { releaseBaseline = DescribeBaseline(baseline, check), page = items is null ? null : McpPaging.Page(items, arguments, baseline.Revision!) });
    }

    static bool PlansProposal(string tool) => tool.StartsWith("propose", StringComparison.Ordinal) || tool == "expand-layout";

    static object? BaselineRefusal(IMcpProposal proposal, ScreenplayWorkspace candidate, BaselinePlan plan)
    {
        var check = WorkspaceReleaseBaseline.Check(proposal.Before, candidate, plan.Read);
        return check.Success ? null : McpJson.ToolResult(new { success = false, failureKind = check.Kind, check.Reason, violations = check.Violations }, true);
    }

    static WorkspaceEventRenameReport? EventRenameReport(IMcpProposal proposal) => proposal is McpAuthoringProposal authored ? authored.Result.EventRenameReport : null;

    static object DescribeBaselineEntry(ReleaseBaselineEntry entry, ReleaseBaselineResult? check)
    {
        var violation = check?.Violations.SingleOrDefault(value => value.Event == entry.Event && value.Revision == entry.Revision);
        return new { entry.Event, entry.Revision, entry.EventType, entry.LastAddress, entry.AdmittedIn, entry.AdmittedAt, status = EntryStatus(violation, check), path = violation?.Path };
    }

    static string EntryStatus(ReleaseBaselineViolation? violation, ReleaseBaselineResult? check) => violation?.Kind switch
    {
        "BaselinedRevisionChanged" => "changed",
        "BaselinedRevisionMissing" => "missing",
        _ => check?.Failure is not null ? "unknown" : "unchanged"
    };

    static void CheckBaselinePin(JsonElement arguments, ReleaseBaselineRead baseline)
    {
        var expected = McpJson.OptionalString(arguments, "expectedReleaseBaselineRevision");
        if (expected is not null && expected != baseline.Revision) throw new McpFailure("StaleReleaseBaseline: read the release baseline again.") { FailureKind = "StaleReleaseBaseline" };
    }

    static bool SameBaseline(BaselinePlan before, BaselinePlan after) => before.Read.Status == after.Read.Status && before.Read.Revision == after.Read.Revision &&
        (before.Read.Status != "unreadable" || (before.Read.Reason == after.Read.Reason && McpManagedFiles.Equal(before.Bytes, after.Bytes)));

    static object DescribeBaseline(ReleaseBaselineRead baseline, ReleaseBaselineResult? check) => new
    {
        baseline.Status,
        baseline.Revision,
        baseline.Reason,
        entries = baseline.Document?.Entries.Length,
        violations = baseline.Status == "unreadable" || check?.Failure is not null ? (int?)null : check?.Violations.Length ?? 0,
        checkFailure = check?.Failure
    };

    BaselinePlan PlanBaseline(IMcpProposal proposal, JsonElement arguments)
    {
        var planned = _planningBaseline ?? ReadBaselinePlan(proposal.Before.IdentityCatalog.Application);
        var current = ReadBaselinePlan(proposal.Before.IdentityCatalog.Application);
        CheckBaselinePin(arguments, current.Read);
        if (!SameBaseline(planned, current)) throw new McpFailure("StaleReleaseBaseline: the baseline changed during planning.") { FailureKind = "StaleReleaseBaseline" };
        return planned;
    }

    BaselinePlan ReadBaselinePlan(ApplicationIdentity application)
    {
        try
        {
            var bytes = new McpManagedFiles(Root).Read(ReleaseBaselineStore.FileName, ReleaseBaselineStore.MaximumBytes);
            return new(bytes, ReleaseBaselineStore.ReadBytes(bytes, application));
        }
        catch (Exception exception) when (exception is McpFailure or IOException or UnauthorizedAccessException)
        {
            return new(null, new("unreadable", null, null, exception.Message));
        }
    }

    void VerifyBaselinePlan(IMcpProposal proposal, JsonElement arguments)
    {
        if (!_baselinePlans.TryGetValue(proposal, out var plan)) throw new McpFailure("No retained release baseline evidence.");
        var current = ReadBaselinePlan(proposal.Before.IdentityCatalog.Application);
        CheckBaselinePin(arguments, current.Read);
        if (!SameBaseline(plan, current)) throw new McpFailure("StaleReleaseBaseline: the baseline changed since this proposal; propose again.") { FailureKind = "StaleReleaseBaseline" };
        var check = WorkspaceReleaseBaseline.Check(proposal.Before, proposal.Workspace, current.Read);
        if (!check.Success) throw new McpFailure($"{check.Kind}: release baseline check failed.") { FailureKind = check.Kind! };
    }

    BaselinePlan ProposalBaseline(IMcpProposal proposal) => _baselinePlans.TryGetValue(proposal, out var plan) ? plan : throw new McpFailure("No retained release baseline evidence.");

    object ReleaseBaselineView(IMcpProposal proposal, JsonElement arguments)
    {
        var baseline = ProposalBaseline(proposal).Read;
        CheckBaselinePin(arguments, baseline);
        if (baseline.Revision is not null) CheckContinuation(arguments, "expectedReleaseBaselineRevision", baseline.Revision);
        var check = WorkspaceReleaseBaseline.Check(proposal.Before, proposal.Workspace, baseline);
        var events = WorkspaceSyntaxIndex.Create(proposal.Before).Entries.Where(entry => entry.Node is EventSyntax && entry.EventContractId is not null)
            .DistinctBy(entry => (entry.EventContractId, ((EventSyntax)entry.Node).Generation))
            .OrderBy(entry => entry.EventContractId!.Value.ToString(), StringComparer.Ordinal).ThenBy(entry => ((EventSyntax)entry.Node).Generation)
            .Select(entry => new { @event = entry.EventContractId!.Value.ToString(), revision = ((EventSyntax)entry.Node).Generation, mutability = baseline.Status == "unreadable" ? null : WorkspaceEventMutability.For(baseline, entry.EventContractId, ((EventSyntax)entry.Node).Generation) });
        return new { releaseBaseline = DescribeBaseline(baseline, check), violations = check.Violations, page = McpPaging.Page(events, arguments, baseline.Revision ?? proposal.Workspace.Revision.ToString()) };
    }

    Dictionary<string, object?> WithReleaseBaselineStatus(object value, ScreenplayWorkspace workspace)
    {
        var baseline = ReadBaselinePlan(workspace.IdentityCatalog.Application).Read;
        var check = baseline.Status == "valid" ? ReleaseBaseline.Check(workspace, baseline) : null;
        var properties = JsonSerializer.SerializeToElement(value, McpJson.Options).EnumerateObject().ToDictionary(property => property.Name, property => (object?)property.Value, StringComparer.Ordinal);
        properties["releaseBaseline"] = DescribeBaseline(baseline, check);
        return properties;
    }

    Dictionary<string, object?> WithReleaseBaselineStatus(object value, byte[]? persisted)
    {
        var workspace = _workspace;
        if (workspace is null)
        {
            try
            {
                workspace = persisted is null ? OpenFromDisk(Root, Root.ApplicationName) : McpState.Deserialize(persisted).Open(Root);
            }
            catch (Exception exception) when (exception is McpFailure or IOException or UnauthorizedAccessException)
            {
                var baseline = ReadBaselinePlan(NotesApplication(persisted)).Read;
                var properties = JsonSerializer.SerializeToElement(value, McpJson.Options).EnumerateObject().ToDictionary(property => property.Name, property => (object?)property.Value, StringComparer.Ordinal);
                properties["releaseBaseline"] = DescribeBaseline(baseline, new(null, [], "ModelDoesNotBind", exception.Message));
                return properties;
            }
        }
        return WithReleaseBaselineStatus(value, workspace);
    }

    sealed record BaselinePlan(byte[]? Bytes, ReleaseBaselineRead Read);
}
