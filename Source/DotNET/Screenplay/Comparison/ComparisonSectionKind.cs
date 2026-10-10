// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines ComparisonSectionKind values for structural model comparison.
/// </summary>
public enum ComparisonSectionKind
{
    /// <summary>
    /// The declarations section.
    /// </summary>
    Declarations,

    /// <summary>
    /// The events section.
    /// </summary>
    Events,

    /// <summary>
    /// The members section.
    /// </summary>
    Members,

    /// <summary>
    /// The specifications section.
    /// </summary>
    Specifications,

    /// <summary>
    /// The dependants section.
    /// </summary>
    Dependants,

    /// <summary>
    /// The identities section.
    /// </summary>
    Identities
}
