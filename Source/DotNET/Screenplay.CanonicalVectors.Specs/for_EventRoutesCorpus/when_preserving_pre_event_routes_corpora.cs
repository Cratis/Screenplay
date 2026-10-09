// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EventRoutesCorpus;

public class when_preserving_pre_event_routes_corpora : Specification
{
    [Fact]
    void should_keep_prior_versions_bytes_revisions_outcomes_and_absent_routes()
    {
        foreach (var corpus in new[] { RegisterProjectCorpus.LegacyV1, RegisterProjectCorpus.V2, ReadModelAbsenceCorpus.V5, ReactionsCorpus.V6, RegisterProjectCorpus.V7, PolicyNegationCorpus.V7 })
        {
            foreach (var form in corpus.SourceForms)
            {
                var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
                var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
                var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
                Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
                var model = compiled.Value!.Model;
                model.SemanticVersion.IsAtLeast(SemanticVersion.V8).ShouldBeFalse();
                model.Application.EventSources.ShouldBeEmpty();
                model.Revision.ShouldEqual(corpus.SemanticRevision);
                SemanticModelSerializer.Serialize(model).SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
                var plan = SemanticExecutionPlan.Compile(model).Plan!;
                foreach (var expected in corpus.SpecificationExpectations)
                {
                    expected.Routes.ShouldBeEmpty();
                    var run = new SemanticSpecificationRunner().Run(plan, expected.Specification);
                    run.Passed.ShouldEqual(expected.Passed);
                    run.Execution.Kind.ShouldEqual(expected.Outcome);
                    if (expected.RejectionMessage is { } message) ((SemanticRejected)run.Execution).Details.ShouldEqual(message);
                    if (expected.RejectionCategory is { } category) ((SemanticRejected)run.Execution).Category.ShouldEqual(category);
                    if (expected.UnsupportedCapability is { } capability) ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(capability);
                    if (expected.WorldFactCount is { } count) run.Execution.World.Facts.Length.ShouldEqual(count);
                    run.Execution.World.Facts.All(fact => fact.Route is null).ShouldBeTrue();
                }
            }
        }
    }
}
