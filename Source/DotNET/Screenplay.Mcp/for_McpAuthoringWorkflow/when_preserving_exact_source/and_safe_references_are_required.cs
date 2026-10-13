// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_safe_references_are_required : given.an_exact_source_connection
{
    JsonElement _refused;

    void Because() => _refused = Call("propose-source", Arguments(ReplaceSource(Original.Replace("\n        produces", "\n        notes UnknownProjectName\n        produces", StringComparison.Ordinal)))).GetProperty("result");

    [Fact] void should_refuse_new_unresolved_references() => _refused.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_retain_a_proposal() => _refused.GetProperty("structuredContent").TryGetProperty("proposalId", out _).ShouldBeFalse();
    [Fact] void should_leave_the_original_source_unchanged() => File.ReadAllText(ApplicationPath).ShouldEqual(Original);
}
