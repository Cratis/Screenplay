// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents a single claim an identity carries - a named assertion about the caller.
/// </summary>
/// <param name="Name">The name of the claim, matching the name a <c>require claim "&lt;name&gt;"</c> condition uses.</param>
/// <param name="Value">The value of the claim.</param>
/// <remarks>
/// A claim is a name and a value rather than an entry in a dictionary because the same name may appear more
/// than once - a caller can hold several of the same claim, and collapsing them would lose the ones a rule
/// needs to see.
/// </remarks>
public record Claim(string Name, string Value);
