// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_refusing_optional_condition_operands;

public class with_an_optional_command_operand : for_SemanticModelBinder.given.a_semantic_binder
{
    [Theory]
    [InlineData("optionalValue == \"active\"")]
    [InlineData("requiredValue == optionalValue")]
    [InlineData("requiredValue == \"active\" and optionalValue == requiredValue")]
    void should_refuse_the_condition_at_its_source(string condition)
    {
        var source = $$"""
            module M
              feature F
                slice StateChange Update
                  command Update
                    requiredValue String
                    optionalValue String optional
                    validate
                      require {{condition}}
                    produces Updated
                  event Updated
            """;
        var result = Bind(source, "conditions.play", "conditions.play");
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Message.ShouldContain("comparison operands must be required");
        diagnostic.Location.Path.ShouldEqual("conditions.play");
        diagnostic.Location.Line.ShouldEqual(8);
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
    }
}
