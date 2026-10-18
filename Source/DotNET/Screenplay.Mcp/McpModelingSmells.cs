// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpModelingSmells
{
    static readonly string[] _eventSuffixes = ["Updated", "Changed", "Edited", "Saved", "Modified", "Deleted", "Synced", "Received"];
    static readonly string[] _commandPrefixes = ["Update", "Edit", "Save", "Set", "Manage", "Get", "Load", "Fetch"];

    // Production and capture mappings, seed values and specification given/append values write an event property; every other indexed link, resolved or not, reads it.
    static readonly string[] _writeRoles = ["mapping-target", "fixture"];

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
        var read = Read(index.PropertyLinks);
        var opaque = OpaqueConsumed(index);
        foreach (var @event in selected.Where(declaration => declaration.Syntax is EventSyntax { Visibility: not EventVisibility.Public } && !Superseded(index, declaration) && !opaque.Contains(declaration.Syntax)))
        {
            foreach (var property in ((EventSyntax)@event.Syntax).Properties.Where(property => !read.Contains(property)))
            {
                Add("SMELL006", @event, $"Is '{@event.Name}.{property.Name}' part of the business fact, although no projection, reducer, reaction, condition or specification reads it?", property.Name);
            }
        }

        return McpReviewSelection.Page(snapshot, findings, arguments, "Advisory questions only; never compiler diagnostics and never part of --warnaserror. No suppression syntax or persistence is implemented. Names are case-sensitive. Shapes compare nonempty sets of declared property names/types/modifiers, not business meaning. Fan-out counts distinct resolved event targets, not occurrences or operations. Fan-in includes explicit mappings, known-event automap, every/all, joins, nested/child paths and variants; through all, only events whose payload supplies the value count. Opaque code and unresolved references are not inferred. Unread event properties count any indexed reference other than a production, capture, seed or given write as a read, including automap, opaque code, comments and unclassified name matches; superseded generations and public contract events are skipped, as are events handled by a reducer rule or reaction with file or inline code, because the index cannot see into it. Related evidence is capped at 20 with complete counts.");
    }

    static string[] Shape(EventSyntax syntax) => [.. syntax.Properties.Select(property => $"{property.Name}:{property.Type.Name}:{property.Type.IsCollection}:{property.Type.IsOptional}").Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    static bool Superseded(McpSyntaxIndex index, McpDeclaration @event) =>
        index.Find(@event.Address, @event.Kind).Any(other => other.Syntax is EventSyntax newer && newer.Generation > ((EventSyntax)@event.Syntax).Generation);

    // A consumer the index cannot see into (a reducer rule or reaction trigger with a file or inline code body) may read any property of the event it handles.
    static HashSet<SyntaxNode> OpaqueConsumed(McpSyntaxIndex index)
    {
        var consumed = new HashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);
        foreach (var declaration in index.Declarations)
        {
            var names = declaration.Syntax switch
            {
                ReducerSyntax reducer => reducer.Rules.Where(rule => rule.File is not null || rule.Code is not null).Select(rule => rule.Event),
                ReactionSyntax reaction => reaction.Triggers.Where(trigger => trigger.File is not null || trigger.Code is not null).Select(trigger => trigger.Source).OfType<NamedTriggerSourceSyntax>().Select(source => source.Name),
                _ => []
            };
            foreach (var name in names)
            {
                // An unresolved or ambiguous name cannot be proven, so every candidate counts as consumed.
                foreach (var candidate in index.Resolve(new(name, ["Event"], declaration.Scope, declaration.Location, "review", declaration.Owner))) consumed.Add(candidate.Syntax);
            }
        }

        return consumed;
    }

    static HashSet<SyntaxNode> Read(WorkspacePropertyLinks links) =>
        links.Links.Where(link => !_writeRoles.Contains(link.Reference.Role, StringComparer.Ordinal)).SelectMany(link => link.Candidates).Select(candidate => candidate.Syntax).ToHashSet<SyntaxNode>(ReferenceEqualityComparer.Instance);

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
                            foreach (var feed in Mapped(@event, joined.Mappings, mappings, Mode(joined.AutoMap, autoMap), prefix, targets, true)) yield return feed;
                        }
                    }
                    break;
                case AllSyntax all:
                    foreach (var @event in index.Declarations.Where(declaration => declaration.Syntax is EventSyntax && index.Find(declaration.Address, declaration.Kind).Length == 1))
                    {
                        // all reaches every event type, so an event feeds a property only when its own payload supplies the value.
                        foreach (var feed in Mapped(@event, all.Mappings, mappings, Mode(all.AutoMap, autoMap), prefix, targets, false, mapping => Reads(mapping, (EventSyntax)@event.Syntax))) yield return feed;
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

    static bool Reads(MappingSyntax mapping, EventSyntax @event) =>
        Paths(mapping).Any(path => @event.Properties.Any(property => property.Name == path.Split('.')[0]));

    static IEnumerable<string> Paths(MappingSyntax mapping) => mapping switch
    {
        SetMappingSyntax set => Paths(set.Source),
        AddMappingSyntax add => Paths(add.Value),
        SubtractMappingSyntax subtract => Paths(subtract.Value),
        _ => []
    };

    static IEnumerable<string> Paths(ExpressionSyntax expression) => expression switch
    {
        PathExpressionSyntax path => [path.Path],
        TemplateExpressionSyntax template => template.Parts.OfType<TemplateInterpolationSyntax>().SelectMany(part => Paths(part.Expression)),
        ListExpressionSyntax list => list.Items.SelectMany(Paths),
        ObjectExpressionSyntax value => value.Members.SelectMany(member => Paths(member.Value)),
        _ => []
    };

    static bool Mode(AutoMapMode mode, bool inherited) => mode == AutoMapMode.Inherit ? inherited : mode == AutoMapMode.Enabled;

    static McpDeclaration? ResolveEvent(McpSyntaxIndex index, McpDeclaration from, string name) => index.Resolve(new(name, ["Event"], from.Scope, from.Location, "review", from.Owner)) is [var @event] ? @event : null;

    static IEnumerable<PropertySyntax> ElementProperties(McpSyntaxIndex index, McpDeclaration projection, IEnumerable<PropertySyntax> targets, string name) =>
        targets.FirstOrDefault(property => property.Name == name) is { } property && index.Resolve(new(property.Type.Name, ["Type"], projection.Scope, property.Location, "review", projection.Owner)) is [var type] && type.Syntax is TypeSyntax syntax ? syntax.Properties : [];

    static IEnumerable<(string Property, McpDeclaration Event)> Mapped(McpDeclaration @event, IEnumerable<MappingSyntax> mappings, IEnumerable<MappingSyntax> inherited, bool autoMap, string prefix, IEnumerable<PropertySyntax> targets, bool isJoin, Func<MappingSyntax, bool>? feeds = null) =>
        mappings.Concat(inherited).Where(mapping => feeds?.Invoke(mapping) ?? true).Select(mapping => (prefix + mapping.Property, @event)).Concat(autoMap && @event.Syntax is EventSyntax syntax
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
