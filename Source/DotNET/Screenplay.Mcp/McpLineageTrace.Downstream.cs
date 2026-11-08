// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Where a member's value goes: the mappings that read it and the members they set, then, for a read-model
// property, the queries and screens that show it.
internal sealed partial class McpLineageTrace
{
    IEnumerable<McpLineageStep> Downstream(McpDeclaration property)
    {
        if (OwnerOf(property)?.Syntax is ReadModelSyntax)
        {
            foreach (var reader in Readers(property)) yield return reader;
        }
        foreach (var effect in _links.ProjectionEffects.Where(effect => effect.Sources.Any(source => source.Members.Any(member => ReferenceEquals(member, property)))))
        {
            yield return ProjectionTarget(effect);
        }
        foreach (var source in _links.Incoming(property).Where(link => link.Reference.Role == "mapping-source" && link.Reference.Owner is not null))
        {
            foreach (var target in _mappingTargets.Value[source.Occurrence].Where(target => target.Reference.Owner?.Kind is not "Projection"))
            {
                yield return MappingTarget(target);
            }
        }
    }

    McpLineageStep ProjectionTarget(WorkspacePropertyProjectionEffect effect)
    {
        var step = ProjectionMapping(effect);
        if (step.Status != "resolved") return step with { Next = null };
        var targets = effect.Targets.Select(Member).ToArray();

        return step with { Next = () => targets };
    }

    // The mapping sets one member from the value; a command with a handler owns its event's values, so its mapping is only a hint (decision 0082).
    McpLineageStep MappingTarget(WorkspacePropertyLink link)
    {
        var owner = link.Reference.Owner!;
        var role = owner.Kind switch
        {
            "Capture" => "capture-mapping",
            "Reaction" when link.Candidates.FirstOrDefault() is { } target && OwnerOf(target)?.Syntax is CommandSyntax => "invocation-mapping",
            "Command" or "Reaction" => "production-mapping",
            _ => "interaction-mapping"
        };
        var step = new McpLineageStep(role, "resolved", owner.Kind, owner.Address, link.Reference.Location) { Text = McpLineageText.Clause(_links.NodeAt(link.Occurrence), link.Reference.Name) };
        if (link.Candidates.Length > 1) return Ambiguous(step, link.Reference.Name, link.Candidates);
        if (link.Reference.Name.Contains('.')) return step with { Status = "incomplete", Reason = $"'{link.Reference.Name}' is a nested path, which the index does not resolve." };
        if (link.Candidates.Length == 0) return step with { Status = "incomplete", Reason = $"'{link.Reference.Name}' does not resolve to a member the mapping sets." };
        if (role == "production-mapping" && Declaration(owner)?.Syntax is CommandSyntax { Handler: not null })
        {
            return McpLineageStep.Break("opaque", "code", owner.Kind, owner.Address, link.Reference.Location, $"Command {owner.Name} has a handler, which owns the values it produces; the mapping is a non-normative hint, and the trace does not read what the handler does with the value.") with { Text = step.Text };
        }
        var next = Member(link.Candidates[0]);

        return step with { Next = () => [next] };
    }
}
