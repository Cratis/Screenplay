// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The exception that is thrown when a semantic contract is malformed or internally inconsistent.
/// </summary>
/// <param name="message">The message describing the invalid contract.</param>
public class InvalidSemanticContract(string message) : Exception(message);
