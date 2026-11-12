// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Defines where restore finds packages: an OCI registry, npm, or a vendored directory.
/// </summary>
public interface IExtensionPackageSource
{
    /// <summary>
    /// Resolves the newest release of a reference that satisfies a range. Only restore calls this, never the compiler.
    /// </summary>
    /// <param name="resolved">The full reference.</param>
    /// <param name="range">The range, <c>latest</c> for any release.</param>
    /// <returns>The package, or <see langword="null"/> when none satisfies the range.</returns>
    ExtensionPackage? Resolve(string resolved, string range);

    /// <summary>
    /// Fetches the exact package a lock entry pins.
    /// </summary>
    /// <param name="resolved">The full reference.</param>
    /// <param name="version">The locked version.</param>
    /// <param name="digest">The locked digest.</param>
    /// <returns>The package, or <see langword="null"/> when the source does not have it.</returns>
    ExtensionPackage? Fetch(string resolved, string version, string digest);
}

/// <summary>
/// Represents a resolved extension package as far as the compiler needs it.
/// </summary>
/// <param name="Resolved">The full reference.</param>
/// <param name="Version">The version.</param>
/// <param name="Digest">The package digest.</param>
/// <param name="Publisher">The publisher identity.</param>
/// <param name="Contract">The bytes of <c>contract.play</c>.</param>
public record ExtensionPackage(string Resolved, string Version, string Digest, string Publisher, byte[] Contract);

/// <summary>
/// Reads packages vendored into a directory laid out <c>&lt;root&gt;/&lt;full reference&gt;/&lt;version&gt;/contract.play</c>.
/// Its digest is the hash of the contract and its publisher is <c>vendored</c>.
/// </summary>
/// <param name="root">The directory.</param>
public class DirectoryExtensionPackageSource(string root) : IExtensionPackageSource
{
    /// <summary>The publisher recorded for a vendored package.</summary>
    public const string Publisher = "vendored";

    /// <inheritdoc/>
    public ExtensionPackage? Resolve(string resolved, string range)
    {
        var directory = Path.Combine(root, Safe(resolved));
        if (!Directory.Exists(directory)) return null;
        var version = Directory.GetDirectories(directory)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(candidate => ExtensionVersionRange.Satisfies(range, candidate) && File.Exists(Path.Combine(directory, candidate, "contract.play")))
            .Order(Comparer<string>.Create((left, right) => ExtensionVersionRange.Compare(right, left)))
            .FirstOrDefault();
        return version is null ? null : Read(resolved, version);
    }

    /// <inheritdoc/>
    public ExtensionPackage? Fetch(string resolved, string version, string digest) =>
        Read(resolved, version) is { } package && package.Digest == digest ? package : null;

    static string Safe(string reference) => reference.Replace(':', '_');

    ExtensionPackage? Read(string resolved, string version)
    {
        var path = Path.Combine(root, Safe(resolved), version, "contract.play");
        if (!File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        return new(resolved, version, ExtensionHashes.Of(bytes), Publisher, bytes);
    }
}
