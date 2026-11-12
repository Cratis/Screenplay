// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Reports every use of a system's extension that the resolved contract does not offer, so swapping an extension for one that
/// does not offer the same members is reported at each use.
/// </summary>
/// <param name="resolution">The <see cref="ExtensionResolution"/> to check against.</param>
public sealed class ExtensionUseSites(ExtensionResolution resolution) : ScreenplaySyntaxWalker
{
    readonly List<Diagnostic> _diagnostics = [];
    Dictionary<string, SystemSyntax> _systems = [];
    ApplicationSyntax _application = null!;

    /// <summary>
    /// Checks a model.
    /// </summary>
    /// <param name="application">The parsed model, as written.</param>
    /// <returns>One diagnostic per incompatible use.</returns>
    public IReadOnlyList<Diagnostic> Check(ApplicationSyntax application)
    {
        _application = application;
        _systems = application.Systems.Where(system => system.Extension is not null).ToDictionary(system => system.Name, StringComparer.Ordinal);
        if (_systems.Count > 0 && !resolution.Contracts.IsEmpty) VisitApplication(application);
        return _diagnostics;
    }

    /// <inheritdoc/>
    public override void VisitReactionTrigger(ReactionTriggerSyntax syntax)
    {
        if (syntax.Source is NamedTriggerSourceSyntax named && named.Name.Split('.') is [var systemName, var triggerName]
            && _systems.TryGetValue(systemName, out var system)
            && resolution.Contracts.TryGetValue(system.Extension!.Reference, out var contract)
            && !contract.Triggers.Any(trigger => trigger.Name == triggerName))
        {
            _diagnostics.Add(Diagnostic.Error(DiagnosticCodes.ExtensionIncompatibleUse, $"Extension '{system.Extension.Reference}' does not offer trigger '{triggerName}' used as '{named.Name}'.", named.Location));
        }

        base.VisitReactionTrigger(syntax);
    }

    /// <inheritdoc/>
    public override void VisitApplicationTriggerInteractionTrigger(ApplicationTriggerInteractionTriggerSyntax syntax)
    {
        if (TryFindContract(syntax.TriggerName.Split('.') is [var systemName, _] ? systemName : null, out var system, out var contract)
            && !contract.Triggers.Any(trigger => trigger.Name == syntax.TriggerName.Split('.')[1]))
        {
            _diagnostics.Add(Diagnostic.Error(DiagnosticCodes.ExtensionIncompatibleUse, $"Extension '{system.Extension!.Reference}' does not offer trigger '{syntax.TriggerName.Split('.')[1]}' used as '{syntax.TriggerName}'.", syntax.Location));
        }

        base.VisitApplicationTriggerInteractionTrigger(syntax);
    }

    /// <inheritdoc/>
    public override void VisitScreenComponent(ScreenComponentSyntax syntax)
    {
        if (syntax.Component.Split('.') is [var systemName, var componentName] && TryFindContract(systemName, out var system, out var contract))
        {
            var component = contract.Components.FirstOrDefault(candidate => candidate.Name == componentName);
            if (component is null)
            {
                _diagnostics.Add(Diagnostic.Error(DiagnosticCodes.UnknownExtensionComponent, $"Extension '{system.Extension!.Reference}' declares no component '{componentName}', placed as '{syntax.Component}'.", syntax.Location));
            }
            else
            {
                CheckComponent(syntax, component);
            }
        }

        base.VisitScreenComponent(syntax);
    }

    bool TryFindContract(string? systemName, out SystemSyntax system, out ExtensionContract contract)
    {
        system = null!;
        contract = null!;
        return systemName is not null
            && _systems.TryGetValue(systemName, out system!)
            && resolution.Contracts.TryGetValue(system.Extension!.Reference, out contract!);
    }

    void CheckComponent(ScreenComponentSyntax placement, ExtensionComponent component)
    {
        foreach (var property in placement.Properties)
        {
            var name = property.Property.Split('.')[0];
            if (!component.Properties.Any(declared => declared.Name == name))
            {
                _diagnostics.Add(Diagnostic.Error(DiagnosticCodes.UnknownExtensionComponentProperty, $"Component '{placement.Component}' has no property '{name}'.", property.Location));
            }
        }

        foreach (var behavior in placement.Behaviors)
        {
            foreach (var binding in behavior.Bindings)
            {
                CheckSignalBinding(placement, component, binding);
            }
        }

        if (!component.Platforms.IsDefaultOrEmpty)
        {
            foreach (var platform in _application.UiProfiles?.SelectMany(profile => profile.Platforms).Distinct(StringComparer.Ordinal) ?? [])
            {
                if (!component.Platforms.Contains(platform, StringComparer.Ordinal))
                {
                    _diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.ExtensionComponentPlatformUnsupported, $"Component '{placement.Component}' does not support platform '{platform}', which a ui profile targets.", placement.Location));
                }
            }
        }
    }

    void CheckSignalBinding(ScreenComponentSyntax placement, ExtensionComponent component, InteractionBindingSyntax binding)
    {
        static IEnumerable<InteractionArgumentSyntax> Arguments(IEnumerable<InteractionActionSyntax> actions) =>
            actions.SelectMany(action => action.Arguments.Concat(Arguments(action.OnSuccess.Concat(action.OnFailure).Concat(action.OnResult))));

        ExtensionSignal? signal = null;
        if (binding.Trigger is ApplicationTriggerInteractionTriggerSyntax application && !application.TriggerName.Contains('.'))
        {
            signal = component.Signals.FirstOrDefault(candidate => candidate.Name == application.TriggerName);
            if (signal is null && !(_application.Triggers ?? []).Any(declared => declared.Name == application.TriggerName))
            {
                _diagnostics.Add(Diagnostic.Error(DiagnosticCodes.UnknownExtensionSignal, $"Component '{placement.Component}' raises no signal '{application.TriggerName}'.", application.Location));
            }
        }

        foreach (var argument in Arguments(binding.Actions.Concat(binding.Alternatives.SelectMany(alternative => alternative.Actions)).Concat(binding.Otherwise?.Actions ?? [])))
        {
            if (argument.Binding.Split('.') is not ["signal", var value, ..]) continue;
            if (signal?.Values.Any(carried => carried.Name == value) != true)
            {
                var detail = signal is null ? "the interaction handles no signal" : $"signal '{signal.Name}' carries no value '{value}'";
                _diagnostics.Add(Diagnostic.Error(DiagnosticCodes.UnknownExtensionSignalValue, $"'{argument.Binding}' reads a signal value, but {detail}.", argument.Location));
            }
        }
    }
}
