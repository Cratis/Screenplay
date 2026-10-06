// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.UnixLinks;

// Platform-dependent link facts are skipped at discovery, not counted as successful no-ops.
internal sealed class FactAttribute : Xunit.FactAttribute
{
    public FactAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Unix link setup requires a Unix host; Windows link privileges are not assumed.";
        }
    }
}
