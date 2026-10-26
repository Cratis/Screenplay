// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents how one array of the canonical semantic model is compared.
/// </summary>
/// <param name="Kind">The array kind.</param>
/// <param name="KeyProperty">The element property holding the name or reference that keys a name or reference set.</param>
internal readonly record struct SemanticArrayRule(SemanticArrayKind Kind, string? KeyProperty = null);
