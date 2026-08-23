// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_query_results : given.a_compiler
{
    const string Source =
        """
        specification LookingUpAProject
          when RegisterProject
            projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then query Projects.ProjectById
            arguments
              projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            result
              projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              name = "Screenplay"
          then query MissingProjectById
            arguments
              projectId = "00000000-0000-0000-0000-000000000000"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_compile_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_parse_both_query_assertions() => _result.Value!.ThenQueries.Count().ShouldEqual(2);
    [Fact] void should_preserve_a_qualified_query_name() => Query.Query.ShouldEqual("Projects.ProjectById");
    [Fact] void should_parse_the_query_argument() => Query.Arguments.Single().Property.ShouldEqual("projectId");
    [Fact] void should_parse_the_query_argument_value() => ((LiteralExpressionSyntax)Query.Arguments.Single().Source).Value.ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_parse_the_expected_result() => Query.Results.Single().Properties.Count().ShouldEqual(2);
    [Fact] void should_preserve_an_empty_query_result() => _result.Value!.ThenQueries.Last().Results.ShouldBeEmpty();

    SpecificationQuerySyntax Query => _result.Value!.ThenQueries.First();
}
