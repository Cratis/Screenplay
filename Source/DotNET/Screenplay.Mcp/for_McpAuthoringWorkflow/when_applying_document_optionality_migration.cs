// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_applying_document_optionality_migration : given.a_document_optionality_proposal
{
    [Fact]
    void should_apply_only_after_explicit_acceptance()
    {
        Apply(_opened, _proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_preview);
    }
}
