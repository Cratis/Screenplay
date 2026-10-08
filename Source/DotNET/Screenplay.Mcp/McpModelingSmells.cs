// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Mcp;

static class McpModelingSmells
{
    static readonly string[] _eventSuffixes = ["Updated", "Changed", "Edited", "Saved", "Modified", "Deleted", "Synced", "Received"];
    static readonly string[] _commandPrefixes = ["Update", "Edit", "Save", "Set", "Manage", "Get", "Load", "Fetch"];

    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var selected = McpReviewSelection.Declarations(snapshot, arguments);
        var index = snapshot.Index;
        var document = McpJson.OptionalString(arguments, "document");
        var fanOut = McpJson.Integer(arguments, "eventFanOutThreshold", 5, 1, 200);
        var fanIn = McpJson.Integer(arguments, "propertyFanInThreshold", 5, 1, 200);
        var findings = new List<object>();
        var events = index.Declarations.Where(declaration => declaration.Syntax is EventSyntax).ToArray();
        var commands = index.Declarations.Where(declaration => declaration.Syntax is CommandSyntax).ToArray();
        var producers = commands.ToDictionary(command => command, command => Produced(index, command));
        var eventProducers = producers.SelectMany(pair => pair.Value.Select(@event => (Event: @event, Command: pair.Key)))
            .ToLookup(pair => pair.Event, pair => pair.Command);
        var shapes = events.ToDictionary(@event => @event, @event => string.Join('\0', Shape((EventSyntax)@event.Syntax)));
        var shapeGroups = events.ToLookup(@event => shapes[@event], StringComparer.Ordinal);

        void Add(string ruleId, McpDeclaration owner, string question, string? property = null, IEnumerable<McpDeclaration>? related = null)
        {
            if (document is not null && !owner.Locations.Any(location => location.Path == document)) return;
            var evidence = (related ?? []).Distinct().OrderBy(declaration => declaration.Address, StringComparer.Ordinal).ToArray();
            findings.Add(new
            {
                ruleId,
                severity = "info",
                declaration = owner.Owner,
                property,
                question,
                relatedCount = evidence.Length,
                related = evidence.Take(20).Select(declaration => declaration.Owner).ToArray(),
                relatedTruncated = evidence.Length > 20
            });
        }

        foreach (var @event in selected.Where(declaration => declaration.Syntax is EventSyntax))
        {
            if (_eventSuffixes.Any(suffix => @event.Name.EndsWith(suffix, StringComparison.Ordinal)))
            {
                Add("SMELL001", @event, $"Does '{@event.Name}' name the business reason for the fact rather than a generic change or technical delivery?");
            }
            var shape = shapes[@event];
            var ownProducers = eventProducers[@event].ToArray();
            var duplicates = shape.Length == 0 || ownProducers.Length == 0 ? [] : shapeGroups[shape].Where(other => other != @event &&
                eventProducers[other].Any(command => ownProducers.Length > 1 || ownProducers[0] != command)).ToArray();
            if (duplicates.Length > 0)
            {
                Add("SMELL003", @event, $"Do '{@event.Name}' and the {duplicates.Length} same-shaped event(s) produced by different commands record distinct business facts, or duplicate one meaning?", related: duplicates);
            }
        }
        foreach (var command in selected.Where(declaration => declaration.Syntax is CommandSyntax))
        {
            if (_commandPrefixes.Any(prefix => command.Name.StartsWith(prefix, StringComparison.Ordinal) &&
                (command.Name.Length == prefix.Length || char.IsUpper(command.Name[prefix.Length]))))
            {
                Add("SMELL002", command, $"Does '{command.Name}' express a business decision rather than a generic edit or technical operation?");
            }
            var produced = producers[command];
            if (produced.Length > fanOut)
            {
                Add("SMELL004", command, $"Does '{command.Name}' need to produce {produced.Length} distinct events (threshold {fanOut}), or does it combine separate business decisions?", related: produced);
            }
        }
        foreach (var projection in selected.Where(declaration => declaration.Syntax is ProjectionSyntax))
        {
            var syntax = (ProjectionSyntax)projection.Syntax;
            var models = index.Outgoing(projection.Owner).Where(reference => reference.Role == "builds" || reference.Role == "buildsVariant")
                .SelectMany(index.Resolve).Where(declaration => declaration.Kind == "ReadModel").Distinct().ToArray();
            foreach (var model in models)
            {
                var variants = syntax.Blocks.OfType<ProjectionVariantSyntax>().Where(variant => McpReviewSelection.Resolves(index, projection, variant.Name, "ReadModel", model)).ToArray();
                var shared = syntax.Blocks.Where(block => block is not ProjectionVariantSyntax).ToArray();
                var blocks = variants.Length == 0 ? shared : variants.SelectMany(variant => ProjectionVariantHandlers.For(shared, variant));
                var feeds = Feeds(index, projection, blocks, syntax.AutoMap != AutoMapMode.Disabled, string.Empty, [], (model.Syntax as ReadModelSyntax)?.Properties ?? [])
                    .Where(feed => model.Syntax is not ReadModelSyntax shape || shape.Properties.Any(property => property.Name == feed.Property.Split('.')[0]));
                foreach (var group in feeds.GroupBy(feed => feed.Property, StringComparer.Ordinal))
                {
                    var sources = group.Select(feed => feed.Event).Distinct().ToArray();
                    if (sources.Length > fanIn)
                    {
                        Add("SMELL005", model, $"Does '{model.Name}.{group.Key}' need values from {sources.Length} distinct events (threshold {fanIn}), or does this view combine unrelated information?", group.Key, sources);
                    }
                }
            }
        }

        return McpReviewSelection.Page(snapshot, findings, arguments, "Advisory questions only; never compiler diagnostics and never part of --warnaserror. No suppression syntax or persistence is implemented. Names are case-sensitive. Shapes compare nonempty sets of declared property names/types/modifiers, not business meaning. Fan-out counts distinct resolved event targets, not occurrences or operations. Fan-in includes explicit mappings, known-event automap, every/all, joins, nested/child paths and variants; opaque code and unresolved references are not inferred. Related evidence is capped at 20 with complete counts.");
    }

    static string[] Shape(EventSyntax syntax) => [.. syntax.Properties.Select(property => $"{property.Name}:{property.Type.Name}:{property.Type.IsCollection}:{property.Type.IsOptional}").Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    static McpDeclaration[] Produced(McpSyntaxIndex index, McpDeclaration command) => [.. index.Outgoing(command.Owner).Where(reference => reference.Role == "produces").Select(index.Resolve).Where(candidates => candidates.Length == 1).Select(candidates => candidates[0]).Where(declaration => declaration.Kind == "Event").Distinct()];

    static IEnumerable<(string Property, McpDeclaration Event)> Feeds(McpSyntaxIndex index, McpDeclaration projection, IEnumerable<ProjectionBlockSyntax> source, bool autoMap, string prefix, IEnumerable<MappingSyntax> inherited, IEnumerable<PropertySyntax> targets)
    {
        var blocks = source.ToArray();
        var every = blocks.OfType<EverySyntax>().ToArray();
        var mappings = inherited.Concat(every.SelectMany(block => block.Mappings)).ToArray();
        var childMappings = inherited.Concat(every.Where(block => block.IncludeChildren).SelectMany(block => block.Mappings)).ToArray();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var @event in from.Events.Select(value => ResolveEvent(index, projection, value.Event)).OfType<McpDeclaration>())
                    {
                        foreach (var feed in Mapped(@event, from.Mappings, mappings, autoMap, prefix, targets, false)) yield return feed;
                    }
                    break;
                case JoinSyntax join:
                    foreach (var joined in join.Events)
                    {
                        if (ResolveEvent(index, projection, joined.Event) is { } @event)
                        {
                            foreach (var feed in Mapped(@event, joined.Mappings, mappings, autoMap, prefix, targets, true)) yield return feed;
                        }
                    }
                    break;
                case AllSyntax all:
                    foreach (var @event in index.Declarations.Where(declaration => declaration.Syntax is EventSyntax && index.Find(declaration.Address, declaration.Kind).Length == 1))
                    {
                        foreach (var feed in Mapped(@event, all.Mappings, mappings, Mode(all.AutoMap, autoMap), prefix, targets, false)) yield return feed;
                    }
                    break;
                case ChildrenSyntax children:
                    foreach (var feed in Feeds(index, projection, children.Blocks, Mode(children.AutoMap, ((ProjectionSyntax)projection.Syntax).AutoMap != AutoMapMode.Disabled), prefix + children.Property + ".", childMappings, ElementProperties(index, projection, targets, children.Property))) yield return feed;
                    break;
                case NestedSyntax nested:
                    foreach (var feed in Feeds(index, projection, nested.Blocks, Mode(nested.AutoMap, ((ProjectionSyntax)projection.Syntax).AutoMap != AutoMapMode.Disabled), prefix + nested.Property + ".", childMappings, ElementProperties(index, projection, targets, nested.Property))) yield return feed;
                    break;
            }
        }
    }

    static bool Mode(AutoMapMode mode, bool inherited) => mode == AutoMapMode.Inherit ? inherited : mode == AutoMapMode.Enabled;

    static McpDeclaration? ResolveEvent(McpSyntaxIndex index, McpDeclaration from, string name) => index.Resolve(new(name, ["Event"], from.Scope, from.Location, "review", from.Owner)) is [var @event] ? @event : null;

    static IEnumerable<PropertySyntax> ElementProperties(McpSyntaxIndex index, McpDeclaration projection, IEnumerable<PropertySyntax> targets, string name) =>
        targets.FirstOrDefault(property => property.Name == name) is { } property && index.Resolve(new(property.Type.Name, ["Type"], projection.Scope, property.Location, "review", projection.Owner)) is [var type] && type.Syntax is TypeSyntax syntax ? syntax.Properties : [];

    static IEnumerable<(string Property, McpDeclaration Event)> Mapped(McpDeclaration @event, IEnumerable<MappingSyntax> mappings, IEnumerable<MappingSyntax> inherited, bool autoMap, string prefix, IEnumerable<PropertySyntax> targets, bool isJoin) =>
        mappings.Concat(inherited).Select(mapping => (prefix + mapping.Property, @event)).Concat(autoMap && @event.Syntax is EventSyntax syntax
            ? ProjectionAutoMap.Properties(
                mappings,
                targets,
                syntax.Properties,
                property => property.Name,
                (target, source) => target.Type.Name == source.Type.Name && target.Type.IsCollection == source.Type.IsCollection && (!source.Type.IsOptional || target.Type.IsOptional),
                isJoin)
                .Select(pair => (prefix + pair.Target.Name, @event))
            : []);
}
