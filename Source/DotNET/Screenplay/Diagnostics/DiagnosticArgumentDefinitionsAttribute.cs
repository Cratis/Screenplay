// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Declares a code's permanent argument contract for the generated catalog.
/// </summary>
/// <param name="definitions">Entries in name:kind[?][=keyword|keyword];meaning form; keyword sets are required for keyword arguments.</param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class DiagnosticArgumentDefinitionsAttribute(params string[] definitions) : Attribute
{
    /// <summary>
    /// Gets the argument declarations in source order.
    /// </summary>
    public IReadOnlyList<string> Definitions { get; } = definitions;
}
