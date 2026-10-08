// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_discovering_workspace_location_guidance : given.a_connection
{
    string _description = null!;

    void Establish() => Initialize();

    void Because()
    {
        using var response = JsonDocument.Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}"""));
        _description = response.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "open-workspace")
            .GetProperty("description").GetString()!;
    }

    [Fact] void should_prioritize_an_explicit_path() => _description.ShouldContain("an explicit path wins, then the client's single project root");
    [Fact] void should_explain_nested_model_discovery() => _description.ShouldContain("discovers nested .play files, else Source or src, else a new Screenplay folder");
    [Fact] void should_preserve_existing_workspace_state_roots() => _description.ShouldContain("Existing identity or pending-journal state along that discovery path preserves its workspace root");
    [Fact] void should_require_a_path_for_multiple_client_roots() => _description.ShouldContain("With multiple client roots, pass path to choose one");
    [Fact] void should_condition_the_working_directory_fallback_on_model_content() => _description.ShouldContain("the working directory when it holds .play files or a .screenplay folder");
    [Fact] void should_name_the_per_user_fallback() => _description.ShouldContain("otherwise Documents/Screenplay in the user's home folder");
}
