// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Represents the digest-keyed local cache of extension contracts the compiler reads.
/// </summary>
/// <param name="root">The cache directory.</param>
public class ExtensionCache(string root)
{
    /// <summary>The environment variable that overrides the default cache directory.</summary>
    public const string EnvironmentVariable = "SCREENPLAY_EXTENSION_CACHE";

    /// <summary>
    /// Gets the default cache: the directory in <see cref="EnvironmentVariable"/>, else a folder in the user's local application data.
    /// </summary>
    public static ExtensionCache Default => new(
        Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cratis", "Screenplay", "extensions"));

    /// <summary>
    /// Gets the cache directory.
    /// </summary>
    public string Root => root;

    /// <summary>
    /// Decodes cached contract bytes.
    /// </summary>
    /// <param name="contract">The bytes.</param>
    /// <returns>The text.</returns>
    public static string Decode(byte[] contract) => new UTF8Encoding(false).GetString(contract);

    /// <summary>
    /// Gets the path of the cached contract for a digest.
    /// </summary>
    /// <param name="digest">The package digest.</param>
    /// <returns>The file path.</returns>
    public string PathOf(string digest) => Path.Combine(root, digest.Replace(':', '-'), "contract.extension");

    /// <summary>
    /// Reads the cached contract bytes for a digest.
    /// </summary>
    /// <param name="digest">The package digest.</param>
    /// <returns>The bytes, or <see langword="null"/> when the digest is not cached.</returns>
    public byte[]? TryRead(string digest) => ExtensionHashes.IsWellFormed(digest) && File.Exists(PathOf(digest)) ? File.ReadAllBytes(PathOf(digest)) : null;

    /// <summary>
    /// Stores a contract under a digest.
    /// </summary>
    /// <param name="digest">The package digest.</param>
    /// <param name="contract">The bytes of <c>contract.play</c>.</param>
    public void Store(string digest, byte[] contract)
    {
        if (!ExtensionHashes.IsWellFormed(digest)) throw new ArgumentException($"'{digest}' is not a sha256 digest.", nameof(digest));
        var path = PathOf(digest);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllBytes(temporary, contract);
        File.Move(temporary, path, overwrite: true);
    }
}
