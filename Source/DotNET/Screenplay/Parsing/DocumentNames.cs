// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The names a document makes available for the cross reference checks of the <see cref="ScreenplayValidator"/>.
/// </summary>
/// <remarks>
/// The events and policies are collected up front; the read models, types and commands are collected the first
/// time they are read, so each set is enumerated at the point in the validation sequence that always collected it.
/// </remarks>
internal sealed class DocumentNames
{
    readonly Lazy<HashSet<string>> _readModels;
    readonly Lazy<HashSet<string>> _types;
    readonly Lazy<HashSet<string>> _commands;

    DocumentNames(HashSet<string> events, HashSet<string> policies, ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices)
    {
        Events = events;
        Policies = policies;

        // A read model is whatever a builder names with '=>', plus anything declared on its own. A
        // projection with variants produces no read model of its own name - each variant is its own,
        // named by the variant instead.
        _readModels = new(() => [.. slices.SelectMany(slice => slice.Projections)
            .SelectMany(ScreenplayValidator.ProjectionBuiltReadModelNames)
            .Concat(slices.SelectMany(slice => slice.Reducers ?? []).Select(reducer => reducer.ReadModel))
            .Concat(slices.SelectMany(slice => slice.ReadModels ?? []).Select(readModel => readModel.Name))
            .Concat(application.Imports.Select(import => import.Name))]);
        _types = new(() => [.. ConceptSyntax.PrimitiveTypes
            .Concat(application.Concepts.Select(concept => concept.Name))
            .Concat((application.Types ?? []).Select(type => type.Name))
            .Concat(application.Imports.Select(import => import.Name))]);
        _commands = new(() => [.. slices.SelectMany(slice => slice.Commands.Select(command => command.Name))
            .Concat(application.Imports.Select(import => import.Name))]);
    }

    /// <summary>
    /// Gets the events the document declares or imports.
    /// </summary>
    public HashSet<string> Events { get; }

    /// <summary>
    /// Gets the policies the document declares.
    /// </summary>
    public HashSet<string> Policies { get; }

    /// <summary>
    /// Gets the read models the document makes available.
    /// </summary>
    public HashSet<string> ReadModels => _readModels.Value;

    /// <summary>
    /// Gets the primitives, concepts, types and imports the document makes available.
    /// </summary>
    public HashSet<string> Types => _types.Value;

    /// <summary>
    /// Gets the commands the document declares or imports.
    /// </summary>
    public HashSet<string> Commands => _commands.Value;

    /// <summary>
    /// Starts collecting the names a document makes available.
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
        return new(events, policies, application, slices);
    }
}
