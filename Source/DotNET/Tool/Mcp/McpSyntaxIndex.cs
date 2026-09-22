// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpSyntaxIndex : ScreenplaySyntaxWalker
{
    readonly List<McpDeclaration> _declarations = [];
    readonly List<McpReference> _references = [];
    readonly List<string> _scope = [];

    internal IEnumerable<McpDeclaration> Declarations => _declarations;
    internal IEnumerable<McpReference> References => _references;

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
                    then = value.ThenEvents.Select(item => item.EventType),
                    errors = value.ThenErrors.Select(item => item.Name),
                    queries = value.ThenQueries.Select(item => item.Query)
                });
                break;
        }

        if (McpReferenceKinds.For(node) is { } reference)
        {
            _references.Add(new(reference.Name, reference.Kinds, [.. _scope], node.Location));
        }
    }

    internal McpDeclaration[] Resolve(McpReference reference)
    {
        var segments = reference.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return [];
        }

        var named = _declarations.Where(declaration => reference.Kinds.Contains(declaration.Kind, StringComparer.Ordinal) && declaration.Name == segments[^1]).ToArray();
        if (segments.Length > 1)
        {
            var qualifiers = segments[..^1];
            return [.. named.Where(declaration => declaration.Scope.Length >= qualifiers.Length && declaration.Scope.TakeLast(qualifiers.Length).SequenceEqual(qualifiers, StringComparer.Ordinal))];
        }

        // Matches the compiler's nearest shared scope rule, never a raw name grep.
        for (var depth = reference.Scope.Length; depth >= 0; depth--)
        {
            var visible = named.Where(declaration => declaration.Scope.Length >= depth && declaration.Scope.Take(depth).SequenceEqual(reference.Scope.Take(depth), StringComparer.Ordinal)).ToArray();
            if (visible.Length > 0)
            {
                return visible;
            }
        }

        return [];
    }

    void Declare(string kind, string name, SyntaxNode node, string? description = null, object? details = null) =>
        _declarations.Add(new(kind, name, [.. _scope], node.Location, description, details, node));
}
