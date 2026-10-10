// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EventRoutesCorpus;

public class when_loading_v10_vectors : Specification
{
    [Theory]
    [InlineData("production-routes")]
    [InlineData("observer-filters")]
    void should_pin_v10_bytes_catalogs_and_outcomes_across_four_source_forms(string key)
    {
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_EVENT_ROUTES_CORPUS") == "1") when_loading_and_executing_event_routes.Regenerate(key);
        var corpus = key == "production-routes" ? EventRoutesCorpus.ProductionRoutesV10 : EventRoutesCorpus.ObserverFiltersV10;
        foreach (var form in corpus.SourceForms)
        {
            var model = when_loading_and_executing_event_routes.Compile(form).Model;
            model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
            model.Revision.ShouldEqual(corpus.SemanticRevision);
            var bytes = SemanticModelSerializer.Serialize(model);
            bytes.SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
            SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).SequenceEqual(bytes).ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            foreach (var expected in corpus.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expected.Specification);
                Assert.True(run.Passed == expected.Passed, $"{key}/{form.Name}/{expected.Name}: {string.Join(';', run.Failures)}");
                run.Execution.Kind.ShouldEqual(expected.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expected.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(expected.RejectionCategory!.Value);
                    rejected.Details.ShouldEqual(expected.RejectionMessage);
                }
                if (run.Execution is SemanticUnsupported unsupported) unsupported.Capability.ShouldEqual(expected.UnsupportedCapability!.Value);
                var facts = (run.Execution as SemanticAccepted)?.Facts ?? [];
                facts.Select(fact => when_loading_and_executing_event_routes.Route(fact.Route)).SequenceEqual(expected.Routes).ShouldBeTrue();
            }
        }
    }
}
