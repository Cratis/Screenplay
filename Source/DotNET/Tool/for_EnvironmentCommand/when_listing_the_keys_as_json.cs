// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_EnvironmentCommand;

public class when_listing_the_keys_as_json : given.a_model_with_providers
{
    int _exitCode;
    JsonElement _secret;

    void Because()
    {
        _exitCode = ToolCommands.Run(["environment", Root, "--format", "json"], Output, Error);
        using var report = JsonDocument.Parse(Output.ToString());
        _secret = report.RootElement.GetProperty("keys").EnumerateArray().Single(key => key.GetProperty("key").GetString() == "authentication.providers.Partner.clientSecret").Clone();
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_flag_the_secret() => _secret.GetProperty("secret").GetBoolean().ShouldBeTrue();
    [Fact] void should_name_the_owner() => _secret.GetProperty("owner").GetString().ShouldEqual("Partner");
}
