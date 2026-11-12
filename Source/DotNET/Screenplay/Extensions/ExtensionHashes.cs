// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Computes the hashes the lock pins.
/// </summary>
public static class ExtensionHashes
{
    /// <summary>
    /// Hashes the exact bytes of a contract file.
    /// </summary>
    /// <param name="contract">The bytes of <c>contract.play</c>.</param>
    /// <returns>The hash written <c>sha256:&lt;hex&gt;</c>.</returns>
    public static string Of(ReadOnlySpan<byte> contract) => "sha256:" + Convert.ToHexString(SHA256.HashData(contract)).ToLowerInvariant();

    /// <summary>
    /// Determines whether text is a well-formed digest or hash.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns><see langword="true"/> when it is <c>sha256:</c> followed by 64 lower-case hex digits.</returns>
    public static bool IsWellFormed(string value) =>
        value.Length == 71 && value.StartsWith("sha256:", StringComparison.Ordinal) && IsHex(value.AsSpan(7));

    static bool IsHex(ReadOnlySpan<char> text)
    {
        foreach (var c in text) if (!(c is (>= '0' and <= '9') or (>= 'a' and <= 'f'))) return false;
        return true;
    }
}
