// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    readonly McpReadOwnership _ownership = new();
    readonly Dictionary<SyntaxNode, McpDeclaration> _owners = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Kind, string Name, string Scope), McpDeclaration> _scaffolds = [];
    McpQueryIndex _queries = null!;

    internal IEnumerable<McpDeclaration> Declarations => _declarations;
    internal IEnumerable<McpReference> References => _references;

    internal IEnumerable<McpQueryIndexResolution> ResolvedReferences => _queries.ResolvedReferences;

    internal int ResolutionCount => _queries.ResolutionCount;

    internal int CandidateInspectionCount => _queries.CandidateInspectionCount;

    /// <inheritdoc/>
    public override void VisitApplication(ApplicationSyntax syntax)
    {
        _ownership.VisitApplication(syntax);
        base.VisitApplication(syntax);
    }

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax)
    {
        Declare("Module", syntax.Name, syntax, syntax.Description);
        _scope.Add(syntax.Name);
        base.VisitModule(syntax);
        _scope.RemoveAt(_scope.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax)
    {
        Declare("Feature", syntax.Name, syntax, syntax.Description);
        _scope.Add(syntax.Name);
        base.VisitFeature(syntax);
        _scope.RemoveAt(_scope.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax)
    {
        Declare("Slice", syntax.Name, syntax, syntax.Description);
        _scope.Add(syntax.Name);
        base.VisitSlice(syntax);
        _scope.RemoveAt(_scope.Count - 1);
    }

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        switch (node)
        {
            case CommandSyntax value: Declare("Command", value.Name, value, value.Description, new { produces = value.Produces.Select(produces => produces.Event) }); break;
            case QuerySyntax value: Declare("Query", value.Name, value); break;
            case EventSyntax value: Declare("Event", value.Name, value); break;
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
            case ConstraintSyntax value: Declare("Constraint", value.Name, value); break;
            case SpecificationSyntax value:
                Declare("Specification", value.Name, value, details: new
                {
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
            var role = owner?.Syntax is SpecificationSyntax specification ? McpFixtureOccurrences.Role(specification, node, reference.Role) : reference.Role;
            _references.Add(new(reference.Name, reference.Kinds, [.. _scope], node.Location, role, owner?.Owner));
        }
    }

    internal void Complete(ApplicationSyntax? application)
    {
        if (application is not null)
        {
            McpLogicalDeclarations.Complete(_declarations, application);
        }

        _declarations.AddRange([.. McpLogicalReadModels.From(_declarations)]);
        _queries = new(_declarations, _references);
    }

    internal McpDeclaration[] Resolve(McpReference reference) => _queries.Resolve(reference);

    internal McpDeclaration[] Find(string address, string kind) => _queries.Find(address, kind);

    internal IEnumerable<McpQueryIndexResolution> Incoming(McpDeclaration declaration) => _queries.Incoming(declaration);

    internal IEnumerable<McpReference> Outgoing(McpReadOwner owner) => _queries.Outgoing(owner);

    internal IEnumerable<McpReference> Outgoing(string ownerAddress) => _queries.Outgoing(ownerAddress);

    void Declare(string kind, string name, SyntaxNode node, string? description = null, object? details = null)
    {
        var key = (kind, name, McpQueryIndex.ScopeKey(_scope));
        var isScaffold = kind == "Module" || kind == "Feature";
        if (isScaffold && _scaffolds.TryGetValue(key, out var scaffold))
        {
            scaffold.Parts.Add(node);
            _owners[node] = scaffold;
            return;
        }

        var declaration = new McpDeclaration(kind, name, [.. _scope], node.Location, description, details, node);
        _declarations.Add(declaration);
        _owners[node] = declaration;
        if (isScaffold)
        {
            _scaffolds.Add(key, declaration);
        }
    }
}
