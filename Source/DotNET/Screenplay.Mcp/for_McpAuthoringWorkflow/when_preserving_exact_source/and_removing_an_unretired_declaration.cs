// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_removing_an_unretired_declaration : given.an_exact_source_connection
{
    JsonElement _refused;

    void Establish()
    {
        Apply(Opened, Result("propose-source", Arguments(ReplaceSource(Original))));
        Opened = Open();
    }

    void Because() => _refused = Call("propose-source", Arguments(ReplaceSource("concept ProjectId : Uuid\nconcept ProjectName : String\n"))).GetProperty("result");

    [Fact] void should_refuse_the_proposal() => _refused.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_still_enforce_identity_continuity() => _refused.GetProperty("structuredContent").GetProperty("conflicts").GetRawText().ShouldContain("Identity");
    [Fact] void should_not_retain_a_proposal() => _refused.GetProperty("structuredContent").TryGetProperty("proposalId", out _).ShouldBeFalse();
    [Fact] void should_leave_the_original_text_intact() => File.ReadAllText(ApplicationPath).ShouldEqual(Original);
}
