// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.for_Documentation;

public class when_executing_generated_response_examples : Specification
{
    [Fact]
    void should_bind_and_pass_every_specification_in_the_complete_fixture()
    {
        var source = File.ReadAllText(Path.Combine(DocumentationExamples.Root(), "screenplay", "fixtures", "generated-responses.play"));
        var plan = Compile(source);
        plan.Specifications.Count.ShouldEqual(2);
        AssertPasses(plan);
    }

    [Fact]
    void should_pass_the_generated_fixture_snippet_in_the_complete_example_context()
    {
        var source = File.ReadAllText(Path.Combine(DocumentationExamples.Root(), "screenplay", "fixtures", "generated-responses.play"));
        var example = DocumentationExamples.All().Single(example => example.Path.Replace('\\', '/') == "screenplay/specifications.md" && example.Source.Contains("specification RegisteringReturnsIdentifiers", StringComparison.Ordinal));
        var body = example.Source[example.Source.IndexOf("      specification RegisteringReturnsIdentifiers", StringComparison.Ordinal)..];
        var start = source.IndexOf("      specification RegisteringReturnsIdentifiers", StringComparison.Ordinal);
        var end = source.IndexOf("    slice StateChange Rename", start, StringComparison.Ordinal);
        AssertPasses(Compile(source[..start] + body.TrimEnd() + "\n" + source[end..]));
    }

    [Fact]
    void should_execute_the_response_only_command_fragment_in_its_documented_concept_context()
    {
        var example = DocumentationExamples.All().Single(example => example.Path.Replace('\\', '/') == "screenplay/commands.md" && example.Source.Contains("command RegisterProject\n", StringComparison.Ordinal));
        var source = "concept ProjectId : Uuid\nconcept ReceiptId : Uuid\nconcept ProjectName : String\n" + example.Source +
            "      specification ReturnsIdentifiers\n        when RegisterProject\n          for \"11111111-1111-1111-1111-111111111111\"\n          generated receiptId = \"22222222-2222-2222-2222-222222222222\"\n          name = \"Apollo\"\n        then returns\n          projectId = \"11111111-1111-1111-1111-111111111111\"\n          receiptId = \"22222222-2222-2222-2222-222222222222\"\n";
        AssertPasses(Compile(source));
    }

    static SemanticExecutionPlan Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string Key = "generated-responses";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "generated-responses.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model);
        plan.Success.ShouldBeTrue();
        return plan.Plan!;
    }

    static void AssertPasses(SemanticExecutionPlan plan)
    {
        var runs = plan.Specifications.Values.Select(specification => (specification.Name, Run: new SemanticSpecificationRunner().Run(plan, specification.Id))).ToArray();
        string.Join('\n', runs.SelectMany(entry => entry.Run.Failures.Select(failure => entry.Name + ": " + failure))).ShouldEqual(string.Empty);
        runs.All(entry => entry.Run.Passed).ShouldBeTrue();
    }
}
