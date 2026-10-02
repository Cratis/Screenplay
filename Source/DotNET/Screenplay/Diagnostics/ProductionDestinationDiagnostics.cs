// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Diagnostics;

internal static class ProductionDestinationDiagnostics
{
    internal static IEnumerable<Diagnostic> In(ApplicationSyntax application) => application.Modules
        .SelectMany(module => module.Features.SelectMany(Features))
        .SelectMany(feature => feature.Slices)
        .SelectMany(slice => slice.Commands)
        .SelectMany(In);

    static IEnumerable<FeatureSyntax> Features(FeatureSyntax feature) => new[] { feature }
        .Concat(feature.Features.SelectMany(Features));

    static IEnumerable<Diagnostic> In(CommandSyntax command)
    {
        var identifiers = command.Properties.Where(property => property.IsIdentifier && !property.Type.IsOptional && !property.Type.IsCollection).ToArray();
        if (identifiers.Length != 1)
        {
            return [];
        }

        return command.Produces.Where(produces => produces.When is null && produces.For is null)
            .Select(produces => new Diagnostic(
                DiagnosticSeverity.Information,
                DiagnosticCodes.OmittedProductionDestination,
                $"Plain 'produces {produces.Event}' omits its destination - use 'for {identifiers[0].Name}' to explicitly select the command's identifier.",
                produces.Location));
    }
}
