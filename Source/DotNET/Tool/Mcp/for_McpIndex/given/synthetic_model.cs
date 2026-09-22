// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp.for_McpIndex.given;

static class synthetic_model
{
    internal static McpSnapshot Create(int count) => new([.. Enumerable.Range(0, count).Select(index => Document($"slice{index}", $$"""
        module Billing
          feature Accounts
            slice StateChange Register{{index}}
              command Register
                produces Opened
              event Opened
        """))]);

    internal static WorkspaceDocument Document(string key, string source) => WorkspaceDocument.Create(key, PortablePlayPath.Parse($"{key}.play"), Encoding.UTF8.GetBytes(source));
}
