// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

internal static class EnumerationPinning
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application)
    {
        var walker = new StoredTypes(application);
        walker.VisitApplication(application);
        foreach (var concept in application.Concepts.Where(concept => concept.IsEnum && concept.ValueNumbers is null && walker.Names.Contains(concept.Name)))
        {
            yield return new(DiagnosticSeverity.Information, DiagnosticCodes.UnpinnedStoredEnumeration, $"Enum '{concept.Name}' is used by an event property; use 'Number every member by its current position' to pin stored member identity before reordering or inserting members", concept.Location);
        }
    }

    sealed class StoredTypes(ApplicationSyntax application) : ScreenplaySyntaxWalker
    {
        internal HashSet<string> Names { get; } = new(StringComparer.Ordinal);

        public override void VisitEvent(EventSyntax syntax)
        {
            foreach (var property in syntax.Properties) Add(property.Type.Name);
            base.VisitEvent(syntax);
        }

        void Add(string name)
        {
            if (!Names.Add(name)) return;
            foreach (var type in application.Types ?? [])
            {
                if (type.Name != name) continue;
                foreach (var property in type.Properties) Add(property.Type.Name);
            }
        }
    }
}
