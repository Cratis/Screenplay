// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Lets native parser decorators retain physical trees without losing the immutable whole-input inventory.
/// </summary>
internal interface ICommandStreamCandidateParser
{
    /// <summary>Captures declarations using this compiler's actual language registry.</summary>
    /// <param name="documents">The immutable documents and their physical placements.</param>
    /// <returns>The shared noncommitting declaration inventory.</returns>
    CommandStreamCandidates CaptureCandidates(IEnumerable<(IReadOnlyList<SourceLine> Lines, PlayPlacement Placement)> documents);

    /// <summary>Parses one physical document using the application's declaration candidates.</summary>
    /// <param name="source">The immutable source text.</param>
    /// <param name="path">The physical path.</param>
    /// <param name="placement">The resolved physical placement.</param>
    /// <param name="candidates">The shared declaration inventory.</param>
    /// <returns>The physical authoring syntax and diagnostics.</returns>
    CompilationResult<ApplicationSyntax> ParseWithCandidates(string source, string? path, PlayPlacement placement, CommandStreamCandidates candidates);
}
