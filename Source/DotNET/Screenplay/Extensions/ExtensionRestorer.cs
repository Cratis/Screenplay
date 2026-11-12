// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Restores extension contracts into the cache and keeps the lock current.
/// </summary>
/// <remarks>
/// Only restore resolves a range, and only when asked to resolve (an update, or an editor restoring a reference that is
/// not locked yet). Restoring from the lock fetches exactly the pinned digest and never looks at a range.
/// </remarks>
/// <param name="cache">The <see cref="ExtensionCache"/> to fill.</param>
/// <param name="source">The <see cref="IExtensionPackageSource"/> to fetch from.</param>
public class ExtensionRestorer(ExtensionCache cache, IExtensionPackageSource source)
{
    /// <summary>
    /// Fetches into the cache every contract the lock pins that the cache does not hold. Resolves nothing.
    /// </summary>
    /// <param name="extensionLock">The lock.</param>
    /// <returns>The diagnostics for entries that could not be restored.</returns>
    public IReadOnlyList<Diagnostic> RestoreFromLock(ExtensionLock extensionLock)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var entry in extensionLock.Entries)
        {
            if (cache.TryRead(entry.Digest) is { } cached && ExtensionHashes.Of(cached) == entry.ContractHash) continue;
            var package = source.Fetch(entry.Resolved, entry.Version, entry.Digest);
            if (package is null)
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.ExtensionNotRestored, $"Extension '{entry.Reference}' {entry.Version} ({entry.Digest}) could not be fetched from its source.", SourceLocation.Start));
            }
            else if (ExtensionHashes.Of(package.Contract) != entry.ContractHash)
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.ExtensionContractHashMismatch, $"Extension '{entry.Reference}' {entry.Version} no longer has the contract the lock pinned.", SourceLocation.Start));
            }
            else
            {
                cache.Store(entry.Digest, package.Contract);
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Resolves the references a model asks for that the lock does not pin (or pins for a different range), and pins them.
    /// </summary>
    /// <param name="references">The model's <c>extension</c> lines.</param>
    /// <param name="extensionLock">The current lock.</param>
    /// <param name="updateAll">Whether to re-resolve references the lock already pins for the same range.</param>
    /// <returns>The new lock and any diagnostics.</returns>
    public (ExtensionLock Lock, IReadOnlyList<Diagnostic> Diagnostics) Resolve(IEnumerable<SystemExtensionSyntax> references, ExtensionLock extensionLock, bool updateAll = false)
    {
        var diagnostics = new List<Diagnostic>();
        var result = extensionLock;
        foreach (var reference in references.DistinctBy(item => item.Reference))
        {
            var range = ExtensionLock.RangeOf(reference);
            if (!updateAll && result.Find(reference.Reference) is { } existing && existing.Range == range) continue;
            var resolved = ExtensionCatalog.Resolve(reference.Reference);
            var package = source.Resolve(resolved, range);
            if (package is null)
            {
                diagnostics.Add(Diagnostic.Error(DiagnosticCodes.ExtensionNotRestored, $"No release of '{reference.Reference}' ({resolved}) satisfies version '{range}'.", reference.Location));
                continue;
            }

            var contractHash = ExtensionHashes.Of(package.Contract);
            cache.Store(package.Digest, package.Contract);
            result = result.With(new(reference.Reference, range, package.Resolved, package.Version, package.Digest, contractHash, package.Publisher));
        }

        return (result, diagnostics);
    }
}
