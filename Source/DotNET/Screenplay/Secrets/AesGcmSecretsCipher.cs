// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// Represents an implementation of <see cref="ISecretsCipher"/> using AES-256-GCM.
/// </summary>
/// <remarks>
/// Every encryption uses a fresh random 12-byte nonce, so encrypting the same value twice yields
/// different tokens. The encrypted format is <c>enc:v1:</c> followed by the base64 encoding of
/// nonce || ciphertext || 16-byte authentication tag.
/// </remarks>
public class AesGcmSecretsCipher : ISecretsCipher
{
    /// <summary>
    /// The prefix identifying the encrypted format and its version.
    /// </summary>
    public const string Prefix = "enc:v1:";

    const int KeySize = 32;
    const int NonceSize = 12;
    const int TagSize = 16;

    /// <inheritdoc/>
    public string Encrypt(string plainText, ReadOnlySpan<byte> key)
    {
        ThrowIfInvalidKey(key);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var payload = new byte[NonceSize + plainBytes.Length + TagSize];
        var nonce = payload.AsSpan(0, NonceSize);
        var cipherText = payload.AsSpan(NonceSize, plainBytes.Length);
        var tag = payload.AsSpan(NonceSize + plainBytes.Length, TagSize);

        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        return Prefix + Convert.ToBase64String(payload);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidEncryptedSecret">The value does not have the <c>enc:v1:&lt;base64&gt;</c> format.</exception>
    /// <exception cref="SecretDecryptionFailed">The value was tampered with or the key is wrong.</exception>
    public string Decrypt(string encryptedValue, ReadOnlySpan<byte> key)
    {
        ThrowIfInvalidKey(key);

        var payload = GetPayload(encryptedValue);
        var nonce = payload.AsSpan(0, NonceSize);
        var cipherText = payload.AsSpan(NonceSize, payload.Length - NonceSize - TagSize);
        var tag = payload.AsSpan(payload.Length - TagSize, TagSize);
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(key, TagSize);
        try
        {
            aes.Decrypt(nonce, cipherText, tag, plainBytes);
        }
        catch (CryptographicException exception)
        {
            throw new SecretDecryptionFailed(exception);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    static byte[] GetPayload(string encryptedValue)
    {
        if (!encryptedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new InvalidEncryptedSecret(encryptedValue);
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(encryptedValue[Prefix.Length..]);
        }
        catch (FormatException)
        {
            throw new InvalidEncryptedSecret(encryptedValue);
        }

        if (payload.Length < NonceSize + TagSize)
        {
            throw new InvalidEncryptedSecret(encryptedValue);
        }

        return payload;
    }

    static void ThrowIfInvalidKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
        {
            throw new InvalidSecretsKey(key.Length, KeySize);
        }
    }
}
