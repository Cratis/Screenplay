// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contracts;

/// <summary>
/// The exception that is thrown when a language or tool fact cannot be classified in the public contract.
/// </summary>
/// <param name="message">The missing or inconsistent fact.</param>
public sealed class InvalidScreenplayContract(string message) : Exception(message);
