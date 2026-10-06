// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_RegisterProjectCorpus;

public class when_running_v7_outcomes : given.a_v7_corpus
{
    [Fact]
    void should_match_all_normalized_outcomes_in_every_form()
    {
        var traces = new Dictionary<SemanticId, string>();
        foreach (var form in Corpus.SourceForms)
        {
            var model = Compile(Corpus, form).Model;
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
            Corpus.SpecificationExpectations.Select(expectation => expectation.Specification).ShouldEqual(slice.Specifications.Select(specification => specification.Id).OrderBy(id => id.ToString(), StringComparer.Ordinal));
            foreach (var expectation in Corpus.SpecificationExpectations)
            {
                slice.Specifications.Single(specification => specification.Id == expectation.Specification).Name.ShouldEqual(expectation.Name);
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldEqual(expectation.Passed);
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                if (expectation.UnsupportedCapability is { } capability) ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(capability);
                Response((run.Execution as SemanticAccepted)?.Response).ShouldEqual(expectation.Response);
                if (expectation.Name == "MismatchedReturn") run.Failures.ShouldEqual("Response field 'receipt' does not match the expected value.");
                if (expectation.Name == "MissingAllocation") run.Failures.ShouldEqual("Expected Accepted, got Unsupported.");
                if (expectation.Passed) run.Failures.ShouldBeEmpty();
                var trace = JsonSerializer.Serialize(new
                {
                    Outcome = run.Execution.Kind.ToString(),
                    Capability = (run.Execution as SemanticUnsupported)?.Capability.ToString(),
                    run.Passed,
                    run.Failures,
                    Response = Response((run.Execution as SemanticAccepted)?.Response),
                    Facts = run.Execution.World.Facts.Select(fact => new
                    {
                        Event = fact.EventContract.ToString(),
                        Destination = ((SemanticTextValue)fact.Destination).Value,
                        Values = fact.Values.Select(value => new { Target = value.TargetProperty.ToString(), Text = ((SemanticTextValue)value.Value).Value })
                    })
                });
                if (traces.TryGetValue(expectation.Specification, out var previous)) trace.ShouldEqual(previous);
                else traces.Add(expectation.Specification, trace);
            }
        }
    }

    static string? Response(SemanticExecutionResponse? response) => response switch
    {
        null => null,
        SemanticRecordExecutionResponse record => JsonSerializer.Serialize(new
        {
            kind = "record",
            fields = record.Fields.Select(field => new
            {
                name = field.Name,
                value = new { kind = "string", value = ((SemanticTextValue)field.Value).Value }
            })
        }),
        _ => throw new InvalidSemanticContract("The RegisterProject v7 corpus expects a record of text values.")
    };
}
