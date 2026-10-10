// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the closed vocabulary of reaction authorization identities.
/// </summary>
public enum SemanticReactionIdentityKind
{
    /// <summary>
    /// An authenticated system principal with exactly the declared roles and no portable claims.
    /// </summary>
    System = 0
}
