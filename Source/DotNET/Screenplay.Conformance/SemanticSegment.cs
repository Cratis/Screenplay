// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents the path segment of one element of an unordered array.
/// </summary>
/// <param name="Value">The segment, without brackets.</param>
/// <param name="IsFallback">Whether the element had no unique key and needed the <c language="text">~n</c> fallback.</param>
internal readonly record struct SemanticSegment(string Value, bool IsFallback);
