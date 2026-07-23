// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// The exception that is thrown when decrypting a secret fails - the value was tampered with or the
/// key is not the one it was encrypted with.
/// </summary>
/// <param name="innerException">The underlying cryptographic exception.</param>
public class SecretDecryptionFailed(Exception innerException)
    : Exception("Decrypting the secret failed - the value was tampered with or the key is wrong", innerException);
