// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents an implementation of <see cref="IPlayFileCompiler"/>.
/// </summary>
/// <param name="playFiles">The <see cref="IPlayFiles"/> used to discover and read files.</param>
/// <param name="compiler">The <see cref="IScreenplayCompiler"/> used to compile each file.</param>
/// <remarks>
/// Folder compilation shares <see cref="PlayFolderMerge"/> with workspace semantic compilation. The legacy
/// result still returns the full syntax tree, original source strings, and diagnostics even for failed input;
/// a workspace instead admits portable UTF-8 documents and derives an executable semantic projection. Opening
/// a workspace here would reject valid legacy paths/encodings and cannot reconstruct the original syntax result.
/// </remarks>
public class PlayFileCompiler(IPlayFiles playFiles, IScreenplayCompiler compiler) : IPlayFileCompiler
{
    readonly IScreenplayLanguageRegistry _languages = (compiler as ILanguageRegistryOwner)?.Languages ?? ScreenplayLanguageRegistry.Default;

    /// <summary>
    /// Initializes a file compiler with an explicit registry shared by discovery and the supplied parser.
    /// </summary>
    /// <param name="playFiles">The source used to discover and read files.</param>
    /// <param name="compiler">The compiler configured with <paramref name="languages"/>.</param>
    /// <param name="languages">The caller's inline language registry.</param>
    public PlayFileCompiler(IPlayFiles playFiles, IScreenplayCompiler compiler, IScreenplayLanguageRegistry languages)
        : this(playFiles, compiler)
    {
        _languages = languages;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayFileCompiler"/> class with default collaborators.
    /// </summary>
    public PlayFileCompiler()
        : this(new PlayFiles(), new ScreenplayCompiler())
    {
    }

    internal AuthoredTimeline Timeline { get; private set; } = null!;

    /// <inheritdoc/>
    public IEnumerable<PlayFileCompilation> CompileIn(string root) =>
        [.. playFiles.FindIn(root).Select(Compile)];

    /// <inheritdoc/>
    public PlayFileCompilation CompileFile(string path) => Compile(Locate(path));

    /// <inheritdoc/>
    public ApplicationCompilation<ApplicationSyntax> CompileFolder(string root)
    {
        // Every file in the folder is a root, so a file nobody imports is still a whole document of the
        // application - and one an import places in a module or feature is placed there.
        var source = new DiskPlayDocumentSource(playFiles, root);
        return Assemble(source, source.FilesBeneath(string.Empty));
    }

    /// <inheritdoc/>
    public ApplicationCompilation<TApplication> CompileFolder<TApplication>(string root, IApplicationSyntaxVisitor<TApplication> visitor) =>
        Visit(CompileFolder(root), visitor);

    /// <inheritdoc/>
    public ApplicationCompilation<TApplication> CompileFile<TApplication>(string path, IApplicationSyntaxVisitor<TApplication> visitor) =>
        Visit(CompileApplication(path), visitor);

    /// <inheritdoc/>
    public ApplicationCompilation<ApplicationSyntax> CompileApplication(string path)
    {
        var file = Locate(path);
        var source = new DiskPlayDocumentSource(playFiles, System.IO.Path.GetDirectoryName(file.Path)!);
        source.Add(file);
        return Assemble(source, [file.RelativePath]);
    }

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

    ApplicationCompilation<ApplicationSyntax> Assemble(DiskPlayDocumentSource source, IEnumerable<string> roots)
    {
        var (documents, result) = PlayApplicationAssembly.Compile(compiler, roots, source, _languages, out var timeline);
        Timeline = timeline;
        return new([.. documents.Select(document => new PlayFileSource(source.File(document.Path), document.Source))], result);
    }

    PlayFileCompilation Compile(PlayFile file)
    {
        var source = playFiles.ReadContent(file);
        return new(file, source, compiler.Compile(source));
    }
}
