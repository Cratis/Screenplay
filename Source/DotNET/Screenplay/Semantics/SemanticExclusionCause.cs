// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes whether a unit failed itself or was excluded through a dependency.
/// </summary>
public enum SemanticExclusionCause
{
    /// <summary>
    /// The unit was excluded for its own binder errors, without an earlier excluded dependency.
    /// </summary>
    Root,

    /// <summary>
    /// The unit was excluded because an earlier dependency was excluded or a security closure forced it out.
    /// </summary>
    Dependent
}
