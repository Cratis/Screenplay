// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>The token scope required for an exposed operation.</summary>
public enum SemanticAgentScope
{
    /// <summary>A read or write token may call the operation.</summary>
    Read,

    /// <summary>Only a write token may call the operation.</summary>
    Write
}
