// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Opens a model root as a <see cref="ScreenplayWorkspace"/> with its persisted identities, exactly as the MCP server and
/// <c>screenplay baseline check</c> do, together with the root's release-baseline read.
/// </summary>
/// <remarks>
/// Opening is read-only: it never writes identities, recovery journals, sidecars or source, and it never creates
/// <c>.screenplay/</c>. A pending recovery journal, missing or corrupt identity state, an identity mapping conflict and an
/// unreadable root are returned as a <see cref="ScreenplayModelRootOutcome"/> with a <c>FailureKind</c>, not thrown.
/// </remarks>
public static class ScreenplayModelRoot
{
    internal const string PersistedIdentitiesRequired = "PersistedIdentitiesRequired";
    internal const string PersistedIdentitiesRequiredReason = "PersistedIdentitiesRequired: release baselines require .screenplay/identities.json.";

    /// <summary>
    /// Opens the root as an authoring workspace whose source must still reconcile exactly with the persisted identities.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <returns>The workspace, identities and release-baseline read, or a typed refusal.</returns>
    /// <remarks>
    /// This is the MCP server's open. It refuses source that would change persisted identities, including a removed
    /// declaration or a deleted mapped document, so it can never let a check report a missing baselined revision.
    /// </remarks>
    public static ScreenplayModelRootOutcome Open(string root) => Run(root, forReleaseBaselineCheck: false);

    /// <summary>
    /// Opens the root for read-only use, retaining persisted identities whose declarations or documents were removed.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <returns>The workspace, identities and release-baseline read, or a typed refusal.</returns>
    /// <remarks>
    /// <para>
    /// This is the open <c>screenplay baseline check</c> uses. Mapped documents may be missing and declarations may be removed,
    /// so <see cref="ReleaseBaseline.Check"/> reports them as <c>BaselinedRevisionMissing</c>; unmapped <c>.play</c> files are
    /// still refused. The workspace carries the persisted catalog unchanged, including assignments for removed declarations,
    /// so it must never be used for authoring or serialized back to <c>identities.json</c>.
    /// </para>
    /// <para>
    /// Whenever <see cref="Open"/> succeeds, both forms compile the same documents with the same catalog and produce the same
    /// model revision. This form also succeeds where <see cref="Open"/> refuses, so a renderer that reports release-baseline
    /// warnings should render <c>Workspace.Compilation</c> from this outcome and assess the same workspace: the assessment then
    /// belongs to the rendered model revision by construction.
    /// </para>
    /// </remarks>
    public static ScreenplayModelRootOutcome OpenForReleaseBaselineCheck(string root) => Run(root, forReleaseBaselineCheck: true);

    internal static PersistedIdentityState ReadIdentities(byte[] bytes)
    {
        try
        {
            return PersistedIdentityStateSerializer.Deserialize(bytes);
        }
        catch (InvalidPersistedIdentityState exception)
        {
            throw new ModelRootRefused(
                $"IdentityStateConflict: .screenplay/identities.json is corrupt or unsupported. Restore a known-good identity file; do not delete it to regenerate IDs. {exception.Message}",
                "IdentityStateConflict",
                exception);
        }
    }

    internal static PersistedModelRootRead? Read(ModelRootDirectory root, bool forReleaseBaselineCheck)
    {
        ModelRootMetadata.RefusePending(root);
        var stateBytes = ModelRootMetadata.Read(root, PersistedIdentityStateSerializer.FileName, PersistedIdentityStateSerializer.MaximumBytes);
        if (stateBytes is null) return null;
        var identities = ReadIdentities(stateBytes);
        var workspace = forReleaseBaselineCheck
            ? PersistedWorkspaceOpener.OpenForReleaseBaselineCheck(root, identities)
            : PersistedWorkspaceOpener.Open(root, identities);
        var baselineBytes = ModelRootMetadata.Read(root, ReleaseBaselineStore.FileName, ReleaseBaselineStore.MaximumBytes);

        return new(workspace, identities, stateBytes, baselineBytes, ReleaseBaselineStore.ReadBytes(baselineBytes, workspace.IdentityCatalog.Application));
    }

    static ScreenplayModelRootOutcome Run(string path, bool forReleaseBaselineCheck)
    {
        try
        {
            var read = Read(new ModelRootDirectory(path), forReleaseBaselineCheck);
            return read is null
                ? new(null, null, null, PersistedIdentitiesRequired, PersistedIdentitiesRequiredReason)
                : new(read.Workspace, read.Identities, read.Baseline);
        }
        catch (Exception exception) when (exception is ModelRootRefused or InvalidSemanticContract or InvalidPortablePlayPath or InvalidWorkspaceDocument or
            InvalidScreenplayWorkspace or IOException or UnauthorizedAccessException)
        {
            // Every expected refusal reading the root - including a source the workspace cannot admit (an unsupported
            // path, non-UTF-8 bytes, an invalid document set) - is a failed outcome, never an exception.
            var kind = exception switch
            {
                ModelRootRefused refused => refused.FailureKind,
                InvalidSemanticContract => nameof(InvalidSemanticContract),
                _ => ModelRootRefused.UnreadableModelRoot
            };
            return new(null, null, null, kind, exception.Message);
        }
    }
}
