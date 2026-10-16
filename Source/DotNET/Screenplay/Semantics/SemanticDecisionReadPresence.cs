// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the presence contract of a protected decision read.
/// </summary>
public enum SemanticDecisionReadPresence
{
    /// <summary>
    /// Absence rejects the command before derived values, requirements and productions.
    /// </summary>
    Required
}
