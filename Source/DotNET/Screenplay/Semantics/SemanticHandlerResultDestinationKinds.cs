// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>Stable destination kinds for handler result entries.</summary>
public static class SemanticHandlerResultDestinationKinds
{
    /// <summary>The command's default destination.</summary>
    public const string Default = "default";

    /// <summary>A scalar command property.</summary>
    public const string Scalar = "scalar";

    /// <summary>An exact member of a command collection.</summary>
    public const string OneOf = "one-of";
}
