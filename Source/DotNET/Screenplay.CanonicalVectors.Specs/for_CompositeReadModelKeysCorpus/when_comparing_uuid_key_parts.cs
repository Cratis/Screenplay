// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_comparing_uuid_key_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("lookup")]
    [InlineData("filter")]
    [InlineData("projection")]
    [InlineData("removal")]
    public void should_keep_distinct_canonical_uuid_text_separate(string scenario)
    {
        const string upper = "abcdef12-3456-4789-abcd-0123456789ac";
        const string lower = "abcdef12-3456-4789-abcd-0123456789ab";
        var declarations = Declarations.Replace("first String", "first Uuid", StringComparison.Ordinal)
            .Replace("      event Removed", "      event Removed\n        first Uuid\n        second String", StringComparison.Ordinal)
            .Replace("query All => Row[]", "query All => Row[]\n        filter first Uuid", StringComparison.Ordinal);
        var action = scenario switch
        {
            "lookup" => $$"""
                when query Find
                  first = "{{lower}}"
                  second = "b"
                then result
                  quantity = 9
                """,
            "filter" => $$"""
                when query All
                  first = "{{lower}}"
                then result
                  first = "{{lower}}"
                  second = "b"
                  quantity = 9
                """,
            "projection" => $$"""
                when append Recorded
                  first = "{{upper}}"
                  second = "b"
                then readmodel Row
                  first = "{{upper}}"
                  second = "b"
                  quantity = 1
                then readmodel Row
                  first = "{{lower}}"
                  second = "b"
                  quantity = 9
                """,
            _ => $$"""
                when append Removed
                  first = "{{upper}}"
                  second = "b"
                then no readmodel Row for {"second":"b","first":"{{upper}}"}
                then readmodel Row
                  first = "{{lower}}"
                  second = "b"
                  quantity = 9
                """
        };
        var source = declarations + "\n" + """
                  projection Rows => Row
                    no automap
                    from Recorded
                      key
                        first = first
                        second = second
                      quantity = 1
                    remove with Removed
                      key
                        second = second
                        first = first
                  specification Check
            """ + "\n" + Indent(
            $$"""
                given readmodel Row
                  first = "{{upper}}"
                  second = "b"
                  quantity = 7
                given readmodel Row
                  first = "{{lower}}"
                  second = "b"
                  quantity = 9
            """ + "\n" + action,
            8);
        var compiled = Compile(source);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(compiled.Value!.Model));
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        var run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
        Assert.True(run.Passed, string.Join('\n', run.Failures));
    }

    [Fact]
    void should_refuse_noncanonical_uuid_text_instead_of_case_folding_it()
    {
        var source = Specification("""
            given readmodel Row
              first = "ABCDEF12-3456-4789-ABCD-0123456789AB"
              second = "b"
              quantity = 1
            when query All
            then no result
            """).Replace("first String", "first Uuid", StringComparison.Ordinal);
        var compiled = Compile(source);
        compiled.Success.ShouldBeFalse();
        compiled.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("incompatible with its target type", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
