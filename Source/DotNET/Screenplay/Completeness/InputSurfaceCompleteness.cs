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
        var (_, _, actions) = Surfaces(application, declarations);
        var forms = application.Modules.SelectMany(module => (module.Forms ?? []).Select(form =>
            Command(form.For, new([module.Name]), declarations))).OfType<CommandSyntax>().ToHashSet();
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
                    $"Action '{entry.Action.Command}' has no input surface - provide a command-bound form or issue the command on its input screen",
                    entry.Action.Location);
            }
        }
    }

    /// <summary>
    /// Finds the State Change commands nothing issues - no action, guarded action, attached execute, reaction or hook. This check is
    /// on by default: the compiler runs it on every model.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to check.</param>
    /// <param name="declarations">The <see cref="ConsistencyDeclarations"/> of the application.</param>
    /// <returns>A <see cref="DiagnosticCodes.CommandWithoutInputSurface"/> warning per command nothing issues.</returns>
    internal static IEnumerable<Diagnostic> Unissued(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var (used, screens, actions) = Surfaces(application, declarations);
        var executed = CompletenessInteractions.In(application, declarations)
            .Where(action => action.Action is ExecuteCommandActionSyntax && (action.Screen is null || used.Contains(action.Screen)))
            .Select(action => Command(action.Target, action.Scope, declarations)).OfType<CommandSyntax>().ToHashSet();
        var invoked = declarations.Slices.SelectMany(entry => entry.Slice.Reactions.SelectMany(reaction => reaction.Triggers)
            .SelectMany(trigger => trigger.Invokes ?? []).Select(invoke => Command(invoke.Command, entry.Scope, declarations)))
            .OfType<CommandSyntax>().ToHashSet();
        var issued = actions.Select(action => action.Command).OfType<CommandSyntax>().ToHashSet();
        issued.UnionWith(screens.SelectMany(entry => ScreenContainers.All(entry.Screen.Directives).OfType<ScreenGuardedActionSyntax>()
            .SelectMany(action => action.Alternatives.Select(alternative => alternative.Command)
                .Concat(action.Otherwise?.Command is { } fallback ? [fallback] : []))
            .Select(name => Command(name, entry.Scope, declarations))).OfType<CommandSyntax>());
        issued.UnionWith(executed);
        issued.UnionWith(Hooked(application));
        foreach (var command in declarations.Slices.Where(entry => entry.Slice.Type == SliceType.StateChange).SelectMany(entry => entry.Slice.Commands)
            .Where(command => !issued.Contains(command) && !invoked.Contains(command)))
        {
            yield return Diagnostic.Warning(
                DiagnosticCodes.CommandWithoutInputSurface,
                $"Command '{command.Name}' has no UI issuer - expose an action on an input screen, a hook that runs or opens its slice, an action with a command-bound form or an attached behavior execute",
                command.Location);
        }
    }

    // A hook issues its slice's commands: directly when it runs, or through the slice's own screen when it opens one.
    static IEnumerable<CommandSyntax> Hooked(ApplicationSyntax application)
    {
        var collector = new HookCollector();
        collector.VisitApplication(application);
        var slices = ToolbarButtonValidator.AllSlices(application);
        foreach (var button in collector.Hooks)
        {
            // The same resolution the hook checks use, so a hook naming its slice by its full path counts as an issuer.
            if (HookValidator.ResolveSlice(slices, button.Slice!) is { } slice && (button.Opens == HookOpening.Runs || slice.Screens.Any()
                || (button.Opens is null && slice.Commands.All(command => HookValidator.UnresolvedInput(command, button.Subject) is null))))
            {
                foreach (var command in slice.Commands)
                {
                    yield return command;
                }
            }
        }
    }

    static (IReadOnlySet<ScreenSyntax> Used, (ScreenSyntax Screen, DeclarationScope Scope)[] Screens, (ScreenActionSyntax Action, ScreenSyntax Screen, DeclarationScope Scope, CommandSyntax? Command)[] Actions) Surfaces(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var used = NavigationCompleteness.UsedScreens(application, declarations);
        var screens = declarations.Slices.SelectMany(entry => entry.Slice.Screens.Where(used.Contains).Select(screen => (Screen: screen, entry.Scope))).ToArray();
        var actions = screens.SelectMany(entry => ScreenContainers.All(entry.Screen.Directives).OfType<ScreenActionSyntax>()
            .Select(action => (Action: action, entry.Screen, entry.Scope, Command: Command(action.Command, entry.Scope, declarations)))).ToArray();
        return (used, screens, actions);
    }

    static CommandSyntax? Command(string name, DeclarationScope scope, ConsistencyDeclarations declarations) =>
        declarations.Resolve(name, scope, slice => slice.Commands, command => command.Name)?.Node;

    sealed class HookCollector : ScreenplaySyntaxWalker
    {
        public List<ToolbarButtonSyntax> Hooks { get; } = [];

        public override void VisitToolbarButton(ToolbarButtonSyntax syntax)
        {
            if (syntax.Slice is not null)
            {
                Hooks.Add(syntax);
            }

            base.VisitToolbarButton(syntax);
        }
    }
}
