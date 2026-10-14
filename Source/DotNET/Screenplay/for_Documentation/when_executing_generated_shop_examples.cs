// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.for_Documentation;

public class when_executing_generated_shop_examples : Specification
{
    [Fact]
    void should_bind_and_execute_the_documented_shop_fixtures()
    {
        var examples = DocumentationExamples.All().Where(example => example.Path.Replace('\\', '/').StartsWith("screenplay/generated-values", StringComparison.Ordinal) && example.Source.Contains("module Shop", StringComparison.Ordinal)).ToArray();
        examples.Length.ShouldEqual(3);
        var specifications = 0;
        foreach (var example in examples)
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
            const string Key = "generated-shop";
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "shop.play", example.Source);
            var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, example.Reference + ": " + string.Join('\n', compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
            var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
            plan.Success.ShouldBeTrue();
            foreach (var specification in plan.Plan!.Specifications.Values)
            {
                var run = new SemanticSpecificationRunner().Run(plan.Plan, specification.Id);
                Assert.True(run.Passed, example.Reference + ": " + string.Join('\n', run.Failures));
                specifications++;
            }
        }

        specifications.ShouldEqual(2);
    }
}
