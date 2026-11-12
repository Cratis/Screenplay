// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Decides whether one extension can stand in for another.
/// </summary>
/// <remarks>
/// An extension replaces another only when it offers the same abilities, components, properties and raised signals: every member the
/// replaced contract offers is offered again with the same typed shape. The replacing contract may offer more.
/// </remarks>
public static class ExtensionCompatibility
{
    /// <summary>
    /// Lists every way <paramref name="offered"/> falls short of <paramref name="required"/>.
    /// </summary>
    /// <param name="required">The contract the model was written against.</param>
    /// <param name="offered">The contract that replaces it.</param>
    /// <returns>One message per missing or different member; empty when compatible.</returns>
    public static IReadOnlyList<string> Mismatches(ExtensionContract required, ExtensionContract offered)
    {
        var found = new List<string>();
        foreach (var ability in required.Abilities)
        {
            var other = offered.Abilities.FirstOrDefault(candidate => candidate.Name == ability.Name);
            if (other is null) found.Add($"ability '{ability.Name}' is not offered");
            else if (Shape(ability.Inputs) != Shape(other.Inputs) || Shape(ability.Returns) != Shape(other.Returns)) found.Add($"ability '{ability.Name}' has different inputs or returns");
        }

        foreach (var trigger in required.Triggers)
        {
            var other = offered.Triggers.FirstOrDefault(candidate => candidate.Name == trigger.Name);
            if (other is null) found.Add($"trigger '{trigger.Name}' is not offered");
            else if (Shape(trigger.Payload) != Shape(other.Payload)) found.Add($"trigger '{trigger.Name}' has a different payload");
        }

        foreach (var component in required.Components)
        {
            var other = offered.Components.FirstOrDefault(candidate => candidate.Name == component.Name);
            if (other is null)
            {
                found.Add($"component '{component.Name}' is not offered");
                continue;
            }

            if (Shape(component.Properties) != Shape(other.Properties)) found.Add($"component '{component.Name}' has different properties");
            foreach (var signal in component.Signals)
            {
                var raised = other.Signals.FirstOrDefault(candidate => candidate.Name == signal.Name);
                if (raised is null) found.Add($"component '{component.Name}' does not raise '{signal.Name}'");
                else if (Shape(signal.Values) != Shape(raised.Values)) found.Add($"component '{component.Name}' raises '{signal.Name}' with different values");
            }

            foreach (var platform in component.Platforms.Where(platform => !other.Platforms.Contains(platform))) found.Add($"component '{component.Name}' does not support platform '{platform}'");
        }

        return found;
    }

    static string Shape(IEnumerable<PropertySyntax> properties) =>
        string.Join(";", properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => $"{property.Name}:{property.Type}"));
}
