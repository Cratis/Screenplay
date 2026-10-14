// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents one immutable exact-source Screenplay authoring workspace.
/// </summary>
public sealed class ScreenplayWorkspace
{
    ScreenplayWorkspace(
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        CompilationResult<SemanticCompilation> compilation,
        WorkspaceRevision revision,
        ImmutableDictionary<string, string> attachmentContents,
        ImmutableArray<Diagnostic> attachmentDiagnostics)
    {
        ApplicationName = applicationName;
        Documents = documents;
        IdentityCatalog = identityCatalog;
        Compilation = compilation;
        Revision = revision;
        AttachmentContents = attachmentContents;
        AttachmentDiagnostics = attachmentDiagnostics;
    }

    /// <summary>
    /// Gets the application name supplied to semantic compilation.
    /// </summary>
    public string ApplicationName { get; }

    /// <summary>
    /// Gets documents in stable document-identity order.
    /// </summary>
    public ImmutableArray<WorkspaceDocument> Documents { get; }

    /// <summary>
    /// Gets the authoritative semantic identity catalog.
    /// </summary>
    public SemanticIdentityCatalog IdentityCatalog { get; }

    /// <summary>
    /// Gets the current derived semantic compilation, including diagnostics when the authored source is not bindable.
    /// </summary>
    public CompilationResult<SemanticCompilation> Compilation { get; }

    /// <summary>
    /// Gets the deterministic revision of exact documents and identity assignments.
    /// </summary>
    public WorkspaceRevision Revision { get; }

    /// <summary>Gets host-supplied attachment text, which is never serialized or included in the workspace revision.</summary>
    public ImmutableDictionary<string, string> AttachmentContents { get; }

    /// <summary>Gets host-supplied attachment loading warnings, which are not persisted.</summary>
    public ImmutableArray<Diagnostic> AttachmentDiagnostics { get; }

    /// <summary>
    /// Creates an immutable workspace without discarding authored source when semantic compilation fails.
    /// </summary>
    /// <param name="applicationName">The application name supplied to semantic compilation.</param>
    /// <param name="documents">The complete exact document set.</param>
    /// <param name="identityCatalog">The authoritative identity catalog.</param>
    /// <returns>The admitted workspace and its current derived compilation.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">The application, documents, paths, or catalog are structurally invalid.</exception>
    public static ScreenplayWorkspace Create(
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog) =>
        CreateCore(applicationName, null, documents, identityCatalog);

    /// <summary>
    /// Creates an immutable workspace with an application identity independent from its friendly semantic name.
    /// </summary>
    /// <param name="applicationIdentity">The stable application identity.</param>
    /// <param name="applicationName">The friendly application name supplied to semantic compilation.</param>
    /// <param name="documents">The complete exact document set.</param>
    /// <param name="identityCatalog">The authoritative identity catalog.</param>
    /// <returns>The admitted workspace and its current derived compilation.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">The application, identity, documents, paths, or catalog are structurally invalid.</exception>
    public static ScreenplayWorkspace Create(
        ApplicationIdentity applicationIdentity,
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog) =>
        CreateCore(applicationName, applicationIdentity, documents, identityCatalog);

    /// <summary>Creates a workspace with already loaded host attachment inputs without a second compilation.</summary>
    /// <param name="applicationIdentity">The stable application identity.</param>
    /// <param name="applicationName">The friendly application name.</param>
    /// <param name="documents">The complete exact document set.</param>
    /// <param name="identityCatalog">The authoritative identity catalog.</param>
    /// <param name="contents">Host-supplied attachment contents.</param>
    /// <param name="diagnostics">Attachment loading warnings.</param>
    /// <returns>The admitted workspace and its derived compilation.</returns>
    public static ScreenplayWorkspace Create(
        ApplicationIdentity applicationIdentity,
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        ImmutableDictionary<string, string> contents,
        ImmutableArray<Diagnostic> diagnostics) =>
        CreateCore(applicationName, applicationIdentity, documents, identityCatalog, SemanticDocumentSet.NormalizeAttachments(contents), diagnostics);

    /// <summary>
    /// Creates an empty authoring workspace for bootstrapping a new application's first typed documents.
    /// Empty source is not executable and does not relax ordinary workspace admission or strict transactions.
    /// </summary>
    /// <param name="applicationIdentity">The stable application identity.</param>
    /// <param name="applicationName">The friendly application name.</param>
    /// <returns>The empty, revision-bound authoring workspace.</returns>
    /// <exception cref="InvalidSemanticContract">The application identity or name is invalid.</exception>
    public static ScreenplayWorkspace CreateEmpty(ApplicationIdentity applicationIdentity, string applicationName) =>
        CreateEmpty(applicationIdentity, applicationName, SemanticIdentityCatalog.Empty(applicationIdentity));

    /// <summary>
    /// Re-homes this workspace's identities onto a different document split of the same model.
    /// </summary>
    /// <param name="documents">The complete new exact-source document set.</param>
    /// <returns>A new workspace whose identities can be serialized with <see cref="PersistedIdentityStateSerializer"/>.</returns>
    /// <remarks>
    /// Semantic and event-contract IDs and origins are kept by address, as are event revisions unless explicitly
    /// declared source advances them forward. Document assignments for re-supplied keys are kept; other old document
    /// assignments are dropped, and new keys receive deterministic bootstrap IDs. Supplied document IDs are rebound
    /// to these assignments. No semantic or event address may be retired or newly bootstrapped.
    /// Changed document assignments change the catalog revision; changed documents or assignments change the workspace
    /// revision, invalidating proposals bound to the old revisions.
    /// Attachments are not carried; re-apply them with <see cref="WithAttachmentContents"/>.
    /// Serialize the result with <see cref="PersistedIdentityStateSerializer.From"/> and write exactly its documents
    /// to a model root for strict <see cref="ScreenplayModelRoot.Open"/> acceptance.
    /// Before overwriting an existing MCP-authored identities.json, compare its event assignments: different
    /// assignments turn previously baselined entries into BaselinedRevisionMissing. This method never writes files.
    /// </remarks>
    /// <exception cref="InvalidScreenplayWorkspace">
    /// Source does not compile, semantic or event addresses differ (listed as retiredAddresses or addedAddresses),
    /// document keys, IDs, or portable paths collide, or a document lies outside what a model root discovers
    /// (excluded directories such as <c>bin</c>, <c>obj</c>, <c>node_modules</c>, <c>.git</c> or <c>.screenplay</c>,
    /// more than 16 directory levels, or more files or source bytes than a model root admits).
    /// </exception>
    public ScreenplayWorkspace Rehome(ImmutableArray<WorkspaceDocument> documents)
    {
        try
        {
            var admitted = AdmitDocuments(documents);
            RequireModelRootScope(admitted);
            var keys = admitted.Select(document => document.StableKey).ToHashSet(StringComparer.Ordinal);
            var catalog = SemanticIdentityCatalog.Create(
                IdentityCatalog.Application,
                [.. IdentityCatalog.Documents.Where(assignment => keys.Contains(assignment.Key))],
                IdentityCatalog.Semantics,
                IdentityCatalog.EventContracts);
            var rebound = admitted.Select(document => WorkspaceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.Path, document.Bytes.AsSpan())).ToImmutableArray();
            catalog = VerifyRehome(ApplicationName, rebound, catalog);
            var workspace = CreateCore(ApplicationName, catalog.Application, rebound, catalog);
            RequireRehomeCompilation(workspace.Compilation);
            return workspace;
        }
        catch (Exception exception) when (exception is InvalidSemanticContract or InvalidWorkspaceDocument or ModelRootRefused)
        {
            throw new InvalidScreenplayWorkspace(exception.Message, exception);
        }
    }

    /// <summary>
    /// Recompiles with immutable host-supplied contents and warnings, without changing source, identities, or revision.
    /// Neither the serializer nor transactions read the file system; callers must refresh these inputs themselves.
    /// </summary>
    /// <param name="contents">Normalized repository-relative paths and their contents.</param>
    /// <param name="diagnostics">Attachment loading warnings.</param>
    /// <returns>A new immutable workspace with derived compilation.</returns>
    public ScreenplayWorkspace WithAttachmentContents(ImmutableDictionary<string, string> contents, ImmutableArray<Diagnostic> diagnostics)
    {
        var normalized = SemanticDocumentSet.NormalizeAttachments(contents);
        var compilation = Documents.IsEmpty ? EmptyCompilation() : Compile(ApplicationName, Documents, IdentityCatalog, normalized, diagnostics);
        return CreateValidated(ApplicationName, Documents, IdentityCatalog, compilation, normalized, diagnostics);
    }

    /// <summary>
    /// Proposes one pure revision-bound transaction without mutating this workspace or touching a destination.
    /// </summary>
    /// <param name="request">The complete transaction request.</param>
    /// <returns>A new immutable workspace and write plan, or typed conflicts.</returns>
    public WorkspaceTransactionResult Propose(WorkspaceTransactionRequest request) =>
        new WorkspaceTransaction(this).Propose(request);

    /// <summary>
    /// Proposes one atomic typed-source transaction with source validity independent from executable readiness.
    /// The existing <see cref="Propose"/> contract remains executable-only.
    /// </summary>
    /// <param name="request">The revision-bound typed authoring request.</param>
    /// <returns>The source-authoring verdict, executable readiness, and accepted write plan.</returns>
    public WorkspaceAuthoringResult ProposeAuthoring(WorkspaceAuthoringRequest request) =>
        new WorkspaceAuthoringTransaction(this).Propose(request);

    /// <summary>
    /// Proposes a logical rename, repairing proven typed references and migrating assigned descendant identities.
    /// No source or catalog is written by a proposal.
    /// </summary>
    /// <param name="request">The revision-bound rename and explicit formatting policy.</param>
    /// <returns>The accepted immutable candidate and write plan, or a bounded refusal.</returns>
    public WorkspaceAuthoringResult ProposeRename(WorkspaceRenameRequest request) =>
        new WorkspaceRefactoring(this).Rename(request);

    /// <summary>
    /// Proposes moving every fragment of a logical slice or feature, preserving identities and behavior.
    /// </summary>
    /// <param name="request">The revision-bound addresses and formatting policy.</param>
    /// <returns>The immutable candidate and continuity report, or a bounded refusal.</returns>
    public WorkspaceAuthoringResult ProposeMove(WorkspaceMoveRequest request) =>
        new WorkspaceRefactoring(this).Move(request);

    /// <summary>
    /// Proposes a selected-set property change along a resolved mapping chain without writing source or identities.
    /// </summary>
    /// <param name="request">The original handles, expected names and explicit event evolution choice.</param>
    /// <returns>The atomic authoring verdict and retained property report.</returns>
    public WorkspaceAuthoringResult ProposeProperty(WorkspacePropertyRequest request) =>
        new WorkspaceRefactoring(this).Property(request);

    internal static ScreenplayWorkspace CreateEmpty(ApplicationIdentity applicationIdentity, string applicationName, SemanticIdentityCatalog catalog)
    {
        var name = SemanticDocumentText.NormalizeRequiredUnicode(applicationName, "workspace application name");
        if (catalog.Application != applicationIdentity || !catalog.Documents.IsEmpty || !catalog.EventContracts.IsEmpty ||
            catalog.Semantics.Any(assignment => assignment.Address.Kind != SemanticKind.Application))
        {
            throw new InvalidSemanticContract("An empty authoring workspace cannot carry document or source declaration assignments.");
        }

        return CreateValidated(name, [], catalog, EmptyCompilation());
    }

    internal static CompilationResult<SemanticCompilation> EmptyCompilation() => CompilationResult<SemanticCompilation>.Failed(
        [Diagnostic.Error(DiagnosticCodes.EmptyAuthoringWorkspace, "An empty authoring workspace has no executable source documents.", SourceLocation.Start)]);

    internal static ScreenplayWorkspace CreateValidated(
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        CompilationResult<SemanticCompilation> compilation,
        ImmutableDictionary<string, string>? attachmentContents = null,
        ImmutableArray<Diagnostic> attachmentDiagnostics = default) =>
        new(
            applicationName,
            documents,
            identityCatalog,
            compilation,
            WorkspaceCanonicalRevision.Compute(applicationName, documents, identityCatalog),
            attachmentContents ?? ImmutableDictionary.Create<string, string>(StringComparer.Ordinal),
            attachmentDiagnostics.IsDefault ? [] : attachmentDiagnostics);

    internal static ImmutableArray<WorkspaceDocument> AdmitDocuments(ImmutableArray<WorkspaceDocument> documents)
    {
        if (documents.IsDefaultOrEmpty)
        {
            throw new InvalidScreenplayWorkspace("A Screenplay workspace requires a non-default, non-empty document array.");
        }

        var ids = new HashSet<DocumentId>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var paths = new HashSet<PortablePlayPath>(PortablePlayPath.CollisionComparer);
        foreach (var document in documents)
        {
            if (document is null || !ids.Add(document.Id) || !keys.Add(document.StableKey))
            {
                throw new InvalidScreenplayWorkspace("A workspace document identity or stable key is duplicated.");
            }

            if (!paths.Add(document.Path))
            {
                throw new InvalidScreenplayWorkspace($"Workspace path '{document.Path}' collides with another portable document path.");
            }
        }

        return [.. documents.OrderBy(document => document.Id.ToString(), StringComparer.Ordinal)];
    }

    internal static SemanticDocumentSet CreateDocumentSet(
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        ImmutableDictionary<string, string>? attachmentContents = null) =>
        SemanticDocumentSet.Create(
        [
            .. documents.Select(document => SemanticSourceDocument.Create(
                document.Id,
                document.StableKey,
                document.Path.Value,
                document.Text))
        ],
        identityCatalog,
        attachmentContents);

    // A re-homed set must be exactly what a strict ScreenplayModelRoot.Open can discover again.
    static void RequireModelRootScope(ImmutableArray<WorkspaceDocument> documents)
    {
        ModelRootDirectory.CheckDocuments(documents);
        foreach (var document in documents)
        {
            ModelRootDirectory.CheckScope(document.Path);
        }
    }

    static void RequireRehomeCompilation(CompilationResult<SemanticCompilation> compilation)
    {
        if (!compilation.Success)
        {
            throw new InvalidScreenplayWorkspace($"Re-home source does not compile: {string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        }
    }

    static SemanticIdentityCatalog VerifyRehome(string applicationName, ImmutableArray<WorkspaceDocument> documents, SemanticIdentityCatalog catalog)
    {
        var discovery = Compile(applicationName, documents, catalog);
        if (!discovery.Success)
        {
            // A stale assignment refuses before returning an index. Discover independently to report removed addresses.
            discovery = Compile(applicationName, documents, SemanticIdentityCatalog.Create(catalog.Application, catalog.Documents, [], []));
        }

        RequireRehomeCompilation(discovery);
        var index = SemanticCompilationIndex.Create(discovery.Value!.Model.Application, catalog.Application);
        var addresses = index.Declarations.Keys.ToHashSet();
        var events = index.Events.Keys.ToHashSet();
        var retiredAddresses = catalog.Semantics.Select(assignment => assignment.Address).Except(addresses)
            .Concat(catalog.EventContracts.Select(assignment => assignment.Address).Except(events));
        var addedAddresses = addresses.Except(catalog.Semantics.Select(assignment => assignment.Address))
            .Concat(events.Except(catalog.EventContracts.Select(assignment => assignment.Address)));
        IdentityMigrationIssue.Reject(
            "A re-home must preserve every semantic and event address.",
            retiredAddresses.Select(address => new IdentityMigrationIssue([nameof(retiredAddresses)], address))
                .Concat(addedAddresses.Select(address => new IdentityMigrationIssue([nameof(addedAddresses)], address))));
        var advancements = index.Events.Where(entry => entry.Value.Revision.Value > catalog.ResolveEventContract(entry.Key).Revision.Value)
            .Select(entry => new EventContractRevisionAdvancement(entry.Key, entry.Value.Revision)).ToImmutableArray();
        if (advancements.IsEmpty)
        {
            return catalog;
        }

        var advanced = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            catalog, catalog.Revision, [.. documents.Select(document => document.StableKey)], [.. addresses], [.. events], advancements).Catalog;
        return SemanticIdentityCatalog.Create(
            advanced.Application,
            advanced.Documents,
            advanced.Semantics,
            [.. advanced.EventContracts.Select(assignment => assignment with { Origin = catalog.ResolveEventContract(assignment.Address).Origin })]);
    }

    static ScreenplayWorkspace CreateCore(
        string applicationName,
        ApplicationIdentity? explicitApplicationIdentity,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        ImmutableDictionary<string, string>? attachmentContents = null,
        ImmutableArray<Diagnostic> attachmentDiagnostics = default)
    {
        try
        {
            var normalizedName = SemanticDocumentText.NormalizeRequiredUnicode(applicationName, "workspace application name");
            var applicationIdentity = explicitApplicationIdentity ?? ApplicationIdentity.Create(normalizedName);
            if (!applicationIdentity.IsSet || identityCatalog is null || identityCatalog.Application != applicationIdentity)
            {
                var subject = explicitApplicationIdentity.HasValue ? "application identity" : "application name";
                throw new InvalidScreenplayWorkspace($"The workspace {subject} and identity catalog describe different applications.");
            }

            var ordered = AdmitDocuments(documents);
            var compilation = Compile(normalizedName, ordered, identityCatalog, attachmentContents, attachmentDiagnostics);
            if (compilation.Success)
            {
                identityCatalog = MaterializeCatalog(ordered, identityCatalog, compilation.Value!);
                compilation = Compile(normalizedName, ordered, identityCatalog, attachmentContents, attachmentDiagnostics);
            }

            return CreateValidated(normalizedName, ordered, identityCatalog, compilation, attachmentContents, attachmentDiagnostics);
        }
        catch (InvalidScreenplayWorkspace)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidSemanticContract or InvalidWorkspaceDocument)
        {
            throw new InvalidScreenplayWorkspace(exception.Message, exception);
        }
    }

    static CompilationResult<SemanticCompilation> Compile(
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        ImmutableDictionary<string, string>? attachmentContents = null,
        ImmutableArray<Diagnostic> attachmentDiagnostics = default)
    {
        var result = new SemanticModelCompiler().Compile(applicationName, CreateDocumentSet(documents, identityCatalog, attachmentContents));
        return attachmentDiagnostics.IsDefaultOrEmpty ? result : result with { Diagnostics = [.. result.Diagnostics, .. attachmentDiagnostics] };
    }

    static SemanticIdentityCatalog MaterializeCatalog(
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        SemanticCompilation compilation)
    {
        var index = SemanticCompilationIndex.Create(compilation.Model.Application, identityCatalog.Application);
        var keys = documents.Select(document => document.StableKey).ToImmutableArray();
        var addresses = index.Declarations.Keys.Order(WorkspaceSemanticAddressComparer.Instance).ToImmutableArray();
        var events = index.Events.Keys.Order(WorkspaceSemanticAddressComparer.Instance).ToImmutableArray();
        var catalog = SemanticIdentityCatalog.PlanMigration(
            identityCatalog,
            identityCatalog.Revision,
            keys,
            addresses,
            events,
            [],
            [],
            []).Catalog;
        var newGenerations = index.Events
            .Where(entry => entry.Value.Revision.Value > 1 &&
                !identityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(entry.Key)))
            .Select(entry => new EventContractRevisionAdvancement(entry.Key, entry.Value.Revision)).ToImmutableArray();
        return newGenerations.IsEmpty ? catalog : SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            catalog, catalog.Revision, keys, addresses, events, newGenerations).Catalog;
    }
}

/// <summary>
/// The exception that is thrown when an exact workspace aggregate is structurally invalid.
/// </summary>
/// <param name="message">The reason the workspace was rejected.</param>
/// <param name="innerException">The underlying contract exception, when available.</param>
public sealed class InvalidScreenplayWorkspace(string message, Exception? innerException = null)
    : Exception(message, innerException);

static class WorkspaceCanonicalRevision
{
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    internal static WorkspaceRevision Compute(
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog)
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteText(writer, applicationName);
        WriteText(writer, identityCatalog.Application.ToString());
        WriteText(writer, identityCatalog.Revision.ToString());
        WriteUInt32(writer, checked((uint)documents.Length));
        foreach (var document in documents.OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            WriteText(writer, document.Id.ToString());
            WriteText(writer, document.StableKey);
            WriteText(writer, document.Path.Value);
            WriteUInt32(writer, checked((uint)document.Encoding));
            WriteBytes(writer, document.Bytes.AsSpan());
        }

        return WorkspaceRevision.Compute(writer.WrittenSpan);
    }

    static void WriteText(ArrayBufferWriter<byte> writer, string value)
    {
        var byteCount = _strictUtf8.GetByteCount(value);
        WriteUInt32(writer, checked((uint)byteCount));
        var destination = writer.GetSpan(byteCount);
        var written = _strictUtf8.GetBytes(value, destination);
        writer.Advance(written);
    }

    static void WriteBytes(ArrayBufferWriter<byte> writer, ReadOnlySpan<byte> value)
    {
        WriteUInt32(writer, checked((uint)value.Length));
        value.CopyTo(writer.GetSpan(value.Length));
        writer.Advance(value.Length);
    }

    static void WriteUInt32(ArrayBufferWriter<byte> writer, uint value)
    {
        var destination = writer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        writer.Advance(sizeof(uint));
    }
}
