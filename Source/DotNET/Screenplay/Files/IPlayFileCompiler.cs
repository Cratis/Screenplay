// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Defines a system that compiles <c>.play</c> files - a single file, every file beneath a folder
/// independently, or every file beneath a folder as one application.
/// </summary>
public interface IPlayFileCompiler
{
    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath a root folder, each on its own.
    /// </summary>
    /// <param name="root">The root folder to search from.</param>
    /// <returns>A <see cref="PlayFileCompilation"/> per discovered file.</returns>
    /// <remarks>
    /// Every file is compiled as a document in its own right, so a name one file declares and another uses is
    /// unresolved in both. Reach for <see cref="CompileFolder(string)"/> when the files describe one
    /// application between them, which is what a folder of <c>.play</c> files normally is.
    /// </remarks>
    IEnumerable<PlayFileCompilation> CompileIn(string root);

    /// <summary>
    /// Compiles a single <c>.play</c> file.
    /// </summary>
    /// <param name="path">The path of the file to compile.</param>
    /// <returns>The <see cref="PlayFileCompilation"/> of the file.</returns>
    /// <remarks>
    /// The resulting <see cref="PlayFile.RelativePath"/> is the file name, so diagnostics read the same
    /// way they do for a discovered file.
    /// </remarks>
    PlayFileCompilation CompileFile(string path);

    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath a root folder as one application.
    /// </summary>
    /// <param name="root">The root folder to search from.</param>
    /// <returns>The <see cref="ApplicationCompilation{TApplication}"/> of the folder as a whole.</returns>
    /// <remarks>
    /// The files are merged into the one application they describe before anything is resolved, so an event
    /// declared in one file and produced in another resolves, and every diagnostic names the file it came from
    /// through <see cref="Diagnostics.SourceLocation.Path"/>. This is the inverse of
    /// <see cref="IPlayFileWriter.Expand(ApplicationSyntax)"/>.
    /// <para>
    /// A folder holding no <c>.play</c> files is an application that declares nothing, which compiles
    /// successfully. Check whether anything was found through
    /// <see cref="ApplicationCompilation{TApplication}.Sources"/> when that should be an error instead.
    /// </para>
    /// </remarks>
    ApplicationCompilation<ApplicationSyntax> CompileFolder(string root);

    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath a root folder as one application, then drives a
    /// visitor over the resulting syntax tree.
    /// </summary>
    /// <typeparam name="TApplication">The type the visitor produces.</typeparam>
    /// <param name="root">The root folder to search from.</param>
    /// <param name="visitor">The <see cref="IApplicationSyntaxVisitor{TApplication}"/> to drive.</param>
    /// <returns>The <see cref="ApplicationCompilation{TApplication}"/> holding the visitor result and any diagnostics.</returns>
    /// <remarks>
    /// The visitor sees the merged application - one tree, whatever it was spread across - and runs only when
    /// the folder compiled without errors.
    /// </remarks>
    ApplicationCompilation<TApplication> CompileFolder<TApplication>(string root, IApplicationSyntaxVisitor<TApplication> visitor);

    /// <summary>
    /// Compiles a single <c>.play</c> file and drives a visitor over the resulting syntax tree.
    /// </summary>
    /// <typeparam name="TApplication">The type the visitor produces.</typeparam>
    /// <param name="path">The path of the file to compile.</param>
    /// <param name="visitor">The <see cref="IApplicationSyntaxVisitor{TApplication}"/> to drive.</param>
    /// <returns>The <see cref="ApplicationCompilation{TApplication}"/> holding the visitor result and any diagnostics.</returns>
    /// <remarks>
    /// The counterpart of <see cref="CompileFolder{TApplication}(string, IApplicationSyntaxVisitor{TApplication})"/>
    /// for the single file case, so a consumer takes the same path through the same visitor whether an
    /// application arrives as one file or as a folder.
    /// </remarks>
    ApplicationCompilation<TApplication> CompileFile<TApplication>(string path, IApplicationSyntaxVisitor<TApplication> visitor);
}
