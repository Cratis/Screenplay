// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_property_named_for : given.a_compiler
{
    const string Source =
        """
        specification RecordingAnAlias
          given AliasRecorded
            for = "customer"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_for_as_a_property_name() => _result.Value!.Given.Single().Values.Single().Property.ShouldEqual("for");
    [Fact] void should_keep_the_property_value() => ((LiteralExpressionSyntax)_result.Value!.Given.Single().Values.Single().Source).Value.ShouldEqual("customer");
    [Fact] void should_not_invent_an_event_source_assertion() => _result.Value!.Given.Single().For.ShouldBeNull();
}
