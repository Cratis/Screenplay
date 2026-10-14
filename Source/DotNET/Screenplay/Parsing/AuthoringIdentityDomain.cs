// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// A nominal scalar domain, independent of field names and runtime values.
/// </summary>
internal sealed record AuthoringIdentityDomain(string Kind, string Primitive, string Target)
{
    internal string Name => Target.Length == 0 ? Primitive : Target;
}
