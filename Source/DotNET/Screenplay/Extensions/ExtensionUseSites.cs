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

    /// <summary>
    /// Checks a model.
    /// </summary>
    /// <param name="application">The parsed model, as written.</param>
    /// <returns>One diagnostic per incompatible use.</returns>
    public IReadOnlyList<Diagnostic> Check(ApplicationSyntax application)
    {
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
}
