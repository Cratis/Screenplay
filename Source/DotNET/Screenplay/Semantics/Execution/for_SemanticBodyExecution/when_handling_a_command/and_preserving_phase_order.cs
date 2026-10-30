// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_preserving_phase_order : given.a_command_handler
{
    [Theory]
    [InlineData("generated")]
    [InlineData("missing-generation")]
    [InlineData("mapping-hint")]
    [InlineData("authorization")]
    [InlineData("validation")]
    void should_reach_the_handler_only_after_earlier_phases(string phase)
    {
        Contexts.Clear();
        var source = phase switch
        {
            "generated" or "missing-generation" => "concept Id : Uuid\n" + Source.Replace("name String identifier", "name String identifier\n        receipt Id generated", StringComparison.Ordinal),
            "mapping-hint" => Source.Replace("tag production", "tag production\n          name = $context.identity.userName", StringComparison.Ordinal),
            "authorization" => "policy Member\n  require authenticated\n" + Source.Replace("name String identifier", "name String identifier\n        authorize Member", StringComparison.Ordinal),
            _ => Source.Replace("name String identifier", "name String identifier\n        validate\n          name min 10", StringComparison.Ordinal)
        };
        Configure(source);
        if (phase == "generated")
        {
            var generated = Plan.Commands.Values.Single().Properties.Single(property => property.IsGenerated);
            var fixture = new SemanticPropertyValue(generated.Id, SemanticValue.Text("11111111-1111-1111-1111-111111111111"));
            Request = Request with { GeneratedValues = [fixture] };
            Execute().ShouldBeOfExactType<SemanticAccepted>();
            Contexts.Single().Command.Properties.ShouldContain(fixture);
        }
        else if (phase == "mapping-hint")
        {
            Request = Request with { Occurrence = null };
            Execute().ShouldBeOfExactType<SemanticAccepted>();
        }
        else
        {
            if (phase == "authorization") Request = Request with { Caller = null };
            var result = Execute();
            if (phase == "missing-generation") ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
            else ((SemanticRejected)result).Category.ShouldEqual(phase == "authorization" ? SemanticRejectionCategory.Unauthorized : SemanticRejectionCategory.Validation);
            Contexts.ShouldBeEmpty();
        }
    }
}
