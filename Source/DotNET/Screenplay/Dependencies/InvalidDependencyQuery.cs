// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// The exception that is thrown when a dependency query uses an unsupported level, kind or direction.
/// </summary>
/// <param name="message">The invalid selection.</param>
[SuppressMessage("Design", "CA1064", Justification = "Thrown only by the internal dependency graph, not by the public package API.")]
internal sealed class InvalidDependencyQuery(string message) : Exception(message);
