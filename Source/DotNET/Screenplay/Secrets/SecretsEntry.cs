// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// Represents a single secret in a <see cref="SecretsFile"/>.
/// </summary>
/// <param name="Name">The name of the secret.</param>
/// <param name="EncryptedValue">The encrypted value in the <c>enc:v1:&lt;base64&gt;</c> format.</param>
public record SecretsEntry(string Name, string EncryptedValue);
