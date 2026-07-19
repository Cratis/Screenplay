// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents an implementation of <see cref="IPlayFileCompiler"/>.
/// </summary>
/// <param name="playFiles">The <see cref="IPlayFiles"/> used to discover and read files.</param>
/// <param name="compiler">The <see cref="IScreenplayCompiler"/> used to compile each file.</param>
public class PlayFileCompiler(IPlayFiles playFiles, IScreenplayCompiler compiler) : IPlayFileCompiler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayFileCompiler"/> class with default collaborators.
    /// </summary>
    public PlayFileCompiler()
        : this(new PlayFiles(), new ScreenplayCompiler())
    {
    }

    /// <inheritdoc/>
    public IEnumerable<PlayFileCompilation> CompileIn(string root) =>
        [.. playFiles.FindIn(root)
            .Select(file =>
            {
                var source = playFiles.ReadContent(file);
                return new PlayFileCompilation(file, source, compiler.Compile(source));
            })];
}
