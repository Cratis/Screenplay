// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Opens a model root's source with its persisted identities, either reconciled for authoring or catalog-retaining for release-baseline checks.
/// </summary>
static class PersistedWorkspaceOpener
{
    internal static ScreenplayWorkspace Open(ModelRootDirectory root, PersistedIdentityState state) => Open(root, state, checkReleaseBaseline: false);

    // Read-only verification must retain catalog identities even when source removed their declarations.
    // This workspace must never be installed as an authoring state or serialized back to identities.json.
    internal static ScreenplayWorkspace OpenForReleaseBaselineCheck(ModelRootDirectory root, PersistedIdentityState state) => Open(root, state, checkReleaseBaseline: true);

    static ScreenplayWorkspace Open(ModelRootDirectory root, PersistedIdentityState state, bool checkReleaseBaseline)
    {
        var documents = ReadMappedDocuments(root, state, checkReleaseBaseline);
        return checkReleaseBaseline ? CompileRetainingCatalog(root, state, documents) : OpenReconciled(root, state, documents);
    }

    static ImmutableArray<WorkspaceDocument> ReadMappedDocuments(ModelRootDirectory root, PersistedIdentityState state, bool checkReleaseBaseline)
    {
        foreach (var mapping in state.Documents)
        {
            _ = root.PathFor(mapping.Path);
        }

        var actual = root.Read(allowEmpty: true).ToDictionary(document => document.Path.Value, StringComparer.Ordinal);
        if (HasMappingConflict(state, actual, checkReleaseBaseline))
        {
            throw new ModelRootRefused(
                "IdentityMappingConflict: mapped .play files are missing, renamed, or accompanied by unmapped files. Restore the mapped paths, then use an MCP proposal to move/add/delete documents explicitly.",
                "IdentityMappingConflict");
        }

        return [.. state.Documents.Where(mapping => actual.ContainsKey(mapping.Path.Value)).Select(mapping => WorkspaceDocument.Create(
            mapping.Id,
            mapping.StableKey,
            mapping.Path,
            actual[mapping.Path.Value].Bytes.AsSpan()))];
    }

    static bool HasMappingConflict(PersistedIdentityState state, Dictionary<string, WorkspaceDocument> actual, bool checkReleaseBaseline) => checkReleaseBaseline
        ? actual.Keys.Any(path => !state.Documents.Any(mapping => mapping.Path.Value == path))
        : actual.Count != state.Documents.Length || state.Documents.Any(mapping => !actual.ContainsKey(mapping.Path.Value));

    static ScreenplayWorkspace CompileRetainingCatalog(ModelRootDirectory root, PersistedIdentityState state, ImmutableArray<WorkspaceDocument> documents)
    {
        var loaded = LoadAttachments(root, documents);
        var compilation = documents.IsEmpty ? ScreenplayWorkspace.EmptyCompilation() : new SemanticModelCompiler().Compile(
            state.ApplicationName, ScreenplayWorkspace.CreateDocumentSet(documents, state.IdentityCatalog, loaded.Contents));
        compilation = compilation with { Diagnostics = [.. compilation.Diagnostics, .. loaded.Diagnostics] };
        return ScreenplayWorkspace.CreateValidated(state.ApplicationName, documents, state.IdentityCatalog, compilation, loaded.Contents, loaded.Diagnostics);
    }

    static ScreenplayWorkspace OpenReconciled(ModelRootDirectory root, PersistedIdentityState state, ImmutableArray<WorkspaceDocument> documents)
    {
        var catalog = state.IdentityCatalog;
        ScreenplayWorkspace workspace;
        try
        {
            workspace = documents.IsEmpty
                ? ScreenplayWorkspace.CreateEmpty(catalog.Application, state.ApplicationName)
                : CreateWithAttachments(root, state, documents);
        }
        catch (Exception exception) when (exception is InvalidScreenplayWorkspace or InvalidSemanticContract)
        {
            throw new ModelRootRefused(
                $"IdentityReconciliationRequired: current source cannot be admitted with the persisted catalog. Restore its declaration names and use explicit identity-change proposals. {exception.Message}",
                "IdentityReconciliationRequired",
                exception);
        }

        var syntax = WorkspaceAuthoringAnalysis.For(workspace).Syntax;
        var addresses = syntax.Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address).ToHashSet();

        // Failed binding retains the old catalog, so identical catalog bytes alone cannot prove source continuity.
        var missingAssignments = !workspace.Compilation.Success &&
            (catalog.Semantics.Any(assignment => !addresses.Contains(assignment.Address)) ||
             catalog.EventContracts.Any(assignment => !addresses.Contains(assignment.Address)));
        if (missingAssignments && !syntax.UnresolvedPlacementDocuments.IsEmpty)
        {
            throw new ModelRootRefused(
                "UnresolvedPlacement: conflicting or cyclic imports prevent proving continuity with the persisted identity catalog. Repair those imports and reopen; identity state was not replaced.",
                "UnresolvedPlacement");
        }

        if (missingAssignments || !SemanticIdentityCatalogSerializer.Serialize(catalog).AsSpan().SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog)))
        {
            throw new ModelRootRefused(
                "IdentityReconciliationRequired: external source edits would change persisted identities. Restore the previous declaration names and apply an explicit semantic/event rename or retirement proposal. Identity state was not replaced.",
                "IdentityReconciliationRequired");
        }

        return workspace;
    }

    static ScreenplayWorkspace CreateWithAttachments(ModelRootDirectory root, PersistedIdentityState state, ImmutableArray<WorkspaceDocument> documents)
    {
        var loaded = LoadAttachments(root, documents);
        return ScreenplayWorkspace.Create(state.IdentityCatalog.Application, state.ApplicationName, documents, state.IdentityCatalog, loaded.Contents, loaded.Diagnostics);
    }

    static AttachmentFileResult LoadAttachments(ModelRootDirectory root, ImmutableArray<WorkspaceDocument> documents) => AttachmentFiles.Load(
        root.DirectoryPath,
        [.. documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text))]);
}
