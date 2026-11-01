// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_EnvironmentCommand;

public class when_listing_the_keys_as_text : given.a_model_with_providers
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["environment", Root], Output, Error);

    string[] Lines => Output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_list_a_key_per_line() => Lines.ShouldContain("authentication.providers.Partner.authority");
    [Fact] void should_mark_secrets() => Lines.ShouldContain("authentication.providers.EntraId.clientSecret  (secret)");
    [Fact] void should_not_mark_plain_values() => Lines.ShouldContain("authentication.providers.EntraId.clientId");
    [Fact] void should_report_nothing_on_the_error_stream() => Error.ToString().ShouldBeEmpty();
}
