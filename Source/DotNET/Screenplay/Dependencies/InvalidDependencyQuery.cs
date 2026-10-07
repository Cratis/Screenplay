// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// The exception that is thrown when a dependency query uses an unsupported level, kind or direction.
/// </summary>
/// <param name="message">The invalid selection.</param>
public sealed class InvalidDependencyQuery(string message) : Exception(message);
