// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

sealed class McpSyntaxIndex : ScreenplaySyntaxWalker
{
    readonly List<McpDeclaration> _declarations = [];
    readonly List<McpReference> _references = [];
    readonly List<string> _scope = [];
    readonly List<McpReadOwner> _hierarchy = [];
    readonly McpReadOwnership _ownership = new();
    readonly Dictionary<SyntaxNode, McpDeclaration> _owners = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Kind, string Name, string Scope), McpDeclaration> _scaffolds = [];
    McpQueryIndex _queries = null!;
    bool _inRefusal;
    ConstraintSyntax? _constraint;

    internal EventSourceReadConfidence? SourceConfidence { get; set; }

    internal McpAuthoringReadiness Readiness { get; private set; } = null!;

    internal IEnumerable<McpDeclaration> Declarations => _declarations;
    internal IEnumerable<McpReference> References => _references;

    internal IEnumerable<McpQueryIndexResolution> ResolvedReferences => _queries.ResolvedReferences;

    internal int ResolutionCount => _queries.ResolutionCount;

    internal int CandidateInspectionCount => _queries.CandidateInspectionCount;

    /// <inheritdoc/>
    public override void VisitApplication(ApplicationSyntax syntax)
    {
        Readiness ??= new(syntax);
        _ownership.VisitApplication(syntax);
        base.VisitApplication(syntax);
    }

    /// <inheritdoc/>
    public override void VisitEventSource(EventSourceSyntax syntax)
    {
        _ownership.VisitEventSource(syntax);
        Declare("EventSource", syntax.Name, syntax, syntax.Description, new { syntaxOnly = true, executionReadiness = Readiness.ExecutionReadiness(syntax) });
        _scope.Add(syntax.Name);
        base.VisitEventSource(syntax);
        _scope.RemoveAt(_scope.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax)
    {
        var declaration = Declare("Module", syntax.Name, syntax, syntax.Description);
        _hierarchy.Add(declaration.Owner);
        _scope.Add(syntax.Name);
        base.VisitModule(syntax);
        _scope.RemoveAt(_scope.Count - 1);
        _hierarchy.RemoveAt(_hierarchy.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax)
    {
        var declaration = Declare("Feature", syntax.Name, syntax, syntax.Description);
        _hierarchy.Add(declaration.Owner);
        _scope.Add(syntax.Name);
        base.VisitFeature(syntax);
        _scope.RemoveAt(_scope.Count - 1);
        _hierarchy.RemoveAt(_hierarchy.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax)
    {
        var declaration = Declare("Slice", syntax.Name, syntax, syntax.Description, new { syntaxOnly = Readiness.SyntaxOnly(syntax), executionReadiness = Readiness.ExecutionReadiness(syntax) });
        _hierarchy.Add(declaration.Owner);
        _scope.Add(syntax.Name);
        base.VisitSlice(syntax);
        _scope.RemoveAt(_scope.Count - 1);
        _hierarchy.RemoveAt(_hierarchy.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitConstraint(ConstraintSyntax syntax)
    {
        var previous = _constraint;
        _constraint ??= syntax;
        base.VisitConstraint(syntax);
        _constraint = previous;
    }

    /// <inheritdoc/>
    public override void VisitInvocationRefusal(InvocationRefusalSyntax syntax)
    {
        _inRefusal = true;
        base.VisitInvocationRefusal(syntax);
        _inRefusal = false;
    }

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        switch (node)
        {
            case CommandSyntax value: Declare("Command", value.Name, value, value.Description, new { produces = Readiness.ProducedEvents(value), generatedProperties = value.Properties.Where(property => property.IsGenerated).Select(property => property.Name), response = value.Response, authoredRoute = value.Stream, ambiguousStreamCandidates = value.StreamCandidates, syntaxOnly = Readiness.SyntaxOnly(value), executionReadiness = Readiness.ExecutionReadiness(value, null) }); break;
            case EventStreamSyntax value: Declare("EventStream", value.Name, value, value.Description, new { syntaxOnly = true, executionReadiness = Readiness.ExecutionReadiness(value) }); break;
            case SystemSyntax value: Declare("System", value.Name, value, value.Description, new { syntaxOnly = true, executionReadiness = Readiness.ExecutionReadiness(value) }); break;
            case OperationSyntax value: Declare("Operation", value.Name, value, value.Description, new { syntaxOnly = true, executionReadiness = Readiness.ExecutionReadiness(value) }); break;
            case QuerySyntax value: Declare("Query", value.Name, value); break;
            case EventSyntax value: Declare("Event", value.Name, value, value.Description); break;
            case ReadModelSyntax value: Declare("ReadModel", value.Name, value); break;
            case ScreenSyntax value: Declare("Screen", value.Name, value); break;
            case ConceptSyntax value: Declare("Concept", value.Name, value); break;
            case TypeSyntax value: Declare("Type", value.Name, value); break;
            case PolicySyntax value: Declare("Policy", value.Name, value); break;
            case PersonaSyntax value: Declare("Persona", value.Name, value); break;
            case LayoutSyntax value: Declare("Layout", value.Name, value); break;
            case ScreenTemplateSyntax value: Declare("ScreenTemplate", value.Name, value); break;
            case DialogTemplateSyntax value: Declare("DialogTemplate", value.Name, value); break;
            case FormSyntax value: Declare("Form", value.Name, value); break;
            case SlotSyntax { Contributes: not null } value: Declare("ContributionPoint", value.Contributes, value); break;
            case UiProfileSyntax value: Declare("UiProfile", value.Name, value); break;
            case ThemeSyntax value: Declare("Theme", value.Name, value); break;
            case TriggerSyntax value: Declare("Trigger", value.Name, value); break;
            case ReactionSyntax value: Declare("Reaction", value.Name, value, value.Description); break;
            case ProjectionSyntax value: Declare("Projection", value.Name, value); break;
            case ReducerSyntax value: Declare("Reducer", value.Name, value); break;
            case CaptureSyntax value: Declare("Capture", value.Name, value); break;
            case ConstraintSyntax value when ReferenceEquals(value, _constraint): Declare("Constraint", value.Name, value); break;
            case SpecificationExampleSyntax value:
                Declare("Example", value.Name, value, value.Description, new { value.Type, value.Values, value.For, value.GeneratedValues });
                break;
            case SpecificationSyntax value:
                Declare("Specification", value.Name, value, details: new
                {
                    generatedValues = value.When?.GeneratedValues,
                    thenReturns = value.ThenReturns,
                    syntaxOnly = Readiness.SyntaxOnly(value),
                    executionReadiness = Readiness.ExecutionReadiness(value),
                    given = value.Given.Select(item => item.EventType),
                    when = value.When?.CommandType,
                    whenAppendedEvent = value.WhenAppended?.EventType,
                    thenDenied = value.ThenDenied is not null,
                    then = value.ThenEvents.Select(item => item.EventType),
                    errors = value.ThenErrors.Select(item => item.Name),
                    queries = value.ThenQueries.Select(item => item.Query)
                });
                break;
        }

        var owningSyntax = _ownership.For(node);
        var owner = owningSyntax is null ? null : _owners.GetValueOrDefault(owningSyntax);
        foreach (var reference in McpReferenceKinds.For(node, owningSyntax))
        {
            var refusalProduction = _inRefusal && node is ProducesSyntax;
            var role = (refusalProduction, owner?.Syntax) switch
            {
                (true, _) => "refusalProduces",
                (_, SpecificationSyntax specification) => McpFixtureOccurrences.Role(specification, node, reference.Role),
                _ => reference.Role
            };
            _references.Add(new(reference.Name, reference.Kinds, [.. _scope], ReferenceLocation(node), role, owner?.Owner)
            {
                UseProductionCandidates = node is ProducesSyntax or SpecificationOperationSyntax or SpecificationOperationFailureSyntax or SpecificationCompensatedSyntax,
                AmbiguousSourceOwner = node is CommandStreamSyntax { PropertyCandidate: not null }
            });
        }
    }

    internal void Initialize(ApplicationSyntax application) => Readiness = new(application);

    internal void Complete(ApplicationSyntax? application)
    {
        if (application is not null)
        {
            McpLogicalDeclarations.Complete(_declarations, application);
        }

        _declarations.AddRange([.. McpLogicalReadModels.From(_declarations)]);
        var productions = new McpProductionInventory([.. _declarations]);
        var sources = _declarations.Where(declaration => declaration.Kind == "EventSource" && declaration.Scope.Length == 0)
            .ToLookup(declaration => declaration.Name, StringComparer.Ordinal);
        for (var referenceIndex = 0; referenceIndex < _references.Count; referenceIndex++)
        {
            var reference = _references[referenceIndex];
            if (reference.Kinds.Contains("EventSource", StringComparer.Ordinal) || reference.Kinds.Contains("EventStream", StringComparer.Ordinal))
            {
                var parts = reference.Name.Split('.');
                var confidence = SourceConfidence?.Resolve(parts[0], reference.Kinds.Contains("EventStream", StringComparer.Ordinal) ? parts.ElementAtOrDefault(1) ?? string.Empty : null);
                _references[referenceIndex] = reference with
                {
                    AmbiguousSourceOwner = reference.AmbiguousSourceOwner || sources[parts[0]].Count() > 1 || confidence?.State == "ambiguous",
                    IncompleteSourceOwner = confidence?.State == "incomplete",
                    SourceConfidenceReasons = confidence?.Reasons ?? []
                };
            }
        }
        for (var index = 0; index < _references.Count; index++)
        {
            var reference = _references[index];
            if (reference.Role is not ("produces" or "refusalProduces")) continue;
            var targets = productions.ResolveReference(reference.Name, reference.Scope);
            _references[index] = reference with { Kinds = targets is [var target] ? [target.Kind] : ["Event", "Operation"] };
        }
        _queries = new(_declarations, _references, productions);
    }

    internal McpDeclaration[] Resolve(McpReference reference) => _queries.Resolve(reference);

    internal McpReferenceEdge ResolveProduction(string name, string[] scope)
    {
        var reference = new McpReference(name, ["Event", "Operation"], scope, new(0, 0, string.Empty), "production", null)
        {
            UseProductionCandidates = true
        };

        return new(reference, Resolve(reference));
    }

    internal McpDeclaration[] Find(string address, string kind) => _queries.Find(address, kind);

    internal bool HasExactOwnershipCollision(string kind, string name, string[] scope) => _queries.HasExactOwnershipCollision(kind, name, scope);

    internal IEnumerable<McpQueryIndexResolution> Incoming(McpDeclaration declaration) => _queries.Incoming(declaration);

    internal IEnumerable<McpReference> Outgoing(McpReadOwner owner) => _queries.Outgoing(owner);

    internal IEnumerable<McpReference> Outgoing(string ownerAddress) => _queries.Outgoing(ownerAddress);

    static SourceLocation ReferenceLocation(SyntaxNode node) => node switch
    {
        CommandStreamSyntax route => route.ReferenceLocation,
        SpecificationStreamSyntax route => route.ReferenceLocation,
        _ => node.Location
    };

    McpDeclaration Declare(string kind, string name, SyntaxNode node, string? description = null, object? details = null)
    {
        var key = (kind, name, McpQueryIndex.ScopeKey(_scope));
        var isScaffold = kind == "Module" || kind == "Feature";
        if (isScaffold && _scaffolds.TryGetValue(key, out var scaffold))
        {
            scaffold.Parts.Add(node);
            _owners[node] = scaffold;
            return scaffold;
        }

        var declaration = new McpDeclaration(kind, name, [.. _scope], node.Location, description, details, node) { Hierarchy = [.. _hierarchy] };
        _declarations.Add(declaration);
        _owners[node] = declaration;
        if (isScaffold)
        {
            _scaffolds.Add(key, declaration);
        }

        return declaration;
    }
}
