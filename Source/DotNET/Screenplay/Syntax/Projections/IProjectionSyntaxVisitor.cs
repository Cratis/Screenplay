// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Defines a visitor that turns a <see cref="ProjectionSyntax"/> into a consumer specific projection representation.
/// </summary>
/// <typeparam name="TProjection">The type the visitor produces.</typeparam>
public interface IProjectionSyntaxVisitor<out TProjection>
{
    /// <summary>
    /// Visits a <see cref="ProjectionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ProjectionSyntax"/> to visit.</param>
    /// <returns>The consumer specific projection representation.</returns>
    TProjection Visit(ProjectionSyntax syntax);
}
