// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleDependencies(AdmissibleSyntax units, IEnumerable<ImportSyntax> imports)
{
    readonly Dictionary<SemanticExclusionReference, HashSet<SemanticExclusionReference>> _edges = [];

    internal static AdmissibleDependencies For(ApplicationSyntax syntax, SemanticIdentityCatalog identities)
    {
        var units = new AdmissibleSyntax(syntax, identities);
        units.Index();
        var authoring = new AuthoringIndex();
        authoring.VisitApplication(syntax);
        authoring.Complete(syntax);
        var dependencies = new AdmissibleDependencies(units, syntax.Imports);
        foreach (var resolution in authoring.ResolvedReferences) dependencies.Index(resolution);
        return dependencies;
    }

    internal IReadOnlySet<SemanticExclusionReference> Of(SemanticExclusionReference consumer) => _edges.GetValueOrDefault(consumer, []);

    static bool Contains(AdmissibleSyntax.Unit outer, AdmissibleSyntax.Unit inner) =>
        outer == inner || (outer.Syntax is SliceSyntax slice && inner.Syntax is SpecificationSyntax specification && slice.Specifications.Contains(specification));

    static SemanticExclusionReference Reference(AdmissibleSyntax.Unit unit) => new(unit.Kind, unit.Address);

    void Index(ReferenceResolution resolution)
    {
        var consumers = units.UnitsAt(resolution.Reference.Location);
        if (consumers.Count == 0) return;
        var producers = Producers(resolution).ToArray();
        foreach (var consumer in consumers)
        {
            foreach (var producer in producers) Add(consumer, producer);
        }
    }

    IEnumerable<AdmissibleSyntax.Unit> Producers(ReferenceResolution resolution)
    {
        if (resolution.Candidates.Length == 0)
        {
            return imports.Where(import => string.Equals(import.Name, resolution.Reference.Name, StringComparison.Ordinal) ||
                string.Equals(import.QualifiedName, resolution.Reference.Name, StringComparison.Ordinal)).Select(units.Find).OfType<AdmissibleSyntax.Unit>();
        }
        return resolution.Candidates.SelectMany(ProducerOwners);
    }

    IEnumerable<AdmissibleSyntax.Unit> ProducerOwners(AuthoredDeclaration candidate) =>
        units.Find(candidate.Syntax) is { } unit ? [unit] : units.UnitsAt(candidate.Location);

    void Add(AdmissibleSyntax.Unit consumer, AdmissibleSyntax.Unit producer)
    {
        if (Contains(consumer, producer) || Contains(producer, consumer)) return;
        var from = Reference(consumer);
        if (!_edges.TryGetValue(from, out var targets)) _edges[from] = targets = [];
        targets.Add(Reference(producer));
    }
}
