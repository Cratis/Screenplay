// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Resolves the components a screen places through a system's extension, with what the restored contracts say about them.
/// </summary>
/// <param name="syntax">The application whose systems name the extensions.</param>
/// <param name="resolution">The restored contracts, or <see langword="null"/> when none were resolved.</param>
internal sealed class UiExtensionIndex(ApplicationSyntax syntax, ExtensionResolution? resolution)
{
    readonly Dictionary<string, SystemSyntax> _systems = syntax.Systems
        .Where(system => system.Extension is not null)
        .ToDictionary(system => system.Name, StringComparer.Ordinal);

    /// <summary>
    /// Gets the extension a component is placed through.
    /// </summary>
    /// <param name="component">The placed component.</param>
    /// <returns>The <see cref="UiExtensionComponent"/>, or <see langword="null"/> when the component does not come from an extension.</returns>
    internal UiExtensionComponent? Component(ScreenComponentSyntax component)
    {
        if (component.Component.Split('.') is not [var systemName, var name] || !_systems.TryGetValue(systemName, out var system)) return null;
        var reference = system.Extension!.Reference;
        var handles = component.Behaviors
            .SelectMany(behavior => behavior.Bindings)
            .Where(binding => binding.Trigger is ApplicationTriggerInteractionTriggerSyntax)
            .Select(binding =>
            {
                var commands = Commands(binding.Actions.Concat(binding.Alternatives.SelectMany(alternative => alternative.Actions)).Concat(binding.Otherwise?.Actions ?? [])).Distinct(StringComparer.Ordinal).ToArray();
                return new UiSignalHandler(((ApplicationTriggerInteractionTriggerSyntax)binding.Trigger).TriggerName) { Commands = commands.Length == 0 ? null : commands };
            })
            .ToArray();
        var placement = new UiExtensionComponent(systemName, reference, name) { Handles = handles.Length == 0 ? null : handles };
        if (resolution is null || !resolution.Contracts.TryGetValue(reference, out var contract)) return placement;
        if (contract.Components.FirstOrDefault(candidate => candidate.Name == name) is not { } declared) return placement;
        return placement with
        {
            Kind = declared.Kind,
            Platforms = declared.Platforms.IsDefaultOrEmpty ? null : declared.Platforms,
            Placeholder = declared.Placeholder,
            PlaceholderHtml = declared.Placeholder is { } path && resolution.Folders.TryGetValue(reference, out var folder) ? ExtensionPlaceholder.TryRead(folder, path) : null
        };
    }

    /// <summary>
    /// Gets the extension a system names.
    /// </summary>
    /// <param name="systemName">The system name.</param>
    /// <returns>The <see cref="UiExtensionNeed"/>, or <see langword="null"/> when the system names no extension.</returns>
    internal UiExtensionNeed? Need(string systemName) =>
        _systems.TryGetValue(systemName, out var system) ? new(systemName, system.Extension!.Reference) : null;

    static IEnumerable<string> Commands(IEnumerable<InteractionActionSyntax> actions) =>
        actions.SelectMany(action => (action is ExecuteCommandActionSyntax execute ? [execute.Command] : Enumerable.Empty<string>())
            .Concat(Commands(action.OnSuccess.Concat(action.OnFailure).Concat(action.OnResult))));
}
