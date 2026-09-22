// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_CommandLineInformation;

public class when_describing_the_installed_tool : Specification
{
    string _help = string.Empty;
    string _version = string.Empty;
    bool _handledHelp;
    bool _handledVersion;
    bool _handledServer;

    void Because()
    {
        using var help = new StringWriter();
        using var version = new StringWriter();
        _handledHelp = CommandLineInformation.TryPrint(["mcp", "--help"], help);
        _handledVersion = CommandLineInformation.TryPrint(["--version"], version);
        _handledServer = CommandLineInformation.TryPrint(["mcp", "model"], help);
        _help = help.ToString();
        _version = version.ToString();
    }

    [Fact] void should_handle_help_without_opening_a_model() => _handledHelp.ShouldBeTrue();
    [Fact] void should_describe_the_stdio_command() => _help.Contains("screenplay mcp <model-folder>", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_an_installed_version() => _handledVersion.ShouldBeTrue();
    [Fact] void should_emit_a_version_value() => _version.Trim().ShouldNotBeEmpty();
    [Fact] void should_not_intercept_server_startup() => _handledServer.ShouldBeFalse();
}
