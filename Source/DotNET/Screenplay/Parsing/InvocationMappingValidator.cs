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

    void ValidateTrigger(ReactionTriggerSyntax trigger, DeclarationScope scope)
    {
        var sources = Sources(trigger, scope);
        foreach (var invocation in trigger.Invokes ?? [])
        {
            var command = invocation.IsAbility ? null : declarations.Resolve(invocation.Command, scope, owner => owner.Commands, command => command.Name)?.Node;
            foreach (var mapping in invocation.Mappings)
            {
                var target = Resolve(command?.Properties, mapping.Property, mapping.Location, $"Command '{invocation.Command}'");
                if (mapping.Source is PathExpressionSyntax path && !IsOtherSource(path.Path, target, trigger))
                {
                    Resolve(sources, path.Path, mapping.Location, "Trigger");
                }
            }
        }
    }

    IEnumerable<PropertySyntax>? Sources(ReactionTriggerSyntax trigger, DeclarationScope scope)
    {
        if (trigger.Source is not NamedTriggerSourceSyntax named) return [];
        var occurrence = declarations.Event(named.Name, scope);
        if (occurrence is not null) return occurrence.Properties;
        var declared = (application.Triggers ?? []).Where(candidate => candidate.Name == named.Name).ToArray();
        if (declared.Length == 1)
        {
            return declared[0].Data.Select(datum => new PropertySyntax(datum.Name, datum.Type ?? new(string.Empty, false, false, datum.Location), datum.Location));
        }

        if (context.Languages.Triggers.TryGetValue(named.Name, out var registered) && registered.Values is { } values)
        {
            return values.Select(value => new PropertySyntax(value, new(string.Empty, false, false, named.Location), named.Location));
        }

        return null;
    }

    bool IsOtherSource(string path, PropertySyntax? target, ReactionTriggerSyntax trigger)
    {
        if (path.StartsWith('$') || path.Equals("now", StringComparison.Ordinal) || path.Equals("today", StringComparison.Ordinal)) return true;
        var root = path.Split('.')[0];
        if ((trigger.Reads ?? []).Any(read => (read.Alias ?? read.ReadModel) == root)) return true;
        return target is not null && IsEnumeration(path, target);
    }

    bool IsEnumeration(string path, PropertySyntax target) => application.Concepts.Any(concept =>
        concept.Name == target.Type.Name && concept.IsEnum &&
        (concept.Values.Contains(path) || concept.Values.Any(member => path == $"{concept.Name}.{member}")));

    PropertySyntax? Resolve(IEnumerable<PropertySyntax>? properties, string path, SourceLocation location, string owner)
    {
        var segments = path.Split('.');
        for (var index = 1; index < segments.Length; index++)
        {
            var parent = declarations.Property(properties, string.Join('.', segments.Take(index)), out _);
            if (parent?.Type.IsCollection == true)
            {
                context.Error(DiagnosticCodes.InvalidInvocationMapping, $"{owner} path '{path}' traverses collection '{parent.Name}'; invocation mappings do not fan out collections.", location);
                return null;
            }
        }

        var property = declarations.Property(properties, path, out var missing);
        if (missing)
        {
            context.Error(DiagnosticCodes.InvalidInvocationMapping, $"{owner} declares no field '{path}'.", location);
        }

        return property;
    }
}
