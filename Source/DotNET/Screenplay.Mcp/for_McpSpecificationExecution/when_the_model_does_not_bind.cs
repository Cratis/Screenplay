// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_the_model_does_not_bind : given.a_model
{
    JsonElement _response;

    void Establish() => File.AppendAllText(Path.Combine(RootPath, "application.play"), "\neventsource Project\n  identifier ProjectId\n");
    void Because() => _response = Call("run-specifications").GetProperty("result");

    [Fact] void should_not_report_success() => _response.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_binding_failure() => _response.GetProperty("structuredContent").GetProperty("outcome").GetString().ShouldEqual("unbound");
    [Fact] void should_return_executable_diagnostics() => _response.GetProperty("structuredContent").GetProperty("executableDiagnostics").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_not_return_a_result_page() => _response.GetProperty("structuredContent").GetProperty("page").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_execute_any_scenario() => _response.GetProperty("structuredContent").GetProperty("executed").GetInt32().ShouldEqual(0);
}
