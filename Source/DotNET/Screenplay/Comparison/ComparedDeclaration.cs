// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Identifies a compared declaration; semantic identity is absent in Address mode.
/// </summary>
/// <param name="Kind">The normalized declaration kind.</param>
/// <param name="BeforeAddress">The dotted baseline address, or null if absent.</param>
/// <param name="AfterAddress">The dotted candidate address, or null if absent.</param>
/// <param name="SemanticId">The persisted identity in Identity mode; otherwise null.</param>
public sealed record ComparedDeclaration(
    string Kind,
    string? BeforeAddress,
    string? AfterAddress,
    SemanticId? SemanticId);
