// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation;

/// <summary>
/// Identifies the winning setting's origin.
/// </summary>
public enum ValidationGateSource
{
    /// <summary>
    /// Explicit command-line input.
    /// </summary>
    CommandLine,

    /// <summary>
    /// The authored gate file.
    /// </summary>
    File,

    /// <summary>
    /// Today's tool default.
    /// </summary>
    Default
}

/// <summary>
/// One resolved gate setting with its origin.
/// </summary>
/// <typeparam name="T">The setting's value type.</typeparam>
/// <param name="Value">The effective value.</param>
/// <param name="Source">The winning origin.</param>
public sealed record ValidationGateValue<T>(T Value, ValidationGateSource Source);
