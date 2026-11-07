// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations.given;

/// <summary>
/// A step that changes nothing, for planning specs.
/// </summary>
/// <param name="from">The version it reads.</param>
/// <param name="to">The version it writes.</param>
/// <param name="name">Its name.</param>
public class a_step(int from, int to, string name) : IScreenplayMigration
{
    /// <inheritdoc/>
    public ScreenplayVersion From => from;

    /// <inheritdoc/>
    public ScreenplayVersion To => to;

    /// <inheritdoc/>
    public MigrationName Name => name;

    /// <inheritdoc/>
    public MigrationResult Apply(DocumentSyntax document) => MigrationResult.Unchanged(document);
}
