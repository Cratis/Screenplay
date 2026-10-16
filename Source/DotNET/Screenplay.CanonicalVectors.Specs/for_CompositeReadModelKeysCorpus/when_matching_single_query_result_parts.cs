// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_matching_single_query_result_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("then result\n  quantity = 1", true)]
    [InlineData("then result\n  first = \"different\"\n  quantity = 1", false)]
    [InlineData("then result\n  first = \"a\"\n  second = \"b\"\n  quantity = 1", true)]
    public void should_infer_omitted_parts_but_fail_a_stated_disagreement_at_execution(string expected, bool passes)
    {
        var source = Specification("""
            given readmodel Row
              first = "a"
              second = "b"
              quantity = 1
            given readmodel Row
              first = "a"
              second = "c"
              quantity = 2
            when query Find
              first = "a"
              second = "b"
            """ + "\n" + expected);
        var compiled = Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compiled.Value!.Model;
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        var run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
        run.Execution.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
        Assert.Equal(passes, run.Passed);
    }
}
