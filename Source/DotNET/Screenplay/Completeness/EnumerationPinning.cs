// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

internal static class EnumerationPinning
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application)
    {
        var stored = StoredTypeNames.For(application);
        foreach (var concept in application.Concepts.Where(concept => concept.IsEnum && concept.ValueNumbers is null && stored.Contains(concept.Name)))
        {
            yield return new(DiagnosticSeverity.Information, DiagnosticCodes.UnpinnedStoredEnumeration, $"Enum '{concept.Name}' is used by an event property; use 'Number every member by its current position' to pin stored member identity before reordering or inserting members", concept.Location);
        }
    }
}
