// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines EventContractChangeKind values for structural model comparison.
/// </summary>
public enum EventContractChangeKind
{
    /// <summary>
    /// An event property is added.
    /// </summary>
    PropertyAdded,

    /// <summary>
    /// An event property is removed.
    /// </summary>
    PropertyRemoved,

    /// <summary>
    /// An event property type changes.
    /// </summary>
    PropertyTypeChanged,

    /// <summary>
    /// An event generation is added.
    /// </summary>
    GenerationAdded,

    /// <summary>
    /// An event generation is removed.
    /// </summary>
    GenerationRemoved
}
