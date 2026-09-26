// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents what caused a command or query to run - the link back through the chain of causes.
/// </summary>
/// <param name="Type">The type of the cause, such as <c>Command</c>, <c>Reactor</c> or <c>Schedule</c>.</param>
/// <param name="Occurred">When the cause occurred.</param>
/// <param name="Properties">The properties describing the cause, such as the name of the command that ran.</param>
public record Causation(string Type, DateTimeOffset Occurred, IReadOnlyDictionary<string, string> Properties)
{
    /// <summary>
    /// The absence of a known cause.
    /// </summary>
    public static readonly Causation NotSet = new(string.Empty, DateTimeOffset.MinValue, new Dictionary<string, string>());
}
