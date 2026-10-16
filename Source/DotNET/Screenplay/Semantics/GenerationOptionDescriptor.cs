// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes an option's syntax and constraints. All conditions must hold.
/// </summary>
/// <param name="Name">The source option name.</param>
/// <param name="Placement">Where the value appears.</param>
/// <param name="Kind">The source value kind.</param>
/// <param name="Required">Whether the value must be supplied.</param>
/// <param name="Minimum">The inclusive numeric, text-length or final emitted pattern-length minimum, when bounded.</param>
/// <param name="Maximum">The inclusive numeric, text-length or final emitted pattern-length maximum, when bounded.</param>
/// <param name="Choices">The accepted keywords, or an empty array.</param>
/// <param name="Default">The decoded effective default as text, or null when there is no unconditional default.</param>
/// <param name="Charset">The character repertoire, such as printableAscii, or null.</param>
/// <param name="Distinct">Whether all text characters must be distinct.</param>
/// <param name="MatchPattern">An additional portable match pattern, or null.</param>
/// <param name="Conditions">Conditions on the primitive or another option.</param>
/// <param name="DiagnosticCode">The diagnostic for a violated value constraint.</param>
public sealed record GenerationOptionDescriptor(
    string Name,
    GenerationOptionPlacement Placement,
    GenerationOptionKind Kind,
    bool Required,
    long? Minimum,
    long? Maximum,
    ImmutableArray<string> Choices,
    string? Default,
    string? Charset,
    bool Distinct,
    string? MatchPattern,
    ImmutableArray<GenerationOptionCondition> Conditions,
    string DiagnosticCode);
