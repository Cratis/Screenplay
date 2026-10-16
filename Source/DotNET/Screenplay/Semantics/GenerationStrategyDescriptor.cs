// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes one generation strategy without executing or admitting a declaration.
/// </summary>
/// <param name="Name">The strategy's source spelling.</param>
/// <param name="EsmKind">The serialized kind, or null for a reserved strategy.</param>
/// <param name="Primitives">Compatible source primitive names; empty for a reserved spelling.</param>
/// <param name="IntroducedIn">The first admitting ESM version, or null when reserved.</param>
/// <param name="Status">The admission status.</param>
/// <param name="Executability">The reference evaluator's capabilities.</param>
/// <param name="FixedBits">Fixed random bits, or null when dependent on options or unknown.</param>
/// <param name="Options">The positional arguments, modifiers and body contract.</param>
public sealed record GenerationStrategyDescriptor(
    string Name,
    string? EsmKind,
    ImmutableArray<string> Primitives,
    SemanticVersion? IntroducedIn,
    GenerationStrategyStatus Status,
    GenerationExecutability Executability,
    int? FixedBits,
    ImmutableArray<GenerationOptionDescriptor> Options);
