// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The names a document makes available for the cross reference checks of the <see cref="ScreenplayValidator"/>.
/// </summary>
/// <param name="Events">The events the document declares or imports.</param>
/// <param name="Policies">The policies the document declares.</param>
/// <param name="ReadModels">The read models the document makes available.</param>
/// <param name="Types">The primitives, concepts, types and imports the document makes available.</param>
/// <param name="Commands">The commands the document declares or imports.</param>
internal sealed record DocumentNames(
    HashSet<string> Events,
    HashSet<string> Policies,
    HashSet<string> ReadModels,
    HashSet<string> Types,
    HashSet<string> Commands)
{
    /// <summary>
    /// Collects the names a document makes available.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to read.</param>
    /// <param name="slices">Every <see cref="SliceSyntax"/> in the document.</param>
    /// <returns>The <see cref="DocumentNames"/>.</returns>
    public static DocumentNames Of(ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices)
    {
        var events = slices.SelectMany(slice => EventDeclarations.In(slice).Select(@event => @event.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        var policies = application.Policies.Select(policy => policy.Name).ToHashSet();

        // A read model is whatever a builder names with '=>', plus anything declared on its own. A
        // projection with variants produces no read model of its own name - each variant is its own,
        // named by the variant instead.
        var readModels = slices.SelectMany(slice => slice.Projections)
            .SelectMany(ScreenplayValidator.ProjectionBuiltReadModelNames)
            .Concat(slices.SelectMany(slice => slice.Reducers ?? []).Select(reducer => reducer.ReadModel))
            .Concat(slices.SelectMany(slice => slice.ReadModels ?? []).Select(readModel => readModel.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        var types = ConceptSyntax.PrimitiveTypes
            .Concat(application.Concepts.Select(concept => concept.Name))
            .Concat((application.Types ?? []).Select(type => type.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        var commands = slices.SelectMany(slice => slice.Commands.Select(command => command.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        return new(events, policies, readModels, types, commands);
    }
}
