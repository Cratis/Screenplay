// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly (Declaration Declaration, QuerySyntax Query)[] _scopedQueries = [.. ScreenplayValidator.ScopedSlices(syntax)
            .SelectMany(entry => entry.Slice.Queries.Select(query => (new Declaration(query.Name, entry.Scope), query)))];
        ReferenceDeclarationIndex? _queryReferenceIndex;
        readonly HashSet<QuerySyntax> _opaqueQueries = new(ReferenceEqualityComparer.Instance);

        bool ReachesOpaqueQuery(SemanticAddress slice, SpecificationQuerySyntax reference)
        {
            var scope = new DeclarationScope([.. slice.Parts
                .Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice)
                .Select(part => part.Key)]);
            _queryReferenceIndex ??= new(_scopedQueries.Select(entry => entry.Declaration));
            var resolution = ReferenceResolver.Resolve(reference.Query, scope, _queryReferenceIndex);
            return resolution.Resolved is { } resolved &&
                _opaqueQueries.Contains(_scopedQueries.Single(entry => ReferenceEquals(entry.Declaration, resolved)).Query);
        }

        SemanticKeyedQuery? ResolveQuery(SemanticAddress slice, SpecificationQuerySyntax reference)
        {
            var scope = new DeclarationScope([.. slice.Parts
                .Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice)
                .Select(part => part.Key)]);
            _queryReferenceIndex ??= new(_scopedQueries.Select(entry => entry.Declaration));
            var resolution = ReferenceResolver.Resolve(reference.Query, scope, _queryReferenceIndex);
            if (resolution.Resolved is { } resolved)
            {
                var query = _scopedQueries.Single(entry => ReferenceEquals(entry.Declaration, resolved)).Query;
                if (_queryDeclarations.TryGetValue(query, out var bound)) return bound;
            }

            if (resolution.Ambiguous.Count > 0)
            {
                var where = string.Join(", ", resolution.Ambiguous.Select(candidate => string.Join('.', candidate.Scope.Segments)));
                Error(DiagnosticCodes.AmbiguousReference, $"Ambiguous query '{reference.Query}' - it matches {resolution.Ambiguous.Count} declarations equally well ({where}); qualify it to say which", reference.Location);
            }
            else
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification query '{reference.Query}' is unresolved.", reference.Location);
            }

            return null;
        }
    }
}
