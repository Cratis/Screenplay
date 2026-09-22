// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// The exception that is thrown when JSON does not describe a supported, well-typed syntax tree.
/// </summary>
/// <param name="message">The reason the syntax cannot be represented.</param>
public class InvalidSyntaxJson(string message) : Exception(message);
