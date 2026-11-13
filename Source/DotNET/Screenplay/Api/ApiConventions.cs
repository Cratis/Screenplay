// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Api;

/// <summary>
/// Describes how a model's commands and queries become HTTP routes.
/// </summary>
/// <remarks>
/// A route is the prefix followed by the kebab-cased module, feature and slice names of the declaring slice.
/// A query route always ends with the query name. A command route ends with the command name only when
/// <see cref="IncludeCommandNameInRoute"/> is set or when its slice declares more than one command.
/// </remarks>
/// <param name="RoutePrefix">The first route segment, without slashes.</param>
/// <param name="IncludeCommandNameInRoute">Whether every command route ends with the command name.</param>
public sealed record ApiConventions(string RoutePrefix, bool IncludeCommandNameInRoute)
{
    /// <summary>
    /// Gets the conventions of an application rendered by the Cratis renderer: routes under <c>/api</c>, and a
    /// command named in its route only when its slice declares more than one command.
    /// </summary>
    public static ApiConventions Cratis { get; } = new("api", false);
}
