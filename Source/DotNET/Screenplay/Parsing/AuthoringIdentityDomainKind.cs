// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// The kind of declaration a nominal identity domain resolves to.
/// </summary>
internal enum AuthoringIdentityDomainKind
{
    /// <summary>
    /// A built-in primitive type.
    /// </summary>
    Primitive = 0,

    /// <summary>
    /// A concept declaration.
    /// </summary>
    Concept = 1,

    /// <summary>
    /// A composite type declaration.
    /// </summary>
    Composite = 2
}
