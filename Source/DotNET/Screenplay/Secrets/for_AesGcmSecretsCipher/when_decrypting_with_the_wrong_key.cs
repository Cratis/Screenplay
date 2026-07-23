// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Screenplay.Secrets.for_AesGcmSecretsCipher;

public class when_decrypting_with_the_wrong_key : Specification
{
    byte[] _key;
    byte[] _wrongKey;
    AesGcmSecretsCipher _cipher;
    Exception _error;

    void Establish()
    {
        _key = RandomNumberGenerator.GetBytes(32);
        _wrongKey = RandomNumberGenerator.GetBytes(32);
        _cipher = new();
    }

    void Because() => _error = Catch.Exception(() => _cipher.Decrypt(_cipher.Encrypt("super secret value", _key), _wrongKey));

    [Fact] void should_fail_to_decrypt() => _error.ShouldBeOfExactType<SecretDecryptionFailed>();
}
