// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents the outcome of compiling a single <c>.play</c> file.
/// </summary>
/// <param name="File">The <see cref="PlayFile"/> that was compiled.</param>
/// <param name="Source">The source text of the file.</param>
/// <param name="Result">The <see cref="CompilationResult{TResult}"/> of the compilation.</param>
public record PlayFileCompilation(PlayFile File, string Source, CompilationResult<ApplicationSyntax> Result);
