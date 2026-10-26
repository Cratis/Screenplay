// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents one difference between two normalised semantic models.
/// </summary>
/// <param name="Path">The normalised path, such as <c language="text">/application/modules[Banking]/features[Deposits]/slices[Deposit]</c>.</param>
/// <param name="Kind">The kind of difference.</param>
/// <param name="Expected">The compact canonical text of the expected value, or <see langword="null"/> when it is missing.</param>
/// <param name="Actual">The compact canonical text of the actual value, or <see langword="null"/> when it is missing.</param>
internal sealed record SemanticDifference(string Path, SemanticDifferenceKind Kind, string? Expected, string? Actual);
