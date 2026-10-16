// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents one caller-supplied part of a single-instance read-model query key.
/// </summary>
/// <param name="Id">The stable query argument identity.</param>
/// <param name="Name">The argument name.</param>
/// <param name="Type">The required scalar argument type.</param>
/// <param name="KeyProperty">The identified read-model property.</param>
public sealed record SemanticQueryKeyPart(SemanticId Id, string Name, SemanticTypeReference Type, SemanticId KeyProperty);
