// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object ReadWorkspace(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        var view = McpJson.OptionalString(arguments, "view") ?? "documents";
        if (view == "executable-model")
        {
            var model = workspace.Compilation.Success ? workspace.Compilation.Value!.Model : null;
            var manifestRevision = McpAttachmentManifest.Revision(workspace.Compilation.ImplementationRequirements);
            CheckContinuation(arguments, "expectedAttachmentManifestRevision", manifestRevision);
            if (model is null)
            {
                if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 || McpJson.OptionalString(arguments, "expectedModelRevision") is not null)
                {
                    throw new McpFailure("StaleRevision: executable model is no longer available.");
                }

                return McpJson.ToolResult(new
                {
                    workspace = McpWorkspaceTransport.Describe(workspace),
                    view,
                    available = false,
                    executableDiagnosticsCount = workspace.Compilation.Diagnostics.Count(),
                    executableDiagnosticsView = "executable-diagnostics"
                });
            }

            CheckContinuation(arguments, "expectedModelRevision", model.Revision.ToString());
            var bytes = SemanticModelSerializer.Serialize(model);
            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace),
                view,
                available = true,
                executableDiagnosticsCount = workspace.Compilation.Diagnostics.Count(),
                executableDiagnosticsView = "executable-diagnostics",
                schema = SemanticModelCanonicalJson.Schema,
                schemaVersion = model.LanguageVersion.Major,
                languageVersion = model.LanguageVersion.ToString(),
                semanticVersion = model.SemanticVersion.ToString(),
                modelRevision = model.Revision.ToString(),
                attachmentManifestRevision = manifestRevision,
                totalBytes = bytes.Length,
                page = McpPaging.Bytes(bytes, arguments, workspace.Revision.ToString())
            });
        }

        if (new[] { "operation-intents", "system-intents", "operation-intent-details", "system-intent-details", "ordered-productions" }.Contains(view, StringComparer.Ordinal))
        {
            var analysis = McpWorkspaceAnalysis.For(workspace);
            var inventory = analysis.OperationIntents;
            IEnumerable<object> values;
            if (view == "ordered-productions")
            {
                values = inventory.Productions();
            }
            else
            {
                var systems = view.StartsWith("system", StringComparison.Ordinal);
                var entries = inventory.Entries.Where(entry => systems ? entry.Node is SystemSyntax : entry.Node is OperationSyntax).ToArray();
                if (view.EndsWith("details", StringComparison.Ordinal))
                {
                    var key = McpJson.RequiredString(arguments, "authoringKey");
                    var matches = entries.Where(entry => inventory.Key(entry) == key).Take(2).ToArray();
                    if (matches.Length > 1) throw new McpFailure("AmbiguousDeclaration: authoring key has multiple source occurrences. Select read-ast handles after repairing the collision.");
                    if (matches.Length == 0)
                    {
                        throw new McpFailure(analysis.Syntax.UnresolvedPlacementDocuments.IsEmpty
                            ? "UnknownDeclaration: no uniquely indexed declaration has that kind and authoring key."
                            : "UnresolvedPlacement: repair conflicting or cyclic imports before requesting declaration details.");
                    }
                    if (inventory.AmbiguousOwner(matches[0])) throw new McpFailure("AmbiguousDeclaration: authoring declaration has a colliding physical owner. Select read-ast handles after repairing the collision.");
                    values = inventory.Details(matches[0]);
                }
                else
                {
                    values = entries.Select(inventory.Summary).Concat(analysis.Syntax.UnresolvedPlacementDocuments.Select(document => (object)new
                    {
                        kind = "unresolved-placement", documentId = document.Id.ToString(), path = document.Path.Value,
                        executionAvailable = false, action = "Repair conflicting or cyclic imports before selecting an authoring owner."
                    }));
                }
            }

            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace), view,
                executionAvailable = false, executionReadiness = "Unavailable until ESM v9 (PLAY0268).",
                authoringDiagnosticsCount = analysis.Syntax.Diagnostics.Length,
                unresolvedPlacementCount = analysis.Syntax.UnresolvedPlacementDocuments.Length,
                page = McpPaging.Page(values, arguments, workspace.Revision.ToString())
            });
        }

        if (view == "handler-intents" || view == "handler-intent-details")
        {
            CheckContinuation(arguments, "expectedCatalogRevision", workspace.IdentityCatalog.Revision.ToString());
            var inventory = McpWorkspaceAnalysis.For(workspace).HandlerIntents;
            if (view == "handler-intent-details")
            {
                var id = McpJson.RequiredString(arguments, "requirementId");
                var matches = inventory.Entries.Where(value => value.RequirementId == id).Take(2).ToArray();
                if (matches.Length > 1)
                {
                    throw new McpFailure("AmbiguousRequirement: multiple handler occurrences share that identity. Read handler-intents for their occurrence handles and repair duplicate declarations before requesting details.");
                }

                var entry = matches.SingleOrDefault()
                    ?? throw new McpFailure(inventory.UnresolvedPlacementDocuments.IsEmpty
                        ? "UnknownRequirement: no handler intent has that identity."
                        : "UnresolvedPlacement: no uniquely placed handler has that identity. Read handler-intents for unresolved documents and repair conflicting or cyclic imports before requesting details.");
                return McpJson.ToolResult(new
                {
                    workspace = McpWorkspaceTransport.Describe(workspace), view,
                    coverage = WorkspaceImplementationInventory.Coverage,
                    handler = DescribeHandlerIntent(entry),
                    page = McpPaging.Page(entry.Hints, arguments, workspace.Revision.ToString())
                });
            }

            var unresolved = inventory.UnresolvedPlacementDocuments.Select(document => (object)new
            {
                placementStatus = "unresolved",
                conflictKind = "UnresolvedPlacement",
                documentId = document.Id.ToString(),
                path = document.Path.Value,
                action = "Repair conflicting or cyclic imports before selecting a handler owner or requirement identity."
            });
            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace), view,
                coverage = WorkspaceImplementationInventory.Coverage,
                unresolvedPlacementCount = inventory.UnresolvedPlacementDocuments.Length,
                page = McpPaging.Page(inventory.Entries.Select(DescribeHandlerIntent).Concat(unresolved), arguments, workspace.Revision.ToString())
            });
        }

        if (view == "implementation-requirements")
        {
            var manifestRevision = McpAttachmentManifest.Revision(workspace.Compilation.ImplementationRequirements);
            CheckContinuation(arguments, "expectedAttachmentManifestRevision", manifestRevision, requireOnContinuation: false);
            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace),
                view,
                attachmentManifestRevision = manifestRevision,
                page = McpPaging.Page(workspace.Compilation.ImplementationRequirements, requirement => DescribeRequirement(requirement, workspace.Compilation.TypedContextDescriptors), arguments, workspace.Revision.ToString())
            });
        }

        if (view == "typed-contexts")
        {
            CheckContinuation(arguments, "expectedDescriptorContractRevision", SemanticTypedContextDescriptor.ContractRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace),
                view,
                descriptorContractRevision = SemanticTypedContextDescriptor.ContractRevision,
                available = workspace.Compilation.TypedContextDescriptors.Any(value => value.IsWrapperReady),
                page = McpPaging.Page(workspace.Compilation.TypedContextDescriptors, DescribeDescriptor, arguments, workspace.Revision.ToString())
            });
        }

        if (view == "source-map")
        {
            var available = workspace.Compilation.Success;
            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace),
                view,
                available,
                executableDiagnosticsCount = workspace.Compilation.Diagnostics.Count(),
                executableDiagnosticsView = "executable-diagnostics",
                page = McpPaging.Page(
                    available ? workspace.Compilation.Value!.SourceMap.Entries : [],
                    entry => new
                    {
                        semanticId = entry.SemanticId.ToString(),
                        role = entry.Role.ToString(),
                        origin = entry.Origin.ToString(),
                        documentId = entry.Span.Document.ToString(),
                        path = workspace.Documents.Single(document => document.Id == entry.Span.Document).Path.Value,
                        span = new { entry.Span.Start, entry.Span.Length, entry.Span.StartLine, entry.Span.StartColumn, entry.Span.EndLine, entry.Span.EndColumn }
                    },
                    arguments,
                    workspace.Revision.ToString())
            });
        }

        var syntax = view == "repairs" ? McpWorkspaceAnalysis.For(workspace).Syntax : null;
        var items = view switch
        {
            "documents" => workspace.Documents.Select(document => (object)new
            {
                documentId = document.Id.ToString(),
                path = document.Path.Value,
                document.StableKey,
                document.Encoding,
                byteCount = document.Bytes.Length,
                root = McpAstHandles.Describe(new(workspace.Revision, document.Id, string.Empty))
            }),
            "semantics" => workspace.IdentityCatalog.Semantics.Select(assignment => (object)new
            {
                semanticId = assignment.Id.ToString(),
                address = McpSemanticAddresses.Describe(assignment.Address),
                assignment.Origin
            }),
            "eventContracts" => workspace.IdentityCatalog.EventContracts.Select(assignment => (object)new
            {
                eventContractId = assignment.Id.ToString(),
                address = McpSemanticAddresses.Describe(assignment.Address),
                revision = assignment.Revision.ToString(),
                assignment.Origin
            }),
            "diagnostics" => McpWorkspaceAnalysis.For(workspace).Source.Compilation.Diagnostics,
            "repairs" => syntax!.RepairableDiagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(syntax, workspace.Revision, diagnostic)
                    .Select(repair => (Repair: repair, diagnostic.Location)))
                .Concat(syntax.Entries.Where(entry => entry.Node is ApplicationSyntax).SelectMany(entry => WorkspaceDiagnosticRepairs.FindDocumentOptionality(syntax, entry.Handle)
                    .Select(repair => (Repair: repair, entry.Location))))
                .Select(item => (object)new
                {
                    item.Repair.DiagnosticCode,
                    item.Repair.RequiredFormatting,
                    item.Repair.Title,
                    item.Repair.CanFixAll,
                    retiredSemanticAddresses = item.Repair.RetiredSemanticAddresses.Select(McpSemanticAddresses.Describe),
                    item.Location,
                    scope = item.Repair.Subject.Path.Length == 0 ? "document" : "occurrence",
                    subject = McpAstHandles.Describe(item.Repair.Subject),
                    operations = item.Repair.Operations.Select(McpAstOperations.Describe)
                }),
            "executable-diagnostics" => workspace.Compilation.Diagnostics,
            _ => throw new McpFailure("Unknown workspace view.", -32602)
        };
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            view,
            page = McpPaging.Page(items, arguments, workspace.Revision.ToString())
        });
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
        var candidates = index.Entries.AsEnumerable();
        if (documentId is not null)
        {
            var id = DocumentId.Parse(documentId);
            if (!workspace.Documents.Any(document => document.Id == id))
            {
                throw new McpFailure("UnknownDocument: the document is not in this workspace.");
            }

            candidates = candidates.Where(entry => entry.Handle.Document == id);
        }

        if (path is not null && documentId is null)
        {
            throw new McpFailure("A node path requires documentId.", -32602);
        }

        candidates = view switch
        {
            "nodes" => path is null ? candidates : candidates.Where(entry => entry.Handle.Path == path),
            "children" => path is null ? throw new McpFailure("Children view requires a parent path.", -32602) : candidates.Where(entry => entry.Parent?.Path == path),
            _ => throw new McpFailure("AST view must be nodes or children.", -32602)
        };
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

        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            index.Diagnostics,
            page = McpPaging.Page(candidates, entry => McpAstHandles.Describe(entry, includeContent, analysis.ChildCount(entry.Handle)), arguments, workspace.Revision.ToString())
        });
    }

    internal object ReadProposal(JsonElement arguments)
    {
        var proposal = Proposal(arguments);
        var view = McpJson.OptionalString(arguments, "view") ?? "changes";
        var result = view switch
        {
            "changes" => McpPaging.Page(
                proposal.WritePlan.Entries.Select(entry => new
                {
                    entry.Kind,
                    documentId = entry.Document.ToString(),
                    beforePath = entry.Before?.Path.Value,
                    afterPath = entry.After?.Path.Value,
                    beforeBytes = entry.Before?.Bytes.Length,
                    afterBytes = entry.After?.Bytes.Length
                }),
                arguments,
                proposal.Workspace.Revision.ToString()),
            "diagnostics" => McpPaging.Page(proposal is McpAuthoringProposal authoring ? authoring.Result.AuthoringDiagnostics : [], arguments, proposal.Workspace.Revision.ToString()),
            "executable-diagnostics" => McpPaging.Page(proposal.Workspace.Compilation.Diagnostics, arguments, proposal.Workspace.Revision.ToString()),
            "implementation-requirements" => McpPaging.Page(proposal.Workspace.Compilation.ImplementationRequirements, requirement => DescribeRequirement(requirement, proposal.Workspace.Compilation.TypedContextDescriptors), arguments, proposal.Workspace.Revision.ToString()),
            "typed-contexts" => ProposalContexts(proposal, arguments),
            "dropped-comments" => McpPaging.Page(
                WorkspaceDroppedComments.In(proposal.WritePlan).Select(comment => new
                {
                    documentId = comment.Document.ToString(),
                    path = comment.Path.Value,
                    comment.Line,
                    comment.Column,
                    comment.Text
                }),
                arguments,
                proposal.Workspace.Revision.ToString()),
            "before" or "after" => ProposalBytes(proposal, arguments, view),
            _ => throw new McpFailure("Unknown proposal view.", -32602)
        };
        return McpJson.ToolResult(new { proposal.Validation, before = McpWorkspaceTransport.Describe(proposal.Before), after = McpWorkspaceTransport.Describe(proposal.Workspace), view, result });
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
        _ = Proposal(arguments);
        _proposals.Remove(McpJson.RequiredString(arguments, "proposalId"));
        return McpJson.ToolResult(new { discarded = true, remainingCount = _proposals.Count });
    }

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
        if (requireOnContinuation && McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && supplied is null)
        {
            throw new McpFailure($"'{name}' is required for continuation.", -32602);
        }

        if (supplied is not null && supplied != revision)
        {
            throw new McpFailure($"StaleRevision: {name} changed between pages.");
        }
    }

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
            while (index + count < lines.Length && lines[index + count].Column == first.Column && lines[index + count].Line == first.Line + count)
            {
                count++;
            }

            yield return new { line = first.Line, column = first.Column, count };
            index += count;
        }
    }

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
}
