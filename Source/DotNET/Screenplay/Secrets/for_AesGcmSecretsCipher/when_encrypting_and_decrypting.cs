// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Screenplay.Secrets.for_AesGcmSecretsCipher;

public class when_encrypting_and_decrypting : Specification
{
    const string PlainText = "super secret value";

    byte[] _key;
    AesGcmSecretsCipher _cipher;
    string _encrypted;
    string _decrypted;

    void Establish()
    {
        _key = RandomNumberGenerator.GetBytes(32);
        _cipher = new();
    }

    void Because()
    {
        _encrypted = _cipher.Encrypt(PlainText, _key);
        _decrypted = _cipher.Decrypt(_encrypted, _key);
    }

    [Fact] void should_prefix_the_encrypted_value() => _encrypted.StartsWith("enc:v1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_round_trip_the_plain_text() => _decrypted.ShouldEqual(PlainText);
    [Fact] void should_use_a_fresh_nonce_per_encryption() => _cipher.Encrypt(PlainText, _key).ShouldNotEqual(_encrypted);
}
