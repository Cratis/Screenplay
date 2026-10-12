// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_the_candidate_does_not_bind : given.a_proposal_model
{
    JsonElement _response;

    void Because()
    {
        var source = ModelSource.Replace("      command Register\n", "      command Register\n        concurrency\n          eventSource\n", StringComparison.Ordinal);
        var proposalId = ProposeSource(source).GetProperty("proposalId").GetString();
        _response = Call("run-specifications", new { proposalId }).GetProperty("result");
    }

    [Fact] void should_not_report_success() => _response.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_candidate_binding_failure() => _response.GetProperty("structuredContent").GetProperty("outcome").GetString().ShouldEqual("unbound");
    [Fact] void should_return_candidate_executable_diagnostics() => _response.GetProperty("structuredContent").GetProperty("executableDiagnostics").EnumerateArray().Any(diagnostic => diagnostic.GetProperty("code").GetString() == "PLAY0271").ShouldBeTrue();
    [Fact] void should_not_return_a_result_page() => _response.GetProperty("structuredContent").GetProperty("page").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_execute_any_candidate_scenario() => _response.GetProperty("structuredContent").GetProperty("executed").GetInt32().ShouldEqual(0);
}
