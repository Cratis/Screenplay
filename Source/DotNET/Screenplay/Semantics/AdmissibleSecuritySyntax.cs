// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleSecuritySyntax : ScreenplaySyntaxWalker
{
    IReadOnlyList<SliceSyntax>? _affected;
    SliceSyntax? _slice;
    Authorization? _authorization;
    bool _command;

    internal bool CertainViewReferences { get; private set; } = true;
    internal List<Authorization> Authorizations { get; } = [];
    internal List<ViewReference> References { get; } = [];
    internal List<ViewReference> Reads { get; } = [];
    internal List<ViewReference> Views { get; } = [];

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        _authorization?.Locations.Add(node.Location);
        foreach (var location in node.DirectiveLocations.Values) _authorization?.Locations.Add(location);
    }

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax)
    {
        var previous = _affected;
        _affected = [.. syntax.Features.SelectMany(AllSlices)];
        base.VisitModule(syntax);
        _affected = previous;
    }

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax)
    {
        var previous = _affected;
        _affected = [.. AllSlices(syntax)];
        base.VisitFeature(syntax);
        _affected = previous;
    }

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax)
    {
        var previous = _affected;
        var previousSlice = _slice;
        _affected = [syntax];
        _slice = syntax;
        base.VisitSlice(syntax);
        _slice = previousSlice;
        _affected = previous;
    }

    /// <inheritdoc/>
    public override void VisitAuthorize(AuthorizeSyntax syntax)
    {
        var previous = _authorization;
        _authorization = new(syntax, _affected);
        Authorizations.Add(_authorization);
        base.VisitAuthorize(syntax);
        _authorization = previous;
    }

    /// <inheritdoc/>
    public override void VisitCommand(CommandSyntax syntax)
    {
        var previous = _command;
        _command = true;
        base.VisitCommand(syntax);
        _command = previous;
    }

    /// <inheritdoc/>
    public override void VisitReads(ReadsSyntax syntax)
    {
        var reference = new ViewReference(syntax.ReadModel, _slice);
        if (_command) Reads.Add(reference);
        References.Add(reference);
        base.VisitReads(syntax);
    }

    /// <inheritdoc/>
    public override void VisitReadModel(ReadModelSyntax syntax)
    {
        Views.Add(new(syntax.Name, _slice));
        base.VisitReadModel(syntax);
    }

    /// <inheritdoc/>
    public override void VisitQuery(QuerySyntax syntax)
    {
        References.Add(new(syntax.ReturnType.Name, _slice));
        base.VisitQuery(syntax);
    }

    /// <inheritdoc/>
    public override void VisitProjection(ProjectionSyntax syntax)
    {
        if (syntax.ReadModel is { } target) References.Add(new(target, _slice));
        else CertainViewReferences = false;
        base.VisitProjection(syntax);
    }

    /// <inheritdoc/>
    public override void VisitReducer(ReducerSyntax syntax)
    {
        References.Add(new(syntax.ReadModel, _slice));
        base.VisitReducer(syntax);
    }

    /// <inheritdoc/>
    public override void VisitSpecificationReadModel(SpecificationReadModelSyntax syntax)
    {
        References.Add(new(syntax.Name, _slice));
        base.VisitSpecificationReadModel(syntax);
    }

    /// <inheritdoc/>
    public override void VisitSpecificationAbsentReadModel(SpecificationAbsentReadModelSyntax syntax)
    {
        References.Add(new(syntax.Name, _slice));
        base.VisitSpecificationAbsentReadModel(syntax);
    }

    internal static bool Known(PolicyRequirementSyntax requirement) => requirement switch
    {
        PolicyReferenceSyntax => true,
        LogicalPolicyRequirementSyntax logical => Known(logical.Left) && Known(logical.Right),
        _ => false
    };

    static IEnumerable<SliceSyntax> AllSlices(FeatureSyntax feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    internal sealed record ViewReference(string Name, SliceSyntax? Slice);
    internal sealed record Authorization(AuthorizeSyntax Syntax, IReadOnlyList<SliceSyntax>? Slices)
    {
        internal HashSet<SourceLocation> Locations { get; } = [];
        internal bool Owns(SourceLocation location) => Locations.Any(value => value.Path == location.Path && value.Line == location.Line);
    }
}
