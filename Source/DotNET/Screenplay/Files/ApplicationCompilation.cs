// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents the outcome of compiling one or more <c>.play</c> files into a single application.
/// </summary>
/// <typeparam name="TApplication">The type the compilation produces - the syntax tree, or whatever a visitor turned it into.</typeparam>
/// <param name="Sources">The <see cref="PlayFileSource">files</see> that were compiled, in path order.</param>
/// <param name="Result">The <see cref="CompilationResult{TResult}"/> of the application as a whole.</param>
/// <remarks>
/// There is one result rather than one per file, because the files describe one application between them. The
/// sources come along because a diagnostic names its file through
/// <see cref="Diagnostics.SourceLocation.Path"/>, and rendering it with its offending line needs that file's
/// text.
/// </remarks>
public record ApplicationCompilation<TApplication>(
    IEnumerable<PlayFileSource> Sources,
    CompilationResult<TApplication> Result);
