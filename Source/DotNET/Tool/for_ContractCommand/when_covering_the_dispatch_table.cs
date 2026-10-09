// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_covering_the_dispatch_table : Specification
{
    [Fact]
    void should_publish_every_command_the_tool_dispatches()
    {
        var contract = JsonNode.Parse(ScreenplayContract.Serialize());
        contract["cliCommands"].AsArray().Select(command => command["name"].GetValue<string>()).Order(StringComparer.Ordinal).ShouldEqual(ToolCommands.Dispatch.Keys.Select(command => command.Name).Order(StringComparer.Ordinal));
        foreach (var command in ToolCommands.Dispatch.Keys.Where(command => command.Name.Length > 0)) CliCommandCatalog.Resolve([command.Name]).ShouldEqual(command);
    }

    [Fact]
    void should_publish_mcp_rootless_startup_and_root_creation()
    {
        CliCommandCatalog.Mcp.Usage.ShouldContain("[<root-directory>]");
        CliCommandCatalog.Mcp.Options.ShouldContain(CliCommandCatalog.CreateRoot);
    }
}
