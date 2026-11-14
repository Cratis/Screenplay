// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines where a property of a collected row is read from.
/// </summary>
public enum SemanticGraphCollectMemberKind
{
    /// <summary>A property of the node the followed edge leads to.</summary>
    Property = 0,

    /// <summary>A property of the followed edge.</summary>
    EdgeProperty = 1
}
