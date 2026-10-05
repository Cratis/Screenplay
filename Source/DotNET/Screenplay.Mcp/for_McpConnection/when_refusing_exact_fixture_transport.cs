// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_refusing_exact_fixture_transport : given.a_connection
{
    [Theory]
    [InlineData("numbers exact\n", true)]
    [InlineData("", false)]
    void should_refuse_recursive_exact_values_without_changing_legacy_fixture_transport(string prefix, bool refused)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), prefix + "import External.Payload\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        payload Payload\n        produces E\n          payload = payload\n      event E\n        payload Payload\n      specification T\n        when C\n          payload = {\"nested\":[9007199254740993]}\n        then E\n          payload = {\"nested\":[9007199254740993]}\n");
        var snapshot = new McpSnapshot(Root.Read());
        var error = Catch.Exception(() => McpFixtureQueries.Values(snapshot, null, null, null, null));
        if (refused)
        {
            error.ShouldBeOfExactType<McpFailure>();
            error.Message.ShouldContain("ExactNumberFixtureTransportUnsupported");
        }
        else
        {
            error.ShouldBeNull();
        }
    }
}
