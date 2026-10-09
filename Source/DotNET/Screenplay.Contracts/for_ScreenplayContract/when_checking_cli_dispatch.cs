// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_checking_cli_dispatch : Specification
{
    [Fact]
    void should_publish_every_command_and_option_in_the_tool_definition_table()
    {
        var commands = ScreenplayContract.CliCommands();
        commands.Select(command => command["name"].GetValue<string>()).ShouldEqual(CliCommandCatalog.All.Select(command => command.Name));
        foreach (var definition in CliCommandCatalog.All)
        {
            var command = commands.Single(command => command["name"].GetValue<string>() == definition.Name);
            command["options"].AsArray().Select(option => option.GetValue<string>()).ShouldEqual(definition.Options.Select(option => option.Name).Order(StringComparer.Ordinal));
            command["aliases"].AsArray().Select(alias => alias.GetValue<string>()).ShouldEqual(definition.Aliases.Order(StringComparer.Ordinal));
            command["usage"].GetValue<string>().ShouldEqual(definition.Usage);
        }
    }
}
