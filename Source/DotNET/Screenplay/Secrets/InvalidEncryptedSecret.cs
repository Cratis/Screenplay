// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// The exception that is thrown when an encrypted secret value does not have the expected
/// <c>enc:v1:&lt;base64&gt;</c> format.
/// </summary>
/// <param name="encryptedValue">The malformed encrypted value.</param>
public class InvalidEncryptedSecret(string encryptedValue)
    : Exception($"Invalid encrypted secret '{encryptedValue}' - expected 'enc:v1:<base64(nonce || ciphertext || tag)>'");
