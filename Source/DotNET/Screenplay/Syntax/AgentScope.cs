// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>The minimum token scope required to call an exposed operation.</summary>
public enum AgentScope
{
    /// <summary>A read-scoped or write-scoped token may call the operation.</summary>
    Read = 0,

    /// <summary>Only a write-scoped token may call the operation.</summary>
    Write = 1
}
