// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    static object ReadScopedDiagnostics(ScreenplayWorkspace workspace, JsonElement arguments, string view, string scope)
    {
        if (view != "diagnostics")
        {
            throw new McpFailure("scope is supported only for the diagnostics workspace view.", -32602);
        }

        var source = McpWorkspaceAnalysis.For(workspace).Source;
        if (!ScopedDiagnostics.TryValidate(source, scope, CompletenessChecks.None, out var selection, out var scopeError))
        {
            throw new McpFailure(scopeError.Message, -32602);
        }
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            view,
            scope,
            success = !selection.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            wholeApplicationSuccess = source.Compilation.Success,
            selection.DeclarationCount,
            selection.DependentDeclarationCount,
            selection.AffectedScopes,
            selection.UnresolvedEventConsumers,
            selection.PossiblyAffectedReferenceCount,
            selection.DependencyCoverage,
            summary = McpModelQueries.DiagnosticSummary(selection.Diagnostics),
            repairEvidenceRevision = McpRepairEvidence.Revision(workspace),
            page = McpPaging.Page(selection.Diagnostics, arguments, workspace.Revision.ToString())
        });
    }

    static object ReadExecutableModelView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
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

    static object ReadEventSourceView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
    {
        CheckContinuation(arguments, "expectedCatalogRevision", workspace.IdentityCatalog.Revision.ToString());
        var analysis = McpWorkspaceAnalysis.For(workspace);
        var inventory = analysis.EventSources;
        var values = EventSourceValues(analysis, arguments, view);
        if (view != "event-source-diagnostics")
        {
            values = values.Concat(inventory.View.UnresolvedPlacementDocuments.Select(UnresolvedPlacementEntry));
        }

        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace), view, syntaxOnly = analysis.Source.Index.Readiness.ModelSyntaxOnly,
            executionAvailable = workspace.Compilation.Success, executionReadiness = analysis.Source.Index.Readiness.ModelExecutionReadiness,
            inventoryComplete = inventory.View.IsComplete,
            authoringDiagnosticsCount = inventory.View.Diagnostics.Length,
            authoringDiagnosticsView = "event-source-diagnostics",
            unresolvedPlacementCount = inventory.View.UnresolvedPlacementDocuments.Length,
            detailShape = view.EndsWith("details", StringComparison.Ordinal) ? "compact-header-v1" : null,
            page = McpPaging.BoundedSourcePage(values, arguments, workspace.Revision.ToString())
        });
    }

    static object UnresolvedPlacementEntry(WorkspaceDocument document) => new
    {
        kind = "unresolved-placement", documentId = document.Id.ToString(), path = document.Path.Value,
        executionAvailable = false, action = "Repair conflicting or cyclic imports before selecting an authoring owner."
    };

    static IEnumerable<object> EventSourceValues(McpWorkspaceAnalysis analysis, JsonElement arguments, string view)
    {
        var inventory = analysis.EventSources;
        if (view == "event-source-diagnostics")
        {
            return inventory.View.Diagnostics.Cast<object>();
        }

        if (view == "command-routes")
        {
            return inventory.Routes();
        }

        var streams = view.StartsWith("event-stream", StringComparison.Ordinal);
        var entries = inventory.Entries.Where(entry => streams ? entry.Node is EventStreamSyntax : entry.Node is EventSourceSyntax).ToArray();
        return view.EndsWith("details", StringComparison.Ordinal)
            ? EventSourceDetails(analysis, arguments, entries)
            : entries.Select(inventory.Summary);
    }

    static IEnumerable<object> EventSourceDetails(McpWorkspaceAnalysis analysis, JsonElement arguments, WorkspaceSyntaxEntry[] entries)
    {
        var inventory = analysis.EventSources;
        var key = McpJson.RequiredString(arguments, "authoringKey");
        var matches = entries.Where(entry => inventory.Key(entry) == key).Take(2).ToArray();
        if (matches.Length > 1 || (matches.Length == 1 && inventory.AmbiguousOwner(matches[0])))
        {
            throw new McpFailure("AmbiguousDeclaration: source or stream has multiple physical owners; select read-ast handles after repairing the collision.") { FailureKind = "AmbiguousDeclaration" };
        }
        if (matches.Length == 0)
        {
            throw new McpFailure(analysis.Syntax.UnresolvedPlacementDocuments.IsEmpty
                ? "UnknownDeclaration: no declaration has that exact kind and authoring key."
                : "UnresolvedPlacement: repair conflicting or cyclic imports before selecting an owner.")
            {
                FailureKind = analysis.Syntax.UnresolvedPlacementDocuments.IsEmpty ? "UnknownDeclaration" : "UnresolvedPlacement"
            };
        }
        if (!inventory.View.IsComplete)
        {
            throw new McpFailure("IncompleteSource: source extent or placement is unresolved; repair source diagnostics before selecting a confident authoring owner.") { FailureKind = "IncompleteSource" };
        }
        return inventory.Details(matches[0]);
    }

    static object ReadOperationIntentView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
    {
        var analysis = McpWorkspaceAnalysis.For(workspace);
        var values = OperationIntentValues(analysis, arguments, view);
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace), view,
            executionAvailable = false, executionReadiness = "Not admitted by any supported executable model (ESM) version yet (PLAY0268) (#301).",
            authoringDiagnosticsCount = analysis.Syntax.Diagnostics.Length,
            unresolvedPlacementCount = analysis.Syntax.UnresolvedPlacementDocuments.Length,
            page = McpPaging.Page(values, arguments, workspace.Revision.ToString())
        });
    }

    static IEnumerable<object> OperationIntentValues(McpWorkspaceAnalysis analysis, JsonElement arguments, string view)
    {
        var inventory = analysis.OperationIntents;
        if (view == "ordered-productions")
        {
            return inventory.Productions();
        }

        var systems = view.StartsWith("system", StringComparison.Ordinal);
        var entries = inventory.Entries.Where(entry => systems ? entry.Node is SystemSyntax : entry.Node is OperationSyntax).ToArray();
        return view.EndsWith("details", StringComparison.Ordinal)
            ? OperationIntentDetails(analysis, arguments, entries)
            : entries.Select(inventory.Summary).Concat(analysis.Syntax.UnresolvedPlacementDocuments.Select(UnresolvedPlacementEntry));
    }

    static IEnumerable<object> OperationIntentDetails(McpWorkspaceAnalysis analysis, JsonElement arguments, WorkspaceSyntaxEntry[] entries)
    {
        var inventory = analysis.OperationIntents;
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
        return inventory.Details(matches[0]);
    }

    static object ReadHandlerIntentView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
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
                handler = DescribeHandlerIntent(entry, workspace.Compilation),
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
            page = McpPaging.Page(inventory.Entries.Select(entry => DescribeHandlerIntent(entry, workspace.Compilation)).Concat(unresolved), arguments, workspace.Revision.ToString())
        });
    }

    static object ReadNamedRuleIntentView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
    {
        CheckContinuation(arguments, "expectedCatalogRevision", workspace.IdentityCatalog.Revision.ToString());
        var inventory = McpWorkspaceAnalysis.For(workspace).NamedRuleIntents;
        if (view == "named-rule-intent-details")
        {
            var entry = SelectNamedRuleIntent(workspace, arguments, inventory);
            return McpJson.ToolResult(new
            {
                workspace = McpWorkspaceTransport.Describe(workspace), view,
                coverage = WorkspaceNamedRuleIntentInventory.Coverage,
                rule = DescribeNamedRuleIntent(entry),
                page = McpPaging.Page(entry.Hints, arguments, workspace.Revision.ToString())
            });
        }

        var unresolved = inventory.UnresolvedPlacementDocuments.Select(document => (object)new
        {
            placementStatus = "unresolved", conflictKind = "UnresolvedPlacement",
            documentId = document.Id.ToString(), path = document.Path.Value,
            action = "Repair conflicting or cyclic imports before selecting a command named-rule owner."
        });
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace), view,
            coverage = WorkspaceNamedRuleIntentInventory.Coverage,
            unresolvedPlacementCount = inventory.UnresolvedPlacementDocuments.Length,
            page = McpPaging.Page(inventory.Entries.Select(DescribeNamedRuleIntent).Concat(unresolved), arguments, workspace.Revision.ToString())
        });
    }

    static WorkspaceNamedRuleIntentEntry SelectNamedRuleIntent(ScreenplayWorkspace workspace, JsonElement arguments, WorkspaceNamedRuleIntentInventory inventory)
    {
        var hasSubject = arguments.TryGetProperty("subject", out var subject);
        if (hasSubject == arguments.TryGetProperty("requirementId", out _)) throw new McpFailure("Select exactly one subject occurrence handle or attached requirementId.", -32602);
        var selected = hasSubject ? McpAstHandles.Read(subject) : null;
        if (selected is not null && selected.Revision != workspace.Revision) throw new McpFailure("StaleRevision: the subject handle belongs to a different workspace snapshot.");
        WorkspaceNamedRuleIntentEntry[] matches = selected is not null
            ? [.. inventory.Entries.Where(entry => Equals(entry.Handle, selected)).Take(2)]
            : [.. inventory.Entries.Where(entry => entry.RequirementId == McpJson.RequiredString(arguments, "requirementId")).Take(2)];
        if (matches.Length > 1) throw new McpFailure("AmbiguousRequirement: select a revision-local named-rule occurrence handle.");
        return matches.SingleOrDefault() ?? throw new McpFailure("UnknownRequirement: no uniquely placed command named-rule occurrence matches. Read named-rule-intents for handles and unresolved placement.");
    }

    static object ReadImplementationRequirementsView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
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

    static object ReadTypedContextsView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
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

    static object ReadSourceMapView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
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

    static object ReadListedView(ScreenplayWorkspace workspace, JsonElement arguments, string view)
    {
        var items = view switch
        {
            "documents" => workspace.Documents.Select(document => DescribeDocument(workspace, document)),
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
            "repairs" => DescribeRepairs(workspace, McpWorkspaceAnalysis.For(workspace).Syntax),
            "executable-diagnostics" => workspace.Compilation.Diagnostics,
            _ => throw new McpFailure("Unknown workspace view.", -32602)
        };
        return McpJson.ToolResult(new
        {
            workspace = McpWorkspaceTransport.Describe(workspace),
            view,
            repairEvidenceRevision = McpRepairEvidence.Revision(workspace),
            page = McpPaging.Page(items, arguments, workspace.Revision.ToString())
        });
    }

    static object DescribeDocument(ScreenplayWorkspace workspace, WorkspaceDocument document) => new
    {
        documentId = document.Id.ToString(),
        path = document.Path.Value,
        document.StableKey,
        document.Encoding,
        byteCount = document.Bytes.Length,
        root = McpAstHandles.Describe(new(workspace.Revision, document.Id, string.Empty))
    };

    static IEnumerable<object> DescribeRepairs(ScreenplayWorkspace workspace, WorkspaceSyntaxIndex syntax) =>
        syntax.RepairableDiagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(syntax, workspace.Revision, diagnostic)
                .Select(repair => (Repair: repair, diagnostic.Location)))
            .Concat(syntax.Entries.Where(entry => entry.Node is ApplicationSyntax).SelectMany(entry => WorkspaceDiagnosticRepairs.FindDocumentOptionality(syntax, entry.Handle).Concat(WorkspaceDiagnosticRepairs.FindDocumentCompliance(syntax, entry.Handle))
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
            });
}
