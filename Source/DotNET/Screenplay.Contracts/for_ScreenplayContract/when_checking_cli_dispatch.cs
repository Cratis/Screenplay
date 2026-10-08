// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_checking_cli_dispatch : Specification
{
    string[] _missingOptions;
    string[] _missingCommands;

    void Because()
    {
        var commands = ScreenplayContract.CliCommands();
        var documentedOptions = commands.SelectMany(command => (command["options"] ?? command["aliases"])?.AsArray().Select(value => value.GetValue<string>()) ?? []).ToHashSet(StringComparer.Ordinal);
        var recognizedOptions = ContractSources.All.Where(entry => entry.Key.StartsWith("cli/", StringComparison.Ordinal)).SelectMany(entry => ContractSources.Matches(entry.Value, "\"(--[a-z][a-z-]*)\""));
        _missingOptions = [.. recognizedOptions.Except(documentedOptions, StringComparer.Ordinal)];
        var dispatchedCommands = ContractSources.Matches(ContractSources.Get("cli/Program.cs"), "args.FirstOrDefault\\(\\) == \"([^\"]+)\"");
        _missingCommands = [.. dispatchedCommands.Except(commands.Select(command => command["name"].GetValue<string>()), StringComparer.Ordinal)];
    }

    [Fact] void should_document_every_dispatched_option() => _missingOptions.ShouldBeEmpty();
    [Fact] void should_document_every_dispatched_command() => _missingCommands.ShouldBeEmpty();
}
