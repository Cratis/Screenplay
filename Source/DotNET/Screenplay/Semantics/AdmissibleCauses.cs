// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleCauses
{
    readonly AdmissibleSyntax _owners;
    readonly AdmissibleSecurity _security;
    readonly Dictionary<SemanticExclusionReference, HashSet<SemanticExclusionReference>> _dependencies = [];
    readonly HashSet<SemanticExclusionReference> _roots = [];
    bool _observed;

    internal AdmissibleCauses(ApplicationSyntax syntax, SemanticIdentityCatalog identities)
    {
        _owners = new(syntax, identities);
        _owners.Index();
        _security = new(syntax, _owners);
        var index = new AuthoringIndex();
        index.VisitApplication(syntax);
        index.Complete(syntax);
        foreach (var resolution in index.ResolvedReferences.Where(value => value.Candidates.Length == 1))
        {
            var source = _owners.Find(resolution.Reference.Location);
            var target = _owners.Find(resolution.Candidates[0].Location);
            if (source is not null && target is not null) Link(source, target);
        }
    }

    internal void Observe(IEnumerable<Diagnostic> errors)
    {
        if (_observed) return;
        _observed = true;
        _roots.UnionWith(_security.RootOwners(errors).Select(Reference));
    }

    internal void Link(AdmissibleSyntax.Unit source, AdmissibleSyntax.Unit target)
    {
        var from = Reference(source);
        var to = Reference(target);
        if (from == to) return;
        if (!_dependencies.TryGetValue(from, out var targets)) _dependencies[from] = targets = [];
        targets.Add(to);
    }

    internal SemanticExclusion Classify(SemanticExclusion exclusion, IEnumerable<SemanticExclusion> excluded)
    {
        var reference = new SemanticExclusionReference(exclusion.Kind, exclusion.Address);
        if (_roots.Contains(reference)) return exclusion;
        var removed = excluded.Select(value => new SemanticExclusionReference(value.Kind, value.Address)).ToHashSet();
        var dependencies = _dependencies.GetValueOrDefault(reference, [])
            .Where(removed.Contains).OrderBy(value => value.Kind).ThenBy(value => value.Address, StringComparer.Ordinal).ToImmutableArray();
        return dependencies.IsEmpty ? exclusion : exclusion with { Cause = SemanticExclusionCause.Dependent, DependsOn = dependencies };
    }

    static SemanticExclusionReference Reference(AdmissibleSyntax.Unit unit) => new(unit.Kind, unit.Address);
}
