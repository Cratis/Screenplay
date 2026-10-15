// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines an explicitly selected portable reaction production boundary.
/// </summary>
public enum SemanticReactionProductionMode
{
    /// <summary>
    /// Direct productions and each selected refusal branch append as independent atomic batches.
    /// </summary>
    Atomic
}
