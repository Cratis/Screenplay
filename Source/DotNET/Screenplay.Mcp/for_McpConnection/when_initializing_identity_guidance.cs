// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_initializing_identity_guidance : given.a_connection
{
    string _instructions = null!;

    void Because()
    {
        using var response = JsonDocument.Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"spec","version":"1"}}}"""));
        _instructions = response.RootElement.GetProperty("result").GetProperty("instructions").GetString()!;
    }

    [Fact] void should_explain_nested_model_discovery() => _instructions.ShouldContain("discovers nested .play files, else Source or src, else a new Screenplay folder");
    [Fact] void should_preserve_existing_workspace_state_roots() => _instructions.ShouldContain("Existing identity or pending-journal state along that discovery path preserves its workspace root");
    [Fact] void should_require_a_path_for_multiple_client_roots() => _instructions.ShouldContain("With multiple client roots, pass path to choose one");
    [Fact] void should_condition_the_working_directory_fallback_on_model_content() => _instructions.ShouldContain("the working directory when it holds .play files or a .screenplay folder");
    [Fact] void should_name_the_per_user_fallback() => _instructions.ShouldContain("otherwise Documents/Screenplay in the user's home folder");
    [Fact] void should_state_that_apply_persists_identity_state() => _instructions.Contains("identity state persists on apply", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_offer_export_as_optional_transfer_or_backup() => _instructions.Contains("export-workspace is optional for portable transfer or backup", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_require_export_to_preserve_identities() => _instructions.Contains("save export-workspace to preserve identities", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_remind_clients_to_review_proposals_and_executable_readiness() => _instructions.Contains("read-proposal", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_teach_the_keyed_query_an_instance_check_needs() => _instructions.Contains("needs exactly one keyed query returning that read model", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_to_fix_introduced_executable_errors_before_apply() => _instructions.Contains("introducedExecutableErrors; fix them before apply", StringComparison.Ordinal).ShouldBeTrue();
}
