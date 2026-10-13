// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    static readonly string[] _eventSourceViews = ["event-sources", "event-streams", "event-source-details", "event-stream-details", "command-routes", "event-source-diagnostics"];
    static readonly string[] _operationIntentViews = ["operation-intents", "system-intents", "operation-intent-details", "system-intent-details", "ordered-productions"];

    internal object ReadWorkspace(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        McpRepairEvidence.Check(McpRepairEvidence.Expected(arguments), workspace);
        var view = McpJson.OptionalString(arguments, "view") ?? "documents";
        if (McpJson.OptionalString(arguments, "scope") is { } scope)
        {
            return ReadScopedDiagnostics(workspace, arguments, view, scope);
        }

        return ReadWorkspaceView(workspace, arguments, view);
    }

    internal object ReadAst(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        var analysis = McpWorkspaceAnalysis.For(workspace);
        var index = analysis.Syntax;
        var documentId = McpJson.OptionalString(arguments, "documentId");
        var path = McpJson.OptionalString(arguments, "path");
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var view = McpJson.OptionalString(arguments, "view") ?? "nodes";
        var candidates = FilterAstByDocument(workspace, index.Entries.AsEnumerable(), documentId);
        if (path is not null && documentId is null)
        {
            throw new McpFailure("A node path requires documentId.", -32602);
        }

        candidates = FilterAstByView(candidates, view, path);
        candidates = FilterAstByAttributes(candidates, arguments);

        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            index.Diagnostics,
            page = McpPaging.Page(candidates, entry => McpAstHandles.Describe(entry, includeContent, analysis.ChildCount(entry.Handle)), arguments, workspace.Revision.ToString())
        });
    }

    internal object ReadProposal(JsonElement arguments)
    {
        var view = McpJson.OptionalString(arguments, "view") ?? "changes";
        var semanticDiff = view == "semantic-diff";
        if (semanticDiff)
        {
            // Report a pending recovery here or in an overlapping root before reading the proposal from disk.
            RefusePendingWorkspace();
        }

        var proposal = Proposal(arguments);
        if (semanticDiff)
        {
            VerifyProposalBaseline(proposal);
        }
        var result = view switch
        {
            "notes" => NotesView(proposal, arguments),
            "release-baseline" => ReleaseBaselineView(proposal, arguments),
            _ => ReadProposalView(proposal, arguments, view)
        };
        var notesImpact = NotesImpact(proposal);
        return McpJson.ToolResult(new { proposal.Validation, releaseBaselineRevision = ProposalBaseline(proposal).Read.Revision, eventPinWarnings = EventPinWarnings(proposal.Workspace), eventRename = proposal is McpAuthoringProposal renamed ? renamed.Result.EventRenameReport : null, orphanedNotes = notesImpact.OrphanedNotes, notes = notesImpact.Notes, moveReport = proposal is McpAuthoringProposal moved ? DescribeMove(moved.Result.MoveReport) : null, property = proposal is McpAuthoringProposal property ? DescribeProperty(property.Result.PropertyReport) : null, repairEvidence = _repairEvidence.TryGetValue(proposal, out var evidence) ? evidence : null, before = McpWorkspaceTransport.Describe(proposal.Before), after = McpWorkspaceTransport.Describe(proposal.Workspace), view, result });
    }

    internal object ExportWorkspace(JsonElement arguments)
    {
        var workspace = McpJson.OptionalString(arguments, "proposalId") is null ? CheckedCurrent(arguments) : Proposal(arguments).Workspace;
        if (McpJson.RequiredString(arguments, "expectedRevision") != workspace.Revision.ToString())
        {
            throw new McpFailure("StaleRevision: export chunks must refer to one immutable workspace.");
        }

        return McpJson.ToolResult(McpPaging.Bytes(McpWorkspaceTransport.ExportBytes(workspace), arguments, workspace.Revision.ToString()));
    }

    internal object DiscardProposal(JsonElement arguments)
    {
        var id = McpJson.RequiredString(arguments, "proposalId");
        if (!_proposals.Remove(id)) throw new McpFailure("UnknownProposal: only an outstanding proposal from this connection can be used.") { FailureKind = "UnknownProposal" };
        return McpJson.ToolResult(new { discarded = true, remainingCount = _proposals.Count });
    }

    static IEnumerable<WorkspaceSyntaxEntry> FilterAstByDocument(ScreenplayWorkspace workspace, IEnumerable<WorkspaceSyntaxEntry> candidates, string? documentId)
    {
        if (documentId is null)
        {
            return candidates;
        }

        var id = DocumentId.Parse(documentId);
        if (!workspace.Documents.Any(document => document.Id == id))
        {
            throw new McpFailure("UnknownDocument: the document is not in this workspace.");
        }

        return candidates.Where(entry => entry.Handle.Document == id);
    }

    static IEnumerable<WorkspaceSyntaxEntry> FilterAstByView(IEnumerable<WorkspaceSyntaxEntry> candidates, string view, string? path) => view switch
    {
        "nodes" => path is null ? candidates : candidates.Where(entry => entry.Handle.Path == path),
        "children" => path is null ? throw new McpFailure("Children view requires a parent path.", -32602) : candidates.Where(entry => entry.Parent?.Path == path),
        _ => throw new McpFailure("AST view must be nodes or children.", -32602)
    };

    static IEnumerable<WorkspaceSyntaxEntry> FilterAstByAttributes(IEnumerable<WorkspaceSyntaxEntry> candidates, JsonElement arguments)
    {
        if (McpJson.OptionalString(arguments, "kind") is { } kind)
        {
            candidates = candidates.Where(entry => entry.Kind == kind);
        }

        if (McpJson.OptionalString(arguments, "name") is { } name)
        {
            candidates = candidates.Where(entry => (entry.Node.GetType().GetProperty("Name")?.GetValue(entry.Node) as string) == name);
        }

        if (McpJson.OptionalString(arguments, "semanticId") is { } semanticId)
        {
            var id = SemanticId.Parse(semanticId);
            candidates = candidates.Where(entry => entry.SemanticId == id);
        }

        return candidates;
    }

    static object ReadProposalView(IMcpProposal proposal, JsonElement arguments, string view) => view switch
    {
        "semantic-diff" => McpSemanticDiff.Read(proposal, arguments),
        "changes" => McpPaging.Page(proposal.WritePlan.Entries, DescribeChangeSummary, arguments, proposal.Workspace.Revision.ToString()),
        "diagnostics" => McpPaging.Page(proposal is McpAuthoringProposal authoring ? authoring.Result.AuthoringDiagnostics : [], arguments, proposal.Workspace.Revision.ToString()),
        "executable-diagnostics" => McpPaging.Page(proposal.Workspace.Compilation.Diagnostics, arguments, proposal.Workspace.Revision.ToString()),
        "implementation-requirements" => McpPaging.Page(proposal.Workspace.Compilation.ImplementationRequirements, requirement => DescribeRequirement(requirement, proposal.Workspace.Compilation.TypedContextDescriptors), arguments, proposal.Workspace.Revision.ToString()),
        "typed-contexts" => ProposalContexts(proposal, arguments),
        "dropped-comments" => McpPaging.Page(WorkspaceDroppedComments.In(proposal.WritePlan), DescribeDroppedComment, arguments, proposal.Workspace.Revision.ToString()),
        "before" or "after" => ProposalBytes(proposal, arguments, view),
        _ => throw new McpFailure("Unknown proposal view.", -32602)
    };

    static object DescribeChangeSummary(WorkspaceWriteEntry entry) => new
    {
        entry.Kind,
        documentId = entry.Document.ToString(),
        beforePath = entry.Before?.Path.Value,
        afterPath = entry.After?.Path.Value,
        beforeBytes = entry.Before?.Bytes.Length,
        afterBytes = entry.After?.Bytes.Length
    };

    static object DescribeDroppedComment(WorkspaceDroppedComment comment) => new
    {
        documentId = comment.Document.ToString(),
        path = comment.Path.Value,
        comment.Line,
        comment.Column,
        comment.Text
    };

    static object DescribeNamedRuleIntent(WorkspaceNamedRuleIntentEntry entry) => new
    {
        handle = McpAstHandles.Describe(entry.Handle),
        owner = McpSemanticAddresses.Describe(entry.Owner), ownerName = entry.Owner.Name,
        ownerId = entry.OwnerId.ToString(), identityOrigin = entry.IdentityOrigin.ToString(),
        entry.IsProvisional, entry.IsAmbiguous, member = entry.Member,
        requirementId = entry.RequirementId, state = entry.State, file = entry.File, language = entry.Language,
        hintCount = entry.Hints.Length, executionEvidence = false,
        executionReadiness = entry.State == "pending" ? "Unsupported: no predicate attachment (PLAY0268)." : "Opaque predicate; execution requires an admitting target, not the reference runner."
    };

    static object DescribeHandlerIntent(WorkspaceImplementationEntry entry) => new
    {
        placementStatus = "resolved",
        handle = McpAstHandles.Describe(entry.Handle),
        owner = McpSemanticAddresses.Describe(entry.Owner),
        ownerId = entry.OwnerId.ToString(),
        requirementId = entry.RequirementId,
        identityOrigin = entry.IdentityOrigin.ToString(),
        provisional = entry.IsProvisional,
        ambiguous = entry.IsAmbiguous,
        hintCount = entry.Hints.Length,
        file = entry.File,
        language = entry.Language,
        state = entry.State,
        executableReady = false
    };

    static void CheckContinuation(JsonElement arguments, string name, string revision, bool requireOnContinuation = true)
    {
        var supplied = McpJson.OptionalString(arguments, name);
        if (IsMissingContinuation(arguments, supplied, requireOnContinuation))
        {
            throw new McpFailure($"'{name}' is required for continuation.", -32602);
        }

        if (supplied is not null && supplied != revision)
        {
            throw new McpFailure($"StaleRevision: {name} changed between pages.");
        }
    }

    static bool IsMissingContinuation(JsonElement arguments, string? supplied, bool requireOnContinuation) =>
        requireOnContinuation && McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && supplied is null;

    static object ProposalContexts(IMcpProposal proposal, JsonElement arguments)
    {
        CheckContinuation(arguments, "expectedDescriptorContractRevision", SemanticTypedContextDescriptor.ContractRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new
        {
            descriptorContractRevision = SemanticTypedContextDescriptor.ContractRevision,
            available = proposal.Workspace.Compilation.TypedContextDescriptors.Any(value => value.IsWrapperReady),
            page = McpPaging.Page(proposal.Workspace.Compilation.TypedContextDescriptors, DescribeDescriptor, arguments, proposal.Workspace.Revision.ToString())
        };
    }

    static object DescribeDescriptor(SemanticTypedContextDescriptor descriptor) => new
    {
        descriptor.RequirementId,
        role = descriptor.Role.ToString(),
        descriptor.ContextVersion,
        modelRevision = descriptor.ModelRevision?.ToString(),
        descriptor.IsWrapperReady,
        types = descriptor.Types.Select(type => new
        {
            id = type.Id.ToString(),
            type.Name,
            kind = type.Kind.ToString(),
            primitive = type.Primitive.ToString(),
            properties = type.Properties.Select(property => new { property.Name, id = property.Id.ToString(), type = DescribeModelType(property.Type) })
        }),
        operationId = descriptor.OperationId?.ToString(),
        members = descriptor.Members.Select(member => new
        {
            member.Name,
            type = new
            {
                member.Type.Kind,
                modelType = DescribeModelType(member.Type.ModelType),
                shape = member.Type.Shape?.ToString(),
                member.Type.RuntimeToken,
                properties = member.Type.Properties.Select(property => new
                {
                    property.Name,
                    id = property.Id.ToString(),
                    type = DescribeModelType(property.Type)
                })
            },
            member.IsNullable,
            member.IsDerived,
            source = new { member.Source.Kind, semanticId = member.Source.SemanticId?.ToString(), member.Source.Path, member.Source.ConstantValue, eventRevision = member.Source.EventRevision?.Value }
        })
    };

    static object? DescribeModelType(SemanticTypeReference? type) => type is null ? null : new
    {
        kind = type.Kind.ToString(),
        primitive = type.Primitive.ToString(),
        target = type.Kind == SemanticTypeReferenceKind.Primitive ? null : type.Target.ToString(),
        type.IsCollection,
        type.IsOptional
    };

    static object DescribeRequirement(SemanticImplementationRequirement requirement, ImmutableArray<SemanticTypedContextDescriptor> descriptors) => new
    {
        role = requirement.Role.ToString(),
        requirement.RequirementId,
        typedContext = new
        {
            count = descriptors.Count(value => value.RequirementId == requirement.RequirementId),
            operationIds = descriptors.Where(value => value.RequirementId == requirement.RequirementId).Select(value => value.OperationId?.ToString())
        },
        requirement.ContextVersion,
        requirement.ResultVersion,
        requirement.RequiredCapability,
        attachmentResolution = requirement.AttachmentResolution.ToString(),
        owner = McpSemanticAddresses.Describe(requirement.Owner),
        requirement.Member,
        requirement.Language,
        requirement.File,
        requirement.ContentHash,
        bodySpan = requirement.BodySpan,
        bodyLines = DescribeBodyLines(requirement.BodyLines),
        semanticId = requirement.Source.SemanticId.ToString(),
        documentId = requirement.Source.Span.Document.ToString(),
        line = requirement.Source.Span.StartLine,
        column = requirement.Source.Span.StartColumn
    };

    static IEnumerable<object> DescribeBodyLines(ImmutableArray<CodeBlockSourceLine> lines)
    {
        for (var index = 0; index < lines.Length;)
        {
            var first = lines[index];
            var count = 1;
            while (ContinuesRun(lines, first, index + count, count))
            {
                count++;
            }

            yield return new { line = first.Line, column = first.Column, count };
            index += count;
        }
    }

    static bool ContinuesRun(ImmutableArray<CodeBlockSourceLine> lines, CodeBlockSourceLine first, int next, int count) =>
        next < lines.Length && lines[next].Column == first.Column && lines[next].Line == first.Line + count;

    static object ProposalBytes(IMcpProposal proposal, JsonElement arguments, string view)
    {
        var id = DocumentId.Parse(McpJson.RequiredString(arguments, "documentId"));
        var entry = proposal.WritePlan.Entries.SingleOrDefault(entry => entry.Document == id)
            ?? throw new McpFailure("The document is not changed by this proposal.");
        var document = view == "before" ? entry.Before : entry.After;
        if (document is null)
        {
            return new { exists = false };
        }

        return new
        {
            exists = true,
            path = document.Path.Value,
            content = McpPaging.Bytes([.. document.Bytes], arguments, view == "before" ? proposal.Before.Revision.ToString() : proposal.Workspace.Revision.ToString())
        };
    }

    object ReadWorkspaceView(ScreenplayWorkspace workspace, JsonElement arguments, string view) => view switch
    {
        "executable-model" => ReadExecutableModelView(workspace, arguments, view),
        _ when _eventSourceViews.Contains(view, StringComparer.Ordinal) => ReadEventSourceView(workspace, arguments, view),
        _ when _operationIntentViews.Contains(view, StringComparer.Ordinal) => ReadOperationIntentView(workspace, arguments, view),
        _ => ReadIntentOrListedView(workspace, arguments, view)
    };

    object ReadIntentOrListedView(ScreenplayWorkspace workspace, JsonElement arguments, string view) => view switch
    {
        "handler-intents" or "handler-intent-details" => ReadHandlerIntentView(workspace, arguments, view),
        "named-rule-intents" or "named-rule-intent-details" => ReadNamedRuleIntentView(workspace, arguments, view),
        "implementation-requirements" => ReadImplementationRequirementsView(workspace, arguments, view),
        "typed-contexts" => ReadTypedContextsView(workspace, arguments, view),
        "source-map" => ReadSourceMapView(workspace, arguments, view),
        _ => ReadListedView(workspace, arguments, view)
    };

    void VerifyProposalBaseline(IMcpProposal proposal)
    {
        new McpManagedFiles(Root).Verify(McpState.FileName, StatePlan(proposal).Before);
        Root.Verify(proposal.Before);
    }
}
