// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

// Which way a lineage trace goes from its subject.
[Flags]
enum McpLineageDirection
{
    None = 0,
    Upstream = 1,
    Downstream = 2,
    Both = Upstream | Downstream
}
