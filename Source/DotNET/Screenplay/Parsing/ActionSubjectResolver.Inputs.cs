// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal sealed partial class ActionSubjectResolver
{
    static bool EligibleIdentifier(PropertySyntax input) => input.IsIdentifier && !input.IsGenerated && !input.Type.IsOptional && !input.Type.IsCollection;

    ScreenActionSubjectKind Subject(ScreenActionSubjectKind? authored, string commandName, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope, SourceLocation location)
    {
        if (authored is { } explicitSubject) return explicitSubject;
        var inputs = CommandInputs(commandName, scope);
        var properties = Properties(subjects, scope);
        var keys = properties.Where(property => property.IsKey).ToArray();
        var eligible = inputs.Where(input => EligibleIdentifier(input) && keys.Any(key => SameConcept(input, key))).ToArray();
        var inferred = subjects.Count == 1 && subjects[0].Type.IsCollection && eligible.Length == 1 && keys.Count(key => SameConcept(eligible[0], key)) == 1
            ? ScreenActionSubjectKind.Item
            : ScreenActionSubjectKind.Screen;
        _diagnostics.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.InferredActionSubject, $"Action '{commandName}' infers 'for {inferred.ToString().ToLowerInvariant()}' from its declared data and identifier types", location));
        if (subjects.Count == 1 && subjects[0].Type.IsCollection)
        {
            var legacy = inputs.SelectMany(input => properties.Where(property => string.Equals(input.Name, property.Name, StringComparison.OrdinalIgnoreCase)).Select(property => (Input: input, Property: property))).ToArray();
            if ((legacy.Length > 0) != (inferred == ScreenActionSubjectKind.Item))
            {
                var paths = string.Join(", ", legacy.Select(pair => $"{pair.Input.Name} from item.{pair.Property.Name}"));
                _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.ChangedActionSubject, $"Action '{commandName}' now infers 'for {inferred.ToString().ToLowerInvariant()}'; legacy row-name matching would use {(paths.Length == 0 ? "no row property" : paths)}", location));
            }
        }

        return inferred;
    }

    InteractionArgumentSyntax[] Inputs(string commandName, IEnumerable<InteractionArgumentSyntax> authored, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope, SourceLocation location)
    {
        var result = authored.ToList();
        if (subjects.Count != 1) return [.. result];
        var properties = Properties(subjects, scope);
        foreach (var input in CommandInputs(commandName, scope).Where(input => !input.IsGenerated))
        {
            if (result.Exists(argument => argument.Name == input.Name)) continue;
            var sameName = properties.Where(property => property.Name == input.Name).ToArray();
            if (sameName.Length == 1)
            {
                result.Add(new(input.Name, $"item.{sameName[0].Name}", location));
                continue;
            }

            if (!EligibleIdentifier(input)) continue;
            var keys = properties.Where(property => property.IsKey && SameConcept(input, property)).ToArray();
            if (keys.Length == 1) result.Add(new(input.Name, $"item.{keys[0].Name}", location));
            else if (keys.Length > 1) _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.AmbiguousActionInputBinding, $"Input '{input.Name}' of action '{commandName}' matches several declared key fields - write an explicit binding", location));
        }

        return [.. result];
    }

    PropertySyntax[] Properties(IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope) => subjects.Count == 1
        ? _declarations.ViewProperties(subjects[0].Type.Name, scope)?.ToArray() ?? []
        : [];

    PropertySyntax[] CommandInputs(string name, DeclarationScope scope) => _declarations.Resolve(name, scope, slice => slice.Commands, command => command.Name)?.Node.Properties.ToArray() ?? [];

    bool SameConcept(PropertySyntax input, PropertySyntax key) => !key.Type.IsCollection && !key.Type.IsOptional && input.Type.Name == key.Type.Name &&
        application.Concepts.Count(concept => concept.Name == input.Type.Name) == 1;
}
