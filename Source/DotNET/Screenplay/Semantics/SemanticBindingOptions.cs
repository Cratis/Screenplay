// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>Defines explicitly requested, optional semantic metadata. Defaults preserve historical binding.</summary>
/// <param name="PreserveReadModelPropertyOrder">Whether to preserve read-model declaration order on a valid ESM 37.0 base.</param>
public sealed record SemanticBindingOptions(bool PreserveReadModelPropertyOrder = false);
