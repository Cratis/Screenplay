// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Represents the name of a migration step as it reads in a report - <c>slots-and-outlets</c>, for example.
/// </summary>
/// <param name="Value">The name.</param>
public sealed record MigrationName(string Value)
{
    /// <summary>
    /// Converts a string to a <see cref="MigrationName"/>.
    /// </summary>
    /// <param name="value">The name.</param>
    public static implicit operator MigrationName(string value) => new(value);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
