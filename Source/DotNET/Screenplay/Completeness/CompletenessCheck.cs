// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness;

/// <summary>
/// Identifies an opt-in structural completeness check.
/// </summary>
public enum CompletenessCheck
{
    /// <summary>
    /// Checks screen data bindings.
    /// </summary>
    DataBindings,

    /// <summary>
    /// Checks command and action input surfaces.
    /// </summary>
    InputSurfaces,

    /// <summary>
    /// Checks read-model field origins.
    /// </summary>
    FieldOrigins,

    /// <summary>
    /// Checks query parameters against view shapes and keys.
    /// </summary>
    QueryKeys,

    /// <summary>
    /// Checks event consumers.
    /// </summary>
    EventConsumers,

    /// <summary>
    /// Checks screen reachability.
    /// </summary>
    Navigation,

    /// <summary>
    /// Checks persona reachability and deterministic caller ambiguity.
    /// </summary>
    Personas
}
