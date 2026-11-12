// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// A temporary cache and lock holding one locked, restored extension, for specifications.
/// </summary>
public sealed class ExtensionFixture : IDisposable
{
    ExtensionFixture(string root, ExtensionCache cache, ExtensionLock extensionLock, string digest)
    {
        Root = root;
        Cache = cache;
        Lock = extensionLock;
        Digest = digest;
    }

    /// <summary>Gets the temporary directory.</summary>
    public string Root { get; }

    /// <summary>Gets the cache.</summary>
    public ExtensionCache Cache { get; }

    /// <summary>Gets the lock.</summary>
    public ExtensionLock Lock { get; }

    /// <summary>Gets the digest of the stored package.</summary>
    public string Digest { get; }

    /// <summary>
    /// Creates a fixture that locks and restores one extension.
    /// </summary>
    /// <param name="reference">The reference as the model writes it.</param>
    /// <param name="range">The range the lock was written for.</param>
    /// <param name="contract">The contract text.</param>
    /// <param name="restored">Whether the contract is in the cache.</param>
    /// <returns>The fixture.</returns>
    public static ExtensionFixture Locked(string reference, string range, string contract, bool restored = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "screenplay-extension-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var cache = new ExtensionCache(Path.Combine(root, "cache"));
        var bytes = new UTF8Encoding(false).GetBytes(contract);
        var hash = ExtensionHashes.Of(bytes);
        var digest = "sha256:" + new string('d', 64);
        if (restored) cache.Store(digest, bytes);
        var extensionLock = new ExtensionLock([new(reference, range, ExtensionCatalog.Resolve(reference), "2.3.1", digest, hash, "vendored")]);
        return new(root, cache, extensionLock, digest);
    }

    /// <summary>
    /// Gets a resolver over this fixture.
    /// </summary>
    /// <param name="mode">How strictly to report unresolved references.</param>
    /// <returns>The resolver.</returns>
    public ExtensionContractResolver Resolver(ExtensionResolutionMode mode = ExtensionResolutionMode.Strict) => new(Cache, mode, Lock);

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
