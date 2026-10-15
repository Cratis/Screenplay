// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_phase_three_generation_is_admitted : Specification
{
    [Theory]
    [InlineData("String", "pattern \"#{4}\" checksum luhn\n  validate checksum luhn")]
    [InlineData("Int", "random 1..9")]
    [InlineData("String", "rule Make\n    ```csharp\n    return value;\n    ```")]
    void should_not_label_generation_or_its_declarative_rules_as_unadmitted(string primitive, string strategy)
    {
        var application = new ScreenplayCompiler().Parse($"concept Value : {primitive}\n  generate {strategy}\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        value Value generated\n        returns value").Value!;
        var readiness = new McpAuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
        readiness.SyntaxOnly(application.Modules.Single().Features.Single().Slices.Single().Commands.Single()).ShouldBeFalse();
    }
}
