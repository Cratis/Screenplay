// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The exception that is thrown when a scope does not name exactly one module, feature or slice of an executable model.
/// </summary>
/// <param name="message">The message describing why the scope was refused.</param>
public class InvalidSemanticModelScope(string message) : Exception(message);
