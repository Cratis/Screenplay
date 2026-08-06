// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Defines a system that expands an application into the folder structure of <c>.play</c> files that
/// describes it.
/// </summary>
/// <remarks>
/// <see cref="Printing.IScreenplayPrinter"/> renders a whole application as one document, which is what you
/// want for a small model or a single file on disk. Past a certain size a single document stops being
/// navigable, and the structure the language already has - modules, features, slices - is the structure a
/// reader wants to browse. This writer lays that structure out as folders, and
/// <see cref="IPlayFileCompiler.CompileFolder(string)"/> reads it back as one application.
/// </remarks>
public interface IPlayFileWriter
{
    /// <summary>
    /// Expands an application into the <c>.play</c> files of a folder structure, without touching the file system.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to expand.</param>
    /// <returns>The <see cref="PlayFileContent">files</see> of the structure.</returns>
    /// <exception cref="AmbiguousPlayFilePath">Thrown when two declarations at the same level would claim the same file.</exception>
    IEnumerable<PlayFileContent> Expand(ApplicationSyntax application);

    /// <summary>
    /// Expands an application into a folder structure and writes it beneath a root folder.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to expand.</param>
    /// <param name="root">The root folder to write beneath, created along with every folder it needs.</param>
    /// <returns>The <see cref="PlayFile">files</see> that were written.</returns>
    /// <exception cref="AmbiguousPlayFilePath">Thrown when two declarations at the same level would claim the same file.</exception>
    /// <remarks>
    /// Files the structure names are overwritten; anything else already beneath the root is left alone, so a
    /// removed slice leaves its file behind. Clear the folder first when you need the structure to be exactly
    /// what the application says.
    /// </remarks>
    IEnumerable<PlayFile> WriteTo(ApplicationSyntax application, string root);
}
