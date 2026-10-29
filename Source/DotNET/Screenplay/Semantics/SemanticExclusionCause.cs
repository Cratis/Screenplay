// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes whether a unit failed itself or was excluded through a dependency.
/// </summary>
public enum SemanticExclusionCause
{
    /// <summary>The unit has its own error or no evidenced excluded dependency.</summary>
    Root,

    /// <summary>The unit was excluded because an immediate dependency was excluded.</summary>
    Dependent
}
