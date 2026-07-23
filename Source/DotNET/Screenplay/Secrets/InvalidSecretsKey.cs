// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// The exception that is thrown when the key supplied to a secrets cipher does not have the expected size.
/// </summary>
/// <param name="actualLength">The length in bytes of the supplied key.</param>
/// <param name="expectedLength">The expected length in bytes.</param>
public class InvalidSecretsKey(int actualLength, int expectedLength)
    : Exception($"Invalid secrets key of {actualLength} bytes - expected {expectedLength} bytes");
