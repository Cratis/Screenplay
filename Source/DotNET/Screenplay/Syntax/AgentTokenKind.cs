// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>The token kinds an agents block can name.</summary>
public enum AgentTokenKind
{
    /// <summary>A personal token acting with its caller's policies.</summary>
    Personal = 0,

    /// <summary>An OAuth token, reserved and currently rejected.</summary>
    OAuth = 1
}
