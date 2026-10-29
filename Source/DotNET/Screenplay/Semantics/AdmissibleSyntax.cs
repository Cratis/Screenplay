// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleSyntax(ApplicationSyntax syntax, SemanticIdentityCatalog identities)
{
    readonly List<Unit> _units = [];
    readonly Func<SourceLocation, DiagnosticArgument?> _locate = DiagnosticSubjects.CreateLocator(syntax);
    readonly Func<SourceLocation, SliceSyntax?> _sliceAt = SliceOwners.CreateLocator(syntax);
    readonly Func<SourceLocation, IReadOnlyList<SyntaxNode>> _unitsAt = SliceOwners.CreateUnitsLocator(syntax);

    internal void Index()
    {
        var application = identities.ResolveSemantic(SemanticAddress.ForApplication(identities.Application));
        foreach (var import in syntax.Imports) _units.Add(new(import, SemanticExclusionKind.Import, import.QualifiedName, application));
        foreach (var policy in syntax.Policies) _units.Add(new(policy, SemanticExclusionKind.Policy, policy.Name, application));
        foreach (var module in syntax.Modules)
        {
            foreach (var feature in module.Features) IndexFeature(module.Name, feature, feature.Name);
        }
    }

    internal Unit? Find(SyntaxNode syntax) => _units.Find(unit => unit.Syntax == syntax);

    internal Unit? Find(Diagnostic diagnostic)
    {
        if (diagnostic.Location is not { } location || _locate(location) is not { } owner) return null;
        var kind = owner.Value.GetProperty("kind").GetString();
        var address = string.Join('/', owner.Value.GetProperty("path").EnumerateArray().Select(part => part.GetString()));
        return _units.Find(unit => unit.Kind switch
        {
            SemanticExclusionKind.Specification => kind == "specification" && unit.Address == address,
            SemanticExclusionKind.Slice => kind != "specification" && unit.Syntax == _sliceAt(location),
            SemanticExclusionKind.Import => kind == "import" && unit.Address == address,
            SemanticExclusionKind.Policy => kind == "policy" && unit.Address == address,
            _ => false
        });
    }

    internal IReadOnlyList<Unit> UnitsAt(SourceLocation location) => [.. _unitsAt(location).Select(Find).OfType<Unit>()];

    internal ApplicationSyntax Remove(HashSet<SyntaxNode> removed) => syntax with
    {
        Imports = [.. syntax.Imports.Where(import => !removed.Contains(import))],
        Policies = [.. syntax.Policies.Where(policy => !removed.Contains(policy))],
        Modules = [.. syntax.Modules.Select(module => module with
        {
            Authorize = module.Authorize is { } authorize && removed.Contains(authorize) ? null : module.Authorize,
            Features = [.. module.Features.Select(feature => Prune(feature, removed))]
        })]
    };

    static FeatureSyntax Prune(FeatureSyntax feature, HashSet<SyntaxNode> removed) => feature with
    {
        Authorize = feature.Authorize is { } authorize && removed.Contains(authorize) ? null : feature.Authorize,
        Slices = [.. feature.Slices.Where(slice => !removed.Contains(slice)).Select(slice => slice with
        {
            Specifications = [.. slice.Specifications.Where(specification => !removed.Contains(specification))]
        })],
        Features = [.. feature.Features.Select(child => Prune(child, removed))]
    };

    void IndexFeature(string module, FeatureSyntax feature, string featurePath)
    {
        var parent = identities.ResolveSemantic(SemanticAddress.ForFeature(identities.Application, module, [.. featurePath.Split('/')]));
        foreach (var slice in feature.Slices)
        {
            var address = $"{module}/{featurePath}/{slice.Name}";
            _units.Add(new(slice, SemanticExclusionKind.Slice, address, parent));
            foreach (var specification in slice.Specifications)
            {
                _units.Add(new(specification, SemanticExclusionKind.Specification, $"{address}/{specification.Name}", parent));
            }
        }
        foreach (var child in feature.Features) IndexFeature(module, child, $"{featurePath}/{child.Name}");
    }

    sealed class SliceOwners : ScreenplaySyntaxWalker
    {
        readonly Dictionary<SourceLocation, SliceSyntax> _exact = [];
        readonly Dictionary<(string? Path, int Line), SliceSyntax> _lines = [];
        readonly Dictionary<SourceLocation, SpecificationSyntax> _specifications = [];
        readonly Dictionary<(string? Path, int Line), SpecificationSyntax> _specificationLines = [];
        SliceSyntax? _slice;
        SpecificationSyntax? _specification;

        /// <inheritdoc/>
        public override void VisitNode(SyntaxNode node)
        {
            if (_slice is null) return;
            Record(node.Location);
            foreach (var location in node.DirectiveLocations.Values) Record(location);
        }

        /// <inheritdoc/>
        public override void VisitSlice(SliceSyntax syntax)
        {
            var previous = _slice;
            _slice = syntax;
            base.VisitSlice(syntax);
            _slice = previous;
        }

        /// <inheritdoc/>
        public override void VisitSpecification(SpecificationSyntax syntax)
        {
            var previous = _specification;
            _specification = syntax;
            base.VisitSpecification(syntax);
            _specification = previous;
        }

        internal static Func<SourceLocation, IReadOnlyList<SyntaxNode>> CreateUnitsLocator(ApplicationSyntax syntax)
        {
            var walker = new SliceOwners();
            walker.VisitApplication(syntax);
            return walker.UnitsAt;
        }

        internal static Func<SourceLocation, SliceSyntax?> CreateLocator(ApplicationSyntax syntax)
        {
            var walker = new SliceOwners();
            walker.VisitApplication(syntax);
            return location => walker._exact.GetValueOrDefault(location) ?? walker._lines.GetValueOrDefault((location.Path, location.Line));
        }

        IReadOnlyList<SyntaxNode> UnitsAt(SourceLocation location)
        {
            var slice = _exact.GetValueOrDefault(location) ?? _lines.GetValueOrDefault((location.Path, location.Line));
            if (slice is null) return [];
            var specification = _specifications.GetValueOrDefault(location) ?? _specificationLines.GetValueOrDefault((location.Path, location.Line));
            return specification is null ? [slice] : [specification, slice];
        }

        void Record(SourceLocation location)
        {
            _exact[location] = _slice!;
            _lines[(location.Path, location.Line)] = _slice!;
            if (_specification is null) return;
            _specifications[location] = _specification;
            _specificationLines[(location.Path, location.Line)] = _specification;
        }
    }

    internal sealed record Unit(SyntaxNode Syntax, SemanticExclusionKind Kind, string Address, SemanticId ParentId);
}
