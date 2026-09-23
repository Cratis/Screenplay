// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The exception that is thrown when an inline JSON number cannot be represented as a finite value.
/// </summary>
public sealed class InvalidStructuredNumber(string message) : Exception(message);
