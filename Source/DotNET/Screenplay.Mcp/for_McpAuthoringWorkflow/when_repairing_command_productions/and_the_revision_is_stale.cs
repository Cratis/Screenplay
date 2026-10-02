// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_repairing_command_productions;

public class and_the_revision_is_stale : given.a_producing_workspace
{
    [Theory]
    [InlineData(false, DiagnosticCodes.UnknownEvent)]
    [InlineData(true, DiagnosticCodes.OmittedProductionDestination)]
    void should_refuse_without_writing(bool declared, string code)
    {
        OpenProducingWorkspace(declared, code);
        var result = Call("propose-repair", Arguments("wsrev1:" + new string('0', 64)));
        result.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(Original).ShouldBeTrue();
    }
}
