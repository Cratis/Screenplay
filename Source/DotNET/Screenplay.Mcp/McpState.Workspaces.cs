// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object State(JsonElement arguments)
    {
        var view = McpJson.OptionalString(arguments, "view") ?? "status";
        var files = new McpManagedFiles(root);
        var persisted = view == "status" || view == "persisted" ? files.Read(McpState.FileName) : null;
        var proposal = McpJson.OptionalString(arguments, "proposalId") is null ? null : Proposal(arguments);
        if (view != "status")
        {
            var bytes = view switch
            {
                "before" => StatePlan(proposal ?? throw new McpFailure("The before view requires proposalId.", -32602)).Before,
                "after" => StatePlan(proposal ?? throw new McpFailure("The after view requires proposalId.", -32602)).After,
                "persisted" => persisted,
                _ => throw new McpFailure("State view must be status, persisted, before, or after.", -32602)
            };
            var revision = McpStatePlan.Revision(bytes);
            if (McpJson.RequiredString(arguments, "expectedStateRevision") != revision)
            {
                throw new McpFailure("IdentityStateDrift: state pages must use the exact state revision reported by workspace-state or the proposal.");
            }

            return McpJson.ToolResult(new { exists = bytes is not null, stateRevision = revision, content = bytes is null ? null : McpPaging.Bytes(bytes, arguments, revision) });
        }

        return McpJson.ToolResult(new
        {
            exists = persisted is not null,
            path = ".screenplay/identities.json",
            stateRevision = McpStatePlan.Revision(persisted),
            byteCount = persisted?.Length ?? 0,
            identity = IdentityStatus(persisted),
            recovery = RecoveryStatus(),
            stateChange = proposal is null ? null : StatePlan(proposal).Describe()
        });
    }

    internal object Recover(JsonElement arguments)
    {
        var operationId = McpJson.RequiredString(arguments, "operationId");
        var journal = McpRecoveryJournal.Load(root) ?? throw new McpFailure("NoPendingOperation: there is no operation to roll back.");
        if (operationId != journal.Record.OperationId)
        {
            throw new McpFailure("PendingOperationChanged: inspect workspace-state before recovering this operation.");
        }

        try
        {
            new McpRecovery(root, journal).Rollback();
        }
        catch (Exception exception)
        {
            var failure = new
            {
                success = false,
                status = "RecoveryRequired",
                operationId,
                conflict = exception.Message,
                recovery = "Marker and remaining backups retained. Restore unexpected external files separately, inspect workspace-state, then explicitly retry rollback."
            };
            return McpJson.ToolResult(failure, true, enforceBudget: false);
        }

        _workspace = null;
        _stateBytes = null;
        _proposals.Clear();
        _statePlans.Clear();
        var result = new
        {
            success = true,
            status = "RolledBack",
            operationId,
            verifiedDocuments = journal.Before.Documents.Length,
            identityStateExists = journal.Record.BeforeState is not null,
            recovery = "Original source bytes and identity state verified. Reopen the workspace."
        };
        return McpJson.ToolResult(result, enforceBudget: false);
    }

    static object IdentityStatus(byte[]? bytes)
    {
        if (bytes is null)
        {
            return new { status = "Absent", conflict = (string?)null };
        }

        try
        {
            var state = McpState.Deserialize(bytes);
            return new
            {
                status = "Persisted",
                state.ApplicationName,
                applicationIdentity = state.Catalog.Application.ToString(),
                catalogRevision = state.Catalog.Revision.ToString(),
                documentCount = state.Mappings.Length
            };
        }
        catch (Exception exception)
        {
            return new { status = "Conflict", conflict = exception.Message };
        }
    }

    McpStatePlan StatePlan(IMcpProposal proposal) => _statePlans.TryGetValue(proposal, out var plan)
        ? plan : throw new McpFailure("UnknownProposal: no durable identity plan exists for this proposal.");

    object RecoveryStatus()
    {
        try
        {
            var journal = McpRecoveryJournal.Load(root);
            return journal is null ? new { pending = false, operationId = (string?)null, canRollback = false, conflict = (string?)null }
                : new McpRecovery(root, journal).Status();
        }
        catch (Exception exception)
        {
            return new { pending = true, operationId = (string?)null, canRollback = false, conflict = exception.Message };
        }
    }
}
