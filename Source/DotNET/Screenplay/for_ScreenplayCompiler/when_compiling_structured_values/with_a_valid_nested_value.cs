// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_structured_values;

public class with_a_valid_nested_value : given.a_structured_specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = Compiler.Compile(Source);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_a_typed_list() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.Values.First().Source.ShouldBeOfExactType<ListExpressionSyntax>();
    [Fact] void should_keep_a_typed_object() => ((ListExpressionSyntax)_result.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.Values.First().Source).Items.Single().ShouldBeOfExactType<ObjectExpressionSyntax>();
    [Fact] void should_record_the_whole_value_span() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.Values.First().SourceLength.ShouldEqual("[{\"sku\":\"A-1\",\"status\":\"open\"}]".Length);
    [Fact] void should_record_nested_literal_spans() => ((LiteralExpressionSyntax)((ObjectExpressionSyntax)((ListExpressionSyntax)_result.Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.Values.First().Source).Items.Single()).Members.First().Value).RawLength.ShouldEqual(5);
}
