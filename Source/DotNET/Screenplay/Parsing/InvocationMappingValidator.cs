// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks reaction invocation paths against the occurrence and command declarations without implying collection fan-out.
/// </summary>
/// <param name="application">The complete application.</param>
/// <param name="declarations">The scoped declarations.</param>
/// <param name="context">The diagnostic sink.</param>
internal sealed class InvocationMappingValidator(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
{
    public void Validate()
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
            {
                ValidateTrigger(trigger, scope);
            }
        }
    }

    static bool IsIntrinsic(string path)
    {
        if (path.StartsWith('$')) return true;
        if (path.Equals("now", StringComparison.Ordinal)) return true;
        return path.Equals("today", StringComparison.Ordinal);
    }

    void ValidateTrigger(ReactionTriggerSyntax trigger, DeclarationScope scope)
    {
        var sources = Sources(trigger, scope);
        foreach (var invocation in trigger.Invokes ?? [])
        {
            foreach (var mapping in invocation.Mappings)
            {
                var target = Target(invocation, mapping, scope);
                ValidateSource(mapping, target, trigger, sources);
            }
        }
    }

    void ValidateSource(PropertyMappingSyntax mapping, PropertySyntax? target, ReactionTriggerSyntax trigger, IEnumerable<PropertySyntax>? sources)
    {
        if (target is null) return;

        // An imported input type can still have an unknown enum or record shape.
        if (declarations.Compatible(target.Type, target.Type) is null) return;
        if (mapping.Source is not PathExpressionSyntax path) return;
        if (IsOtherSource(path.Path, target, trigger)) return;
        Resolve(sources, path.Path, mapping.Location, "Trigger");
    }

    PropertySyntax? Target(InvokesSyntax invocation, PropertyMappingSyntax mapping, DeclarationScope scope)
    {
        if (invocation.IsAbility)
        {
            var ability = application.Systems.SelectMany(system => system.Abilities.Where(ability => invocation.Command == $"{system.Name}.{ability.Name}")).FirstOrDefault();

            // Ability validation already owns unknown input diagnostics.
            return declarations.Property(ability?.Inputs, mapping.Property, out _);
        }

        var command = declarations.Resolve(invocation.Command, scope, owner => owner.Commands, command => command.Name)?.Node;
        return Resolve(command?.Properties, mapping.Property, mapping.Location, $"Command '{invocation.Command}'");
    }

    IEnumerable<PropertySyntax>? Sources(ReactionTriggerSyntax trigger, DeclarationScope scope)
    {
        if (trigger.Source is not NamedTriggerSourceSyntax named) return [];
        var occurrence = declarations.Event(named.Name, scope);
        if (occurrence is not null) return occurrence.Properties;
        var declared = DeclaredSources(named.Name);
        if (declared is not null) return declared;
        if (context.Languages.Triggers.TryGetValue(named.Name, out var registered) && registered.Values is { } values)
        {
            return values.Select(value => new PropertySyntax(value, new(string.Empty, false, false, named.Location), named.Location));
        }

        return null;
    }

    IEnumerable<PropertySyntax>? DeclaredSources(string name)
    {
        var declared = (application.Triggers ?? []).Where(candidate => candidate.Name == name).ToArray();
        return declared.Length == 1
            ? declared[0].Data.Select(datum => new PropertySyntax(datum.Name, datum.Type ?? new(string.Empty, false, false, datum.Location), datum.Location))
            : null;
    }

    bool IsOtherSource(string path, PropertySyntax target, ReactionTriggerSyntax trigger)
    {
        if (IsIntrinsic(path)) return true;
        var root = path.Split('.')[0];
        if ((trigger.Reads ?? []).Any(read => (read.Alias ?? read.ReadModel) == root)) return true;
        return IsEnumeration(path, target);
    }

    bool IsEnumeration(string path, PropertySyntax target) => application.Concepts.Any(concept =>
        concept.Name == target.Type.Name && concept.IsEnum &&
        (concept.Values.Contains(path) || concept.Values.Any(member => path == $"{concept.Name}.{member}")));

    PropertySyntax? Resolve(IEnumerable<PropertySyntax>? properties, string path, SourceLocation location, string owner)
    {
        var collection = CollectionParent(properties, path);
        if (collection is not null)
        {
            context.Error(DiagnosticCodes.InvalidInvocationMapping, $"{owner} path '{path}' traverses collection '{collection.Name}'; invocation mappings do not fan out collections.", location);
            return null;
        }

        var property = declarations.Property(properties, path, out var missing);
        if (missing)
        {
            context.Error(DiagnosticCodes.InvalidInvocationMapping, $"{owner} declares no field '{path}'.", location);
        }

        return property;
    }

    PropertySyntax? CollectionParent(IEnumerable<PropertySyntax>? properties, string path)
    {
        var segments = path.Split('.');
        for (var index = 1; index < segments.Length; index++)
        {
            var parent = declarations.Property(properties, string.Join('.', segments.Take(index)), out _);
            if (parent?.Type.IsCollection == true) return parent;
        }

        return null;
    }
}
