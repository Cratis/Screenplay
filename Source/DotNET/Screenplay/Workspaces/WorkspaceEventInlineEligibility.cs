// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The outcome of checking whether a standalone event can become the inline event of its only production.
/// </summary>
/// <param name="Refusal">The reason inlining cannot be proven to preserve meaning, or null when eligible.</param>
/// <param name="Production">The single eligible production when eligible.</param>
sealed record WorkspaceEventInlineEligibility(string? Refusal, WorkspaceSyntaxEntry? Production)
{
    internal static WorkspaceEventInlineEligibility Check(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry declaration)
    {
        var @event = (EventSyntax)declaration.Node;
        if (CheckDeclaration(index, declaration, @event) is { } declarationRefusal)
        {
            return Refused(declarationRefusal);
        }

        var references = new WorkspaceReferenceBindings(index).Bindings
            .Where(binding => binding.Target?.Entry?.Handle == declaration.Handle ||
                (binding.Reference.Domain is WorkspaceReferenceDomain.Event or WorkspaceReferenceDomain.Fixture && binding.Reference.Text.Split('.')[^1] == @event.Name))
            .ToArray();
        var productions = references.Where(binding => binding.Reference.Entry.Node is ProducesSyntax).ToArray();
        if (productions.Length != 1)
        {
            return Refused(productions.Length == 0 ? "no production resolves to the event." : "more than one production names the event.");
        }

        if (references.Length != 1 || productions[0].Target?.Entry?.Handle != declaration.Handle)
        {
            return Refused("the event is referenced elsewhere (projections, reactions, specifications, constraints, queries or other declarations); the references are not proven to survive inlining.");
        }

        var production = productions[0].Reference.Entry;
        if (CheckProduction(index, declaration, @event, production) is { } productionRefusal)
        {
            return Refused(productionRefusal);
        }

        return new(null, production);
    }

    static WorkspaceEventInlineEligibility Refused(string reason) => new(reason, null);

    static string? CheckDeclaration(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry declaration, EventSyntax @event)
    {
        if (declaration.Member != "events" || declaration.Parent is null || index.Find(declaration.Parent)?.Node is not SliceSyntax)
        {
            return "the event is not declared directly in a slice, so cross-slice ownership cannot be excluded.";
        }

        if (@event.Origin is not null || @event.Visibility != EventVisibility.Private)
        {
            return "an external-origin or public event is a contract with other applications.";
        }

        if (@event.HasGenerationMarker || @event.Generation != 1 ||
            index.Entries.Any(entry => entry.Node is EventSyntax other && other.Name == @event.Name && entry.Handle != declaration.Handle))
        {
            return "the event has multiple generations or shares its name with another declaration.";
        }

        if (@event.File is not null || !index.Workspace.AttachmentContents.IsEmpty)
        {
            return "a realization file is attached, and inline events cannot carry one.";
        }

        return index.Entries.Any(entry => WorkspaceOpaqueText.Named(entry.Node, @event.Name) is not null)
            ? "opaque code, imports or file text name the event, so every use cannot be resolved."
            : null;
    }

    static string? CheckProduction(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry declaration, EventSyntax @event, WorkspaceSyntaxEntry production)
    {
        var syntax = (ProducesSyntax)production.Node;
        var command = production.Parent is { } commandHandle ? index.Find(commandHandle) : null;
        if (command?.Node is not CommandSyntax commandSyntax || command.Parent != declaration.Parent)
        {
            return "the production is not owned by a command of the same slice as the event.";
        }

        if (production.Handle.Document != declaration.Handle.Document)
        {
            return "the production and the event are declared in different documents.";
        }

        if (commandSyntax.Handler is not null || syntax.InlineEvent is not null || syntax.InlineOperation is not null)
        {
            return "the producer is a handler or already declares an inline construct.";
        }

        if (syntax.When is not null)
        {
            return "the production is conditional.";
        }

        if (syntax.Tags?.Any() == true)
        {
            return "plain production tags are per-production tags and would become event-type tags.";
        }

        if (syntax.For is null)
        {
            return "the production omits 'for', which would silently become the inline identifier default.";
        }

        var properties = @event.Properties.Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        var mappings = syntax.Mappings.Select(mapping => mapping.Property).Order(StringComparer.Ordinal).ToArray();
        return properties.Length == 0 || !properties.SequenceEqual(mappings) || properties.Distinct(StringComparer.Ordinal).Count() != properties.Length
            ? "the production does not map every event property exactly once with typed mappings."
            : null;
    }
}
