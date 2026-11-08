// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Reads the commands a screen issues and the places its actions, behaviors and buttons lead to.
/// </summary>
internal sealed partial class UiScreenReader
{
    void Action(ScreenActionSyntax action)
    {
        var added = Command(action.Command, action.Label ?? action.Command, action.Subject, action.Inputs, null);
        if (action.Navigate is { } navigate)
        {
            _actions[^1] = added with { Then = navigate.Screen };
            _navigation.Add(UiScreenText.Navigation(navigate, "action"));
        }
    }

    void Guarded(ScreenGuardedActionSyntax guarded)
    {
        foreach (var alternative in guarded.Alternatives)
        {
            Command(alternative.Command, guarded.Label, guarded.Subject, alternative.Arguments, ScreenplaySyntaxText.Condition(alternative.Condition));
        }

        if (guarded.Otherwise is { Command: { } otherwise } fallback) Command(otherwise, guarded.Label, guarded.Subject, fallback.Arguments, "otherwise");
        if (guarded.Navigate is { } navigate) _navigation.Add(UiScreenText.Navigation(navigate, "action"));
    }

    UiAction Command(string name, string label, ScreenActionSubjectKind? subject, IEnumerable<InteractionArgumentSyntax> arguments, string? when)
    {
        var bindings = arguments.ToDictionary(argument => argument.Name, argument => argument.Binding, StringComparer.Ordinal);
        var found = index.Command(slice, name);
        var inputs = found is { } command
            ? command.Command.Properties.Where(property => !property.IsGenerated)
                .Select(property => new UiInput(property.Name, UiScreenText.TypeName(property.Type), bindings.GetValueOrDefault(property.Name)))
            : bindings.Select(binding => new UiInput(binding.Key, string.Empty, binding.Value));
        var operation = found is { } declared ? index.Operation(declared.Slice, name) : null;
        var action = new UiAction(name, label, UiScreenText.Subject(subject), [.. inputs])
        {
            When = when,
            CommandId = operation?.Id.ToString(),
            Route = operation?.Route
        };
        _actions.Add(action);
        return action;
    }

    void Behaviors(IEnumerable<BehaviorSyntax> behaviors, ScreenActionSubjectKind? subject)
    {
        foreach (var binding in behaviors.SelectMany(behavior => behavior.Bindings))
        {
            var actions = binding.Actions
                .Concat(binding.Alternatives.SelectMany(alternative => alternative.Actions))
                .Concat(binding.Otherwise?.Actions ?? []);
            Interactions(actions, UiScreenText.Trigger(binding.Trigger), subject);
        }
    }

    void Interactions(IEnumerable<InteractionActionSyntax> actions, string trigger, ScreenActionSubjectKind? subject)
    {
        foreach (var action in actions)
        {
            if (action is NavigateActionSyntax navigate)
            {
                _navigation.Add(new(navigate.Screen, trigger) { By = navigate.Arguments.FirstOrDefault()?.Binding });
            }

            if (action is ExecuteCommandActionSyntax execute) Command(execute.Command, execute.Command, subject, execute.Arguments, null);
            Interactions(action.OnSuccess.Concat(action.OnFailure).Concat(action.OnResult), trigger, subject);
        }
    }

    void Toolbar(ScreenToolbarSyntax toolbar)
    {
        _scene.Toolbar(toolbar, Button);
        foreach (var item in toolbar.Items)
        {
            if (item.Kind == ToolbarItemKind.Action) Command(item.Target, item.Label ?? item.Target, item.Subject, [], null);
            else _navigation.Add(new(item.Target, "toolbar") { Opens = item.Kind == ToolbarItemKind.Dialog ? "dialog" : null });
        }
    }

    void Button(ToolbarButtonSyntax button)
    {
        var command = button.Runs ?? (button.Opens == HookOpening.Runs && button.Slice is { } runs ? index.CommandOfSlice(runs) : null);
        if (command is not null)
        {
            Command(command, button.Title, button.Subject, [], null);
            return;
        }

        if (button.Slice is { } target) _navigation.Add(new(target, "button") { Opens = UiScreenText.Opens(button) });
    }
}
