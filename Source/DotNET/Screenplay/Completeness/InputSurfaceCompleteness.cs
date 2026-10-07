// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

static class InputSurfaceCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var used = NavigationCompleteness.UsedScreens(application, declarations);
        var screens = declarations.Slices.SelectMany(entry => entry.Slice.Screens.Where(used.Contains).Select(screen => (Screen: screen, entry.Scope))).ToArray();
        var actions = screens.SelectMany(entry => ScreenContainers.All(entry.Screen.Directives).OfType<ScreenActionSyntax>()
            .Select(action => (Action: action, entry.Screen, entry.Scope, Command: Command(action.Command, entry.Scope, declarations)))).ToArray();
        var forms = application.Modules.SelectMany(module => (module.Forms ?? []).Select(form =>
            Command(form.For, new([module.Name]), declarations))).OfType<CommandSyntax>().ToHashSet();
        var executed = CompletenessInteractions.In(application, declarations)
            .Where(action => action.Action is ExecuteCommandActionSyntax && (action.Screen is null || used.Contains(action.Screen)))
            .Select(action => Command(action.Target, action.Scope, declarations)).OfType<CommandSyntax>().ToHashSet();
        var invoked = declarations.Slices.SelectMany(entry => entry.Slice.Reactions.SelectMany(reaction => reaction.Triggers)
            .SelectMany(trigger => trigger.Invokes ?? []).Select(invoke => Command(invoke.Command, entry.Scope, declarations)))
            .OfType<CommandSyntax>().ToHashSet();

        foreach (var entry in actions.Where(entry => entry.Command is not null))
        {
            var command = entry.Command!;
            var ownSlice = declarations.Slices.First(owner => owner.Slice.Commands.Contains(command));
            var ownScreen = ownSlice.Scope.Segments.SequenceEqual(entry.Scope.Segments, StringComparer.Ordinal);
            if (!ownScreen && !forms.Contains(command) && !command.Properties.All(property => property.IsGenerated))
            {
                // Command properties have no authored context-source member in the current syntax.
                // Context expressions in produces mappings do not make a caller-supplied property generated.
                yield return Diagnostic.Warning(
                    DiagnosticCodes.ActionWithoutInputSurface,
                    $"Action '{entry.Action.Command}' has no input surface - provide a command-bound form or issue the command on its input screen; action navigation happens after execution",
                    entry.Action.Location);
            }
        }

        var issued = actions.Select(action => action.Command).OfType<CommandSyntax>().ToHashSet();
        issued.UnionWith(executed);
        foreach (var command in declarations.Slices.Where(entry => entry.Slice.Type == SliceType.StateChange).SelectMany(entry => entry.Slice.Commands)
            .Where(command => !issued.Contains(command) && !invoked.Contains(command)))
        {
            yield return Diagnostic.Warning(
                DiagnosticCodes.CommandWithoutInputSurface,
                $"Command '{command.Name}' has no UI issuer - expose an action on an input screen, an action with a command-bound form or an attached behavior execute",
                command.Location);
        }
    }

    static CommandSyntax? Command(string name, DeclarationScope scope, ConsistencyDeclarations declarations) =>
        declarations.Resolve(name, scope, slice => slice.Commands, command => command.Name)?.Node;
}
