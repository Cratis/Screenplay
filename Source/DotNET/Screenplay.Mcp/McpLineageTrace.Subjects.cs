// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpLineageTrace
{
    const string SubjectShape = "a property address (the command, event or read model's address followed by the member name) or a screen field " +
        "address (the screen's or form's address followed by the property the field binds)";

    internal McpLineageSubject Subject(string address)
    {
        var properties = _links.Find(address).Where(property => OwnerOf(property) is not null).ToArray();
        if (properties is [var property]) return new(property.Kind, property.Address, property.Location, property, null);
        if (properties.Length > 1) throw new McpFailure($"'{address}' names {properties.Length} properties; trace-lineage needs exactly one.");

        var fields = ScreenFields(address);
        return fields switch
        {
            [var field] => new("ScreenField", address, field.Reference.Location, null, field),
            [] => throw new McpFailure($"trace-lineage needs {SubjectShape}. '{address}' is neither."),
            _ => throw new McpFailure($"'{address}' names {fields.Length} screen fields; trace-lineage needs exactly one.")
        };
    }

    WorkspacePropertyLink[] ScreenFields(string address)
    {
        var separator = address.LastIndexOf('.');
        if (separator <= 0) return [];
        var owner = address[..separator];
        var name = address[(separator + 1)..];

        return [.. _links.Links.Where(link => link.Reference.Role == "screen-binding" && link.Reference.Name == name && link.Reference.Owner?.Address == owner)
            .DistinctBy(link => link.Occurrence)];
    }

    McpLineageStep Root(McpLineageSubject subject)
    {
        if (subject.Member is { } member) return Member(member) with { Member = null };
        var field = subject.Field!;
        var step = new McpLineageStep("screen-field", "resolved", field.Reference.Owner!.Kind, subject.Address, subject.Location)
        {
            Text = McpLineageText.Clause(_links.NodeAt(field.Occurrence), field.Reference.Name)
        };
        if (field.Candidates.Length > 1) return Ambiguous(step, field.Reference.Name, field.Candidates);
        if (field.Candidates.Length == 0) return step with { Status = "missing", Reason = $"The field binds '{field.Reference.Name}', which no command, event or read-model property declares." };
        if (field.Reference.Name.Contains('.')) return step with { Status = "incomplete", Reason = $"'{field.Reference.Name}' is a nested path, which the index does not resolve." };

        return step;
    }

    // A field bound to a command property is an input: its value goes downstream. A field showing a read-model
    // property is a display: its value comes from upstream.
    IEnumerable<McpLineageStep> Downstream(McpLineageSubject subject) => subject switch
    {
        { Member: { } member } => Downstream(member),
        { Field.Candidates: [var bound] } when Root(subject).Status == "resolved" && OwnerOf(bound)?.Syntax is CommandSyntax => [Member(bound)],
        _ => []
    };

    // An input field populated from a query ('from <property>') has that read-model property upstream.
    IEnumerable<McpLineageStep> Upstream(McpLineageSubject subject) => subject switch
    {
        { Member: { } member } => Sources(member),
        { Field: { Candidates: [var bound] } field } when Root(subject).Status == "resolved" => OwnerOf(bound)?.Syntax is CommandSyntax
            ? _links.Links.Where(link => link.Occurrence == field.Occurrence && link.Reference.Role == "screen-binding" && !ReferenceEquals(link, field) && link.Candidates.Length == 1)
                .Select(link => Member(link.Candidates[0]))
            : [Member(bound)],
        _ => []
    };
}
