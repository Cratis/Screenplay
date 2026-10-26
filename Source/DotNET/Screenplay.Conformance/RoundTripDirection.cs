// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Describes which half of a round trip reported a diagnostic.
/// </summary>
internal enum RoundTripDirection
{
    /// <summary>
    /// Rendering the model into the target.
    /// </summary>
    Render = 0,

    /// <summary>
    /// Recovering the model from the target through its adapter.
    /// </summary>
    Adapter = 1,
}
