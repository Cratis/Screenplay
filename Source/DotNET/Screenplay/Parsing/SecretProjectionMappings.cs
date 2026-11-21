// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Walks one projection and reports every read-model property it copies a secret value into: an explicit mapping whose
/// target is typed with a secret concept or whose source is a secret event property, and a property AutoMap fills from
/// a same-named secret event property. Metadata, literals and aggregates copy no event value and are not reported.
/// </summary>
/// <param name="projection">The projection to walk.</param>
/// <param name="secrets">The names of the concepts marked <c>secret</c>.</param>
/// <param name="declarations">The declarations events and read models resolve against.</param>
/// <param name="scope">The scope of the slice declaring the projection.</param>
/// <param name="context">The context diagnostics are reported to.</param>
internal sealed class SecretProjectionMappings(
    ProjectionSyntax projection,
    HashSet<string> secrets,
    ConsistencyDeclarations declarations,
    DeclarationScope scope,
    ParserContext context)
{
    readonly HashSet<(string Property, SourceLocation Location)> _reported = [];

    public void Validate()
    {
        var autoMap = projection.AutoMap != AutoMapMode.Disabled;
        var shared = projection.Blocks.Where(block => block is not ProjectionVariantSyntax).ToList();
        var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToList();
        if (variants.Count == 0)
        {
            var readModel = projection.ReadModel ?? projection.Name;
            Walk(shared, declarations.ViewProperties(readModel, scope), readModel, autoMap);
        }

        foreach (var variant in variants)
        {
            Walk([.. shared, .. variant.Blocks], declarations.ViewProperties(variant.Name, scope), variant.Name, autoMap);
        }
    }

    // Joins inherit their current level; child and nested scopes inherit the projection's own setting.
    static bool Enabled(AutoMapMode mode, bool inherited) => mode switch
    {
        AutoMapMode.Enabled => true,
        AutoMapMode.Disabled => false,
        _ => inherited
    };

    void Walk(IEnumerable<ProjectionBlockSyntax> blocks, IEnumerable<PropertySyntax>? properties, string owner, bool autoMap)
    {
        if (properties is null) return;
        var targets = properties.ToList();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    var events = from.Events.Select(source => declarations.Event(source.Event, scope)).OfType<EventSyntax>().ToList();
                    Copies(from.Mappings, events, targets, owner, autoMap, false, from.Location);
                    break;
                case JoinSyntax join:
                    foreach (var joined in join.Events)
                    {
                        var joinedEvents = declarations.Event(joined.Event, scope) is { } joinedEvent ? [joinedEvent] : new List<EventSyntax>();
                        Copies(joined.Mappings, joinedEvents, targets, owner, Enabled(joined.AutoMap, autoMap), true, joined.Location);
                    }

                    break;
                case ChildrenSyntax children:
                    Element(children.Property, children.Blocks, Enabled(children.AutoMap, projection.AutoMap != AutoMapMode.Disabled), targets, owner);
                    break;
                case NestedSyntax nested:
                    Element(nested.Property, nested.Blocks, Enabled(nested.AutoMap, projection.AutoMap != AutoMapMode.Disabled), targets, owner);
                    break;
            }
        }
    }

    void Element(string path, IEnumerable<ProjectionBlockSyntax> blocks, bool autoMap, List<PropertySyntax> targets, string owner)
    {
        if (declarations.Property(targets, path, out _) is { } property)
        {
            Walk(blocks, declarations.TypeProperties(property.Type.Name), $"{owner}.{path}", autoMap);
        }
    }

    void Copies(IEnumerable<MappingSyntax> mappings, List<EventSyntax> events, List<PropertySyntax> targets, string owner, bool autoMap, bool isJoin, SourceLocation location)
    {
        var mappingList = mappings.ToList();
        foreach (var mapping in mappingList.OfType<SetMappingSyntax>())
        {
            Explicit(mapping, events, targets, owner);
        }

        if (!autoMap) return;
        foreach (var @event in events)
        {
            foreach (var (target, _) in ProjectionAutoMap.Properties(mappingList, targets, @event.Properties, property => property.Name, SameType, isJoin))
            {
                Report(target.Type.Name, $"{owner}.{target.Name}", location);
            }
        }
    }

    void Explicit(SetMappingSyntax mapping, List<EventSyntax> events, List<PropertySyntax> targets, string owner)
    {
        if (mapping.Source is not PathExpressionSyntax source || source.Path.StartsWith('$')) return;
        var concept = Secret(declarations.Property(targets, mapping.Property, out _))
            ?? events.Select(@event => Secret(declarations.Property(@event.Properties, source.Path, out _))).FirstOrDefault(name => name is not null);
        if (concept is not null) Report(concept, $"{owner}.{mapping.Property}", mapping.Location);
    }

    bool SameType(PropertySyntax target, PropertySyntax source) =>
        secrets.Contains(target.Type.Name) && target.Type.Name == source.Type.Name &&
        target.Type.IsCollection == source.Type.IsCollection && (!source.Type.IsOptional || target.Type.IsOptional);

    string? Secret(PropertySyntax? property) => property is not null && secrets.Contains(property.Type.Name) ? property.Type.Name : null;

    void Report(string concept, string property, SourceLocation location)
    {
        if (!_reported.Add((property, location))) return;
        context.Warning(
            DiagnosticCodes.SecretConceptProjected,
            SecretProjectionValidator.Message("Projection", projection.Name, concept, property),
            location);
    }
}
