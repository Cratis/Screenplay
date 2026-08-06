// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

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
        [.. playFiles.FindIn(root).Select(Compile)];

    /// <inheritdoc/>
    public PlayFileCompilation CompileFile(string path) => Compile(Locate(path));

    /// <inheritdoc/>
    public ApplicationCompilation<ApplicationSyntax> CompileFolder(string root) =>
        AsOneApplication([.. playFiles.FindIn(root).Select(Read)]);

    /// <inheritdoc/>
    public ApplicationCompilation<TApplication> CompileFolder<TApplication>(string root, IApplicationSyntaxVisitor<TApplication> visitor) =>
        Visit(CompileFolder(root), visitor);

    /// <inheritdoc/>
    public ApplicationCompilation<TApplication> CompileFile<TApplication>(string path, IApplicationSyntaxVisitor<TApplication> visitor) =>
        Visit(AsOneApplication([Read(Locate(path))]), visitor);

    static ApplicationCompilation<TApplication> Visit<TApplication>(
        ApplicationCompilation<ApplicationSyntax> compilation,
        IApplicationSyntaxVisitor<TApplication> visitor) =>
        new(
            compilation.Sources,
            compilation.Result.Success
                ? new(visitor.Visit(compilation.Result.Value!), compilation.Result.Diagnostics)
                : CompilationResult<TApplication>.Failed(compilation.Result.Diagnostics));

    static PlayFile Locate(string path)
    {
        var full = System.IO.Path.GetFullPath(path);
        return new(full, System.IO.Path.GetFileName(full));
    }

    ApplicationCompilation<ApplicationSyntax> AsOneApplication(IReadOnlyList<PlayFileSource> sources) =>
        new(sources, PlayFolderMerge.Merge([.. sources.Select(source => compiler.Parse(source.Source, source.File.RelativePath))]));

    PlayFileSource Read(PlayFile file) => new(file, playFiles.ReadContent(file));

    PlayFileCompilation Compile(PlayFile file)
    {
        var source = playFiles.ReadContent(file);
        return new(file, source, compiler.Compile(source));
    }
}
