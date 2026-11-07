// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Defines one step that moves a document from one Screenplay version to the next. A step is found by convention: any
/// class in the compiler that implements this interface and has a public parameterless constructor.
/// </summary>
/// <remarks>
/// A step rewrites the syntax tree, never the text. It returns the rewritten tree and, for every change, the source
/// lines it replaces and the nodes that replace them; the runner prints those nodes with the printer into the original
/// text, so every line the step did not touch - comments and layout included - stays as it was.
/// </remarks>
public interface IScreenplayMigration
{
    /// <summary>
    /// Gets the version the step reads.
    /// </summary>
    ScreenplayVersion From { get; }

    /// <summary>
    /// Gets the version the step writes.
    /// </summary>
    ScreenplayVersion To { get; }

    /// <summary>
    /// Gets the name of the step, as it reads in a report.
    /// </summary>
    MigrationName Name { get; }

    /// <summary>
    /// Rewrites one document.
    /// </summary>
    /// <param name="document">The <see cref="DocumentSyntax"/> as the parser read it.</param>
    /// <returns>The <see cref="MigrationResult"/>.</returns>
    MigrationResult Apply(DocumentSyntax document);
}
