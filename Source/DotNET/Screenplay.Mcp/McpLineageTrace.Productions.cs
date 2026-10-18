// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpLineageTrace
{
    // Each production is its own branch: a producer producing the event twice can map the property in one and not the other.
    List<McpLineageStep> EventSources(McpDeclaration property, McpDeclaration @event)
    {
        var mappings = _links.Incoming(property).Where(link => link.Reference.Role == "mapping-target" && link.Reference.Owner is not null).ToArray();
        var steps = mappings.Select(link => Production(link, property, @event)).ToList();
        var mapped = mappings.Select(link => _links.ProductionOf(link.Occurrence)?.Location).OfType<SourceLocation>().ToHashSet();
        var producers = References(@event, "produces", "refusalProduces", "appends")
            .Where(producer => _producerKinds.Contains(producer.Owner.Kind, StringComparer.Ordinal))
            .DistinctBy(producer => (producer.Owner.Kind, producer.Owner.Address, producer.Reference.Location));
        steps.AddRange(producers.Where(producer => !mapped.Contains(producer.Reference.Location)).Select(producer => Unmapped(producer, property, @event)));
        if (steps.Count > 0) return steps;
        if (@event.Syntax is EventSyntax { Origin: { } origin })
        {
            return [new("external", "resolved", @event.Kind, @event.Address, @event.Location) { Text = $"origin {origin}", Reason = $"{@event.Name} is declared with an origin and nothing in the model produces it." }];
        }

        return [McpLineageStep.Break("missing", "production-mapping", @event.Kind, @event.Address, property.Location, $"Nothing in the model produces {@event.Name}, so no production mapping sets {property.Name}.")];
    }

    // A command with a handler leaves event property values to the handler; its mappings are non-normative hints (decision 0082).
    McpLineageStep Production(WorkspacePropertyLink link, McpDeclaration property, McpDeclaration @event)
    {
        var owner = link.Reference.Owner!;
        if (owner.Kind == "Capture") return Mapping(link, "capture-mapping");
        if (Declaration(owner)?.Syntax is not CommandSyntax { Handler: not null }) return Mapping(link, "production-mapping");
        var reason = $"Command {owner.Name} has a handler, which owns the values of {@event.Name}; the mapping is a non-normative hint, and the trace does not read what the handler sets {property.Name} to.";

        return McpLineageStep.Break("opaque", "code", owner.Kind, owner.Address, link.Reference.Location, reason) with { Text = McpLineageText.Clause(_links.NodeAt(link.Occurrence), link.Reference.Name) };
    }

    McpLineageStep Unmapped(Referrer producer, McpDeclaration property, McpDeclaration @event)
    {
        var owner = producer.Owner;
        var code = Declaration(owner)?.Syntax switch
        {
            CommandSyntax { Handler: not null } => $"Command {owner.Name} produces {@event.Name} through its handler",
            ReactionSyntax reaction when reaction.Triggers.Any(trigger => trigger.File is not null || trigger.Code is not null) => $"Reaction {owner.Name} produces {@event.Name} with trigger code",
            _ => null
        };
        if (code is not null) return McpLineageStep.Break("opaque", "code", owner.Kind, owner.Address, producer.Reference.Location, $"{code}; the trace does not read what the code sets {property.Name} to.");
        var role = owner.Kind == "Capture" ? "capture-mapping" : "production-mapping";
        var verb = owner.Kind == "Capture" ? "appends" : "produces";

        return McpLineageStep.Break("missing", role, owner.Kind, owner.Address, producer.Reference.Location, $"{owner.Name} {verb} {@event.Name} without mapping {property.Name}.");
    }

    // A command property is the model's input boundary: form inputs, screen action arguments and reaction invocations feed it.
    IEnumerable<McpLineageStep> CommandSources(McpDeclaration property)
    {
        foreach (var link in _links.Incoming(property).Where(link => link.Reference.Owner is not null))
        {
            var owner = link.Reference.Owner!;
            if (link.Reference.Role == "screen-binding")
            {
                yield return new("screen-input", "resolved", owner.Kind, owner.Address, link.Reference.Location) { Text = McpLineageText.Clause(_links.NodeAt(link.Occurrence), link.Reference.Name) };
            }
            else if (link.Reference.Role == "mapping-target")
            {
                yield return Mapping(link, owner.Kind == "Reaction" ? "invocation-mapping" : "interaction-mapping");
            }
        }
    }

    McpLineageStep Mapping(WorkspacePropertyLink link, string role)
    {
        var owner = link.Reference.Owner!;
        var node = _links.NodeAt(link.Occurrence);
        var location = link.Reference.Location;
        var step = new McpLineageStep(role, "resolved", owner.Kind, owner.Address, location) { Text = McpLineageText.Clause(node, link.Reference.Name) };
        var sources = _mappingSources.Value[link.Occurrence].Where(source => !ReferenceEquals(source, link))
            .OrderBy(source => source.Reference.Location.Line).ThenBy(source => source.Reference.Location.Column).ToArray();
        if (link.Candidates.Length > 1) return Ambiguous(step, link.Reference.Name, link.Candidates);
        if (sources.FirstOrDefault(source => source.Candidates.Length > 1) is { } ambiguous) return Ambiguous(step, ambiguous.Reference.Name, ambiguous.Candidates);
        if (link.Reference.Name.Contains('.')) return step with { Status = "incomplete", Reason = $"'{link.Reference.Name}' is a nested path, which the index does not resolve." };
        var expression = (node as PropertyMappingSyntax)?.Source;
        if (expression is RawExpressionSyntax) return step with { Status = "incomplete", Reason = "The mapping is a raw expression, which the index does not resolve." };

        // A capture source-item path that names no declared input event member is an outside value.
        var outside = sources.Where(source => source.Candidates.Length == 0 && source.Reference.Name.StartsWith("$.", StringComparison.Ordinal)).ToArray();
        var inside = sources.Except(outside).ToArray();
        if (inside.FirstOrDefault(source => source.Candidates.Length != 1 || source.Reference.IncompleteSourceOwner) is { } unresolved)
        {
            return step with { Status = "incomplete", Reason = $"'{unresolved.Reference.Name}' does not resolve to exactly one member, or reaches it through a nested path or a legacy command read." };
        }
        if (outside.Length == 0 && link.Resolution != "resolved") return step with { Status = "incomplete", Reason = "The index cannot resolve this mapping's target or production fully." };
        var next = inside.Select(source => Member(source.Candidates[0]))
            .Concat(outside.Select(source => McpLineageStep.Terminal("capture-source", source.Reference.Location, source.Reference.Name))).ToArray();
        if (next.Length > 0) return step with { Next = () => next };
        var terminal = McpLineageText.Terminal(expression, location);

        return terminal is null
            ? step with { Status = "incomplete", Reason = "The mapping value is neither a member nor a literal or context value the trace recognizes." }
            : step with { Next = () => [terminal] };
    }
}
