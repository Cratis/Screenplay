// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal sealed partial class ActionSubjectResolver
{
    internal Dictionary<SyntaxNode, (SyntaxNode Effective, SyntaxNode? Legacy)> Repairs { get; } = [];

    void Remember(SyntaxNode authored, SyntaxNode effective, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope)
    {
        var command = authored is ScreenActionSyntax action ? action.Command : ((ToolbarItemSyntax)authored).Target;
        var original = authored is ScreenActionSyntax plain ? plain.Inputs.ToArray() : [.. ((ToolbarItemSyntax)authored).Parameters.Select(parameter => new InteractionArgumentSyntax(parameter.Name, parameter.Binding.Path, parameter.Location))];
        var properties = Properties(subjects, scope);
        var legacy = new List<InteractionArgumentSyntax>(original);
        var safe = subjects.Count == 1 && subjects[0].Type.IsCollection;
        foreach (var input in CommandInputs(command, scope).Where(input => !input.IsGenerated && !original.Any(argument => argument.Name == input.Name)))
        {
            var matches = properties.Where(property => string.Equals(property.Name, input.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length > 1) safe = false;
            if (matches.Length == 1) legacy.Add(new(input.Name, $"item.{matches[0].Name}", authored.Location));

            // An implicit key fallback would add a binding the legacy rule never supplied.
            if (matches.Length == 0 && EligibleIdentifier(input) && properties.Any(property => property.IsKey && SameConcept(input, property))) safe = false;
        }

        SyntaxNode? retained = safe ? authored switch
        {
            ScreenActionSyntax node => node with { Subject = ScreenActionSubjectKind.Item, Inputs = legacy },
            ToolbarItemSyntax node => node with
            {
                Subject = ScreenActionSubjectKind.Item,
                Parameters = [.. node.Parameters, .. legacy.Skip(original.Length).Select(input => new ScreenNavigationParameterSyntax(input.Name, new(UiBindingKind.DataContext, input.Binding, input.Location), input.Location))]
            },
            _ => null
        } : null;
        Repairs[authored] = (effective, retained);
    }
}
