// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes how generated fixtures can be checked by the reference evaluator.
/// </summary>
public enum GenerationExecutability
{
    /// <summary>
    /// No executable contract is available.
    /// </summary>
    None,

    /// <summary>
    /// The reference evaluator checks the built-in strategy's fixture shape.
    /// </summary>
    Reference,

    /// <summary>
    /// A target implementation is required; the reference evaluator never executes its body.
    /// </summary>
    RequiresImplementation
}
