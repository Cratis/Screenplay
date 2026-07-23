// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// Defines a cipher that encrypts and decrypts secret values with a caller supplied key.
/// </summary>
/// <remarks>
/// Key management is out of scope for this library - the caller owns, stores and backs up the key.
/// </remarks>
public interface ISecretsCipher
{
    /// <summary>
    /// Encrypts a plaintext secret value.
    /// </summary>
    /// <param name="plainText">The plaintext value to encrypt.</param>
    /// <param name="key">The 256-bit key to encrypt with.</param>
    /// <returns>The encrypted value in the <c>enc:v1:&lt;base64&gt;</c> format.</returns>
    string Encrypt(string plainText, ReadOnlySpan<byte> key);

    /// <summary>
    /// Decrypts an encrypted secret value.
    /// </summary>
    /// <param name="encryptedValue">The encrypted value in the <c>enc:v1:&lt;base64&gt;</c> format.</param>
    /// <param name="key">The 256-bit key to decrypt with.</param>
    /// <returns>The decrypted plaintext value.</returns>
    string Decrypt(string encryptedValue, ReadOnlySpan<byte> key);
}
