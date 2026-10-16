// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_projecting_automapped_key_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("", true)]
    [InlineData("          first = second\n", false)]
    public void should_accept_equal_parts_and_fail_a_contradictory_mapping(string mapping, bool passes)
    {
        var source = Declarations.Replace("      event Recorded\n        first String\n        second String", "      event Recorded\n        first String\n        second String\n        quantity Int", StringComparison.Ordinal) + "\n" + """
                  projection Rows => Row
                    from Recorded
                      key
                        first = first
                        second = second
            """ + "\n" + mapping + """
                  specification Project
                    when append Recorded
                      first = "a"
                      second = "b"
                      quantity = 3
                    then readmodel Row
                      first = "a"
                      second = "b"
                      quantity = 3
            """;
        var compiled = Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compiled.Value!.Model;
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        var run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
        Assert.Equal(passes, run.Passed);
        if (passes)
        {
            run.Execution.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
        }
        else
        {
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            ((SemanticUnsupported)run.Execution).Details.ShouldContain("key disagrees with read-model identifier 'first'");
        }
    }
}
