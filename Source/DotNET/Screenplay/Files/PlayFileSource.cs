// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents a <c>.play</c> file together with the source text that was read from it.
/// </summary>
/// <param name="File">The <see cref="PlayFile"/> the source was read from.</param>
/// <param name="Source">The source text of the file.</param>
/// <remarks>
/// A folder compiles to one application, so there is one result rather than one per file. The sources still
/// matter afterwards: a diagnostic names its file through <see cref="Diagnostics.SourceLocation.Path"/>, and
/// rendering it with its offending line needs the text that line came from.
/// </remarks>
public record PlayFileSource(PlayFile File, string Source);
