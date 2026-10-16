// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes executable-model admission of a generation strategy.
/// </summary>
public enum GenerationStrategyStatus
{
    /// <summary>
    /// Admission has not been established.
    /// </summary>
    Unknown,

    /// <summary>
    /// The strategy is admitted in its declared version.
    /// </summary>
    Admitted,

    /// <summary>
    /// The spelling is reserved and binding refuses it.
    /// </summary>
    AwaitingAdmission
}
