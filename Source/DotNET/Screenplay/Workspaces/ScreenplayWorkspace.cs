// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;
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
        WorkspaceRevision revision)
    {
        ApplicationName = applicationName;
        Documents = documents;
        IdentityCatalog = identityCatalog;
        Compilation = compilation;
        Revision = revision;
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

    /// <summary>
    /// Proposes one pure revision-bound transaction without mutating this workspace or touching a destination.
    /// </summary>
    /// <param name="request">The complete transaction request.</param>
    /// <returns>A new immutable workspace and write plan, or typed conflicts.</returns>
    public WorkspaceTransactionResult Propose(WorkspaceTransactionRequest request) =>
        new WorkspaceTransaction(this).Propose(request);

    internal static ScreenplayWorkspace CreateValidated(
        string applicationName,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        CompilationResult<SemanticCompilation> compilation) =>
        new(
            applicationName,
            documents,
            identityCatalog,
            compilation,
            WorkspaceCanonicalRevision.Compute(applicationName, documents, identityCatalog));

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
        SemanticIdentityCatalog identityCatalog) =>
        SemanticDocumentSet.Create(
        [
            .. documents.Select(document => SemanticSourceDocument.Create(
                document.Id,
                document.StableKey,
                document.Path.Value,
                document.Text))
        ],
        identityCatalog);

    static ScreenplayWorkspace CreateCore(
        string applicationName,
        ApplicationIdentity? explicitApplicationIdentity,
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog)
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
            var compilation = Compile(normalizedName, ordered, identityCatalog);
            if (compilation.Success)
            {
                identityCatalog = MaterializeCatalog(ordered, identityCatalog, compilation.Value!);
                compilation = Compile(normalizedName, ordered, identityCatalog);
            }

            return CreateValidated(normalizedName, ordered, identityCatalog, compilation);
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
        SemanticIdentityCatalog identityCatalog) =>
        new SemanticModelCompiler().Compile(applicationName, CreateDocumentSet(documents, identityCatalog));

    static SemanticIdentityCatalog MaterializeCatalog(
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog,
        SemanticCompilation compilation)
    {
        var index = SemanticCompilationIndex.Create(compilation.Model.Application, identityCatalog.Application);
        return SemanticIdentityCatalog.PlanMigration(
            identityCatalog,
            identityCatalog.Revision,
            [.. documents.Select(document => document.StableKey)],
            [.. index.Declarations.Keys.Order(WorkspaceSemanticAddressComparer.Instance)],
            [.. index.Events.Keys.Order(WorkspaceSemanticAddressComparer.Instance)],
            [],
            [],
            []).Catalog;
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
