// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal sealed partial class ActionSubjectResolver
{
    static bool EligibleIdentifier(PropertySyntax input) => input.IsIdentifier && !input.IsGenerated && !input.Type.IsOptional && !input.Type.IsCollection;

    ScreenActionSubjectKind? Subject(ScreenActionSubjectKind? authored, string command, IEnumerable<string> bound, ActionSubjectContext context)
    {
        if (authored is not null) return authored;
        var inputs = CommandInputs(command, context.Scope);
        var properties = Properties(context.Data, context.Scope);
        var identifiers = inputs.Where(EligibleIdentifier).ToArray();
        var item = context.Data is [{ Type.IsCollection: true }] && identifiers is [var identifier] && Keys(identifier, properties).Length == 1;
        var inferred = item ? ScreenActionSubjectKind.Item : ScreenActionSubjectKind.Screen;
        _diagnostics.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.InferredActionSubject, $"Action '{command}' infers 'for {inferred.ToString().ToLowerInvariant()}' from its declared data and identifier types", context.Location));
        ReportMigration(command, bound, context, item);

        // Screen is the legacy omitted default; inference must not manufacture authored metadata.
        return item ? ScreenActionSubjectKind.Item : null;
    }

    void ReportMigration(string command, IEnumerable<string> bound, ActionSubjectContext context, bool item)
    {
        if (context.Data is not [{ Type.IsCollection: true }]) return;
        var properties = Properties(context.Data, context.Scope);
        var inputs = CommandInputs(command, context.Scope).Where(input => !input.IsGenerated && !bound.Contains(input.Name)).ToArray();
        var legacy = inputs.SelectMany(input => properties.Where(property => string.Equals(input.Name, property.Name, StringComparison.OrdinalIgnoreCase)).Select(property => $"{input.Name} from item.{property.Name}")).ToArray();
        var current = item ? inputs.Select(input => (Input: input, Property: Binding(input, properties))).Where(pair => pair.Property is not null).Select(pair => $"{pair.Input.Name} from item.{pair.Property!.Name}").ToArray() : [];
        if (!legacy.SequenceEqual(current))
        {
            _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.ChangedActionSubject, $"Action '{command}' now infers 'for {(item ? "item" : "screen")}'; legacy row-name matching would use {(legacy.Length == 0 ? "no row property" : string.Join(", ", legacy))}", context.Location));
        }
    }

    InteractionArgumentSyntax[] Inputs(string command, IEnumerable<InteractionArgumentSyntax> authored, ActionSubjectContext context)
    {
        var result = authored.ToList();
        if (context.Data.Count != 1) return [.. result];
        var properties = Properties(context.Data, context.Scope);
        foreach (var input in CommandInputs(command, context.Scope).Where(input => !input.IsGenerated))
        {
            if (result.Exists(argument => argument.Name == input.Name)) continue;
            if (Binding(input, properties) is { } property)
            {
                result.Add(new(input.Name, $"item.{property.Name}", context.Location));
            }
            else if (Keys(input, properties).Length > 1)
            {
                _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.AmbiguousActionInputBinding, $"Input '{input.Name}' of action '{command}' matches several declared key fields - write an explicit binding", context.Location));
            }
        }

        return [.. result];
    }

    PropertySyntax? Binding(PropertySyntax input, PropertySyntax[] properties)
    {
        var named = properties.Where(property => property.Name == input.Name && _declarations.Compatible(property.Type, input.Type) == true).ToArray();
        if (named is [var property]) return property;
        return Keys(input, properties) is [var key] ? key : null;
    }

    PropertySyntax[] Keys(PropertySyntax input, PropertySyntax[] properties) => EligibleIdentifier(input)
        ? [.. properties.Where(property => property.IsKey && SameConcept(input, property))]
        : [];

    PropertySyntax[] Properties(IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope) => subjects.Count == 1
        ? _declarations.ViewProperties(subjects[0].Type.Name, scope)?.ToArray() ?? []
        : [];

    PropertySyntax[] CommandInputs(string name, DeclarationScope scope) => _declarations.Resolve(name, scope, slice => slice.Commands, command => command.Name)?.Node.Properties.ToArray() ?? [];

    bool SameConcept(PropertySyntax input, PropertySyntax key) => !key.Type.IsCollection && !key.Type.IsOptional && input.Type.Name == key.Type.Name &&
        application.Concepts.Count(concept => concept.Name == input.Type.Name) == 1;
}
