// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// The exception that is thrown when a canonical semantic model cannot be normalised.
/// </summary>
/// <param name="path">The JSON path of the value that could not be normalised.</param>
/// <param name="reason">Why the value could not be normalised.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Source-only test support compiles into the consumer; none of its types are public APIs.")]
internal sealed class SemanticNormalizationFailed(string path, string reason) : Exception($"Cannot normalise '{path}': {reason}")
{
    /// <summary>
    /// Gets the JSON path of the value that could not be normalised.
    /// </summary>
    public string Path { get; } = path;
}
