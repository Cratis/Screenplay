// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>An opaque whole-command validation block; its implementation yields zero or more rejection messages.</summary>
/// <param name="RequirementId">The stable identity of the command validation attachment.</param>
public sealed record SemanticCodeValidation(string RequirementId);
