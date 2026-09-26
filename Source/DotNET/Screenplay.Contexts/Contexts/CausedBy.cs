// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents the identity that caused a command or query to run.
/// </summary>
/// <param name="Subject">The subject of the identity - its stable identifier from the identity provider.</param>
/// <param name="Name">The display name of the identity.</param>
/// <param name="UserName">The user name of the identity.</param>
/// <remarks>
/// The three values match the <c>$causedBy</c> expression a projection can read, so what a handler sees and
/// what a projection can map are the same thing.
/// </remarks>
public record CausedBy(string Subject, string Name, string UserName)
{
    /// <summary>
    /// The absence of a known identity - an unauthenticated or system originated call.
    /// </summary>
    public static readonly CausedBy NotSet = new(string.Empty, string.Empty, string.Empty);
}
