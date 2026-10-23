// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_moving_a_declaration;

public class and_its_refusal_uses_the_standard_rejection_shape : given.a_declaration_move_connection
{
    JsonElement _result;

    void Because() => _result = ProposeMove(Address("Query", "ProjectNamed"), Address("Slice", "Lookup"));

    JsonElement Content => _result.GetProperty("structuredContent");

    [Fact] void should_refuse() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_include_release_violations() => Content.GetProperty("violations").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_include_identity_migration_issues() => Content.GetProperty("identityMigrationIssues").GetArrayLength().ShouldEqual(0);
    [Fact] void should_include_authoring_diagnostics() => Content.GetProperty("authoringDiagnostics").ValueKind.ShouldEqual(JsonValueKind.Array);
    [Fact] void should_include_executable_readiness() => Content.GetProperty("executableReady").ValueKind.ShouldEqual(JsonValueKind.False);
    [Fact] void should_include_executable_diagnostics() => Content.GetProperty("executableDiagnostics").ValueKind.ShouldEqual(JsonValueKind.Array);
    [Fact] void should_use_the_standard_failure_kind() => Content.GetProperty("failureKind").GetString().ShouldEqual("ProposalRejected");
}
