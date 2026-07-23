// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Screenplay.Secrets.for_AesGcmSecretsCipher;

public class when_decrypting_tampered_ciphertext : Specification
{
    byte[] _key;
    AesGcmSecretsCipher _cipher;
    Exception _error;

    void Establish()
    {
        _key = RandomNumberGenerator.GetBytes(32);
        _cipher = new();
    }

    void Because()
    {
        var payload = Convert.FromBase64String(_cipher.Encrypt("super secret value", _key)[AesGcmSecretsCipher.Prefix.Length..]);
        payload[^1] ^= 0xFF;
        var tampered = AesGcmSecretsCipher.Prefix + Convert.ToBase64String(payload);
        _error = Catch.Exception(() => _cipher.Decrypt(tampered, _key));
    }

    [Fact] void should_fail_to_decrypt() => _error.ShouldBeOfExactType<SecretDecryptionFailed>();
}
