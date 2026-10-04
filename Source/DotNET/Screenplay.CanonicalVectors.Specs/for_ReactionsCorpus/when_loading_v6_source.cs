// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_loading_v6_source : Specification
{
    readonly CanonicalCorpusVector _vector = ReactionsCorpus.V6;

    [Fact] void should_pin_the_source_backed_revision() => _vector.SemanticRevision.ToString().ShouldEqual("rev1:1be294990a5cf564bba0cbc9aae5a1db1d2e0b27387e2a164de2149e8a4f0700");
    [Fact] void should_expect_every_specification() => _vector.SpecificationExpectations.Select(expectation => expectation.Name).ShouldEqual("SendingAnInvoice", "ClosingAPaidInvoice", "IssuingTheWeeklyDigest", "SeeingALegacyPayment", "RejectingAnEmptyReason", "RejectingASecondClose", "UnsupportedStartup");
    [Fact] void should_admit_the_version_and_run_every_specification_in_every_source_form()
    {
        _vector.SourceForms.Select(form => form.Name).ShouldEqual("single", "folder", "reordered", "relocated");
        var traces = new Dictionary<SemanticId, string>();
        foreach (var form in _vector.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
            var result = new SemanticModelCompiler().Compile(_vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            result.Success.ShouldBeTrue();
            result.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
            var model = result.Value!.Model;
            model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
            model.Revision.ShouldEqual(_vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            foreach (var expectation in _vector.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldEqual(expectation.Passed);
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                if (expectation.RejectionCategory is { } category) ((SemanticRejected)run.Execution).Category.ShouldEqual(category);
                if (expectation.RejectionMessage is { } message) ((SemanticRejected)run.Execution).Details.ShouldEqual(message);
                if (expectation.UnsupportedCapability is { } capability) ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(capability);
                if (expectation.WorldFactCount is { } count) run.Execution.World.Facts.Length.ShouldEqual(count);
                var trace = Normalize(run);
                if (traces.TryGetValue(expectation.Specification, out var previous)) trace.ShouldEqual(previous);
                else traces.Add(expectation.Specification, trace);
            }
        }
    }
    static string Normalize(SemanticSpecificationRun run) => JsonSerializer.Serialize(new
    {
        Outcome = run.Execution.Kind.ToString(),
        Rejection = (run.Execution as SemanticRejected)?.Category.ToString(),
        Capability = (run.Execution as SemanticUnsupported)?.Capability.ToString(),
        run.Passed,
        run.Failures,
        Facts = run.Execution.World.Facts.Select(fact => new
        {
            Event = fact.EventContract.ToString(),
            Destination = Value(fact.Destination),
            Occurred = fact.Occurred?.ToString("O", CultureInfo.InvariantCulture),
            Reaction = fact.ReactionOrigin?.ToString(),
            Values = fact.Values.Select(value => new { Property = value.TargetProperty.ToString(), Value = Value(value.Value) })
        })
    });

    static string Value(SemanticValue value) => value switch
    {
        SemanticNullValue => "null",
        SemanticTextValue text => $"text:{text.Value}",
        SemanticNumberValue number => $"number:{number.Value.ToString("R", CultureInfo.InvariantCulture)}",
        SemanticBooleanValue boolean => boolean.Value ? "boolean:true" : "boolean:false",
        _ => throw new InvalidSemanticContract("The v6 corpus trace requires a supported scalar value.")
    };

    [Fact] void should_round_trip_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_vector.EsmBytes.AsSpan())).SequenceEqual(_vector.EsmBytes).ShouldBeTrue();
}
