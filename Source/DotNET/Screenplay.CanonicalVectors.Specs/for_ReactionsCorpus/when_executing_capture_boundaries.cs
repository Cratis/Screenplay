// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_executing_capture_boundaries : Specification
{
    [Fact]
    void should_reject_incomplete_instants_before_and_after_canonical_reading()
    {
        foreach (var value in new[] { "12:00", "12:00Z", "2026-10", "2026-10-02", "2026-10-02T12:00:00", "10-02T12:00:00Z" })
        {
            var model = Capture($"payload = \"{value}\"", "DateTime optional");
            foreach (var run in Runs(model))
            {
                run.Execution.ShouldBeOfExactType<SemanticRejected>();
                run.Execution.World.Facts.ShouldBeEmpty();
            }
        }
    }

    [Fact]
    void should_normalize_zoned_instants_without_using_the_scenario_date()
    {
        foreach (var clock in new[] { "1980-01-01T00:00:00Z", "2099-12-31T23:59:59Z" })
        {
            foreach (var value in new[] { "2026-10-02T12:00:00Z", "2026-10-02T14:00:00+02:00" })
            {
                foreach (var run in Runs(Capture($"payload = \"{value}\"", "DateTime", clock: clock)))
                {
                    run.Passed.ShouldBeTrue();
                    ((SemanticAccepted)run.Execution).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("2026-10-02T12:00:00.0000000Z"));
                }
            }
        }
    }

    [Fact]
    void should_never_accept_present_structures_or_unknown_paths_as_null()
    {
        foreach (var (record, field) in new[]
        {
            ("payload = {\"name\":\"present\"}", "payload"),
            ("payload = [{\"name\":\"present\"}]", "payload"),
            ("payload = []", "payload"),
            ("payload = {\"name\":\"present\"}", "payload.name")
        })
        {
            foreach (var run in Runs(Capture(record, "String optional", field)))
            {
                run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
                run.Passed.ShouldBeFalse();
                run.Execution.World.Facts.ShouldBeEmpty();
            }
        }
    }

    [Fact]
    void should_keep_missing_null_and_structured_field_outcomes_distinct()
    {
        foreach (var record in new[] { "", "payload = null" })
        {
            foreach (var run in Runs(Capture(record, "String optional")))
            {
                ((SemanticAccepted)run.Execution).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Null);
            }

            foreach (var run in Runs(Capture(record, "String"))) run.Execution.ShouldBeOfExactType<SemanticRejected>();
        }
    }

    [Fact]
    void should_remove_null_and_omitted_nested_records_once()
    {
        foreach (var contact in new[] { "", "contact = null" })
        {
            var model = Compile($$"""
                module Billing
                  feature Import
                    slice Translate Records
                      capture Records
                        key id
                        nested contact
                          append Removed
                            when removed
                      event Removed
                      specification PresentRecord
                        given capture Records
                          id = "root"
                          contact = {"name":"before"}
                        when capture Records
                          id = "root"
                          {{contact}}
                        then Removed
                """);
            foreach (var run in Runs(model))
            {
                run.Passed.ShouldBeTrue();
                run.Execution.World.Facts.Length.ShouldEqual(1);
            }
        }
    }

    [Fact]
    void should_preserve_typed_decimal_child_identity_through_canonical_reading()
    {
        foreach (var (previous, current, duplicate) in new[] { (1m, 1.00m, false), (0m, -0.0m, false), (-2.0m, -2.00m, false), (1m, 1.00m, true) })
        {
            var model = Compile("""
                module Billing
                  feature Import
                    slice Translate Records
                      capture Records
                        key id
                        append Seen
                        children items identified by id
                          append Added
                            when added
                          append Removed
                            when removed
                      event Seen
                      event Added
                      event Removed
                      specification PresentRecord
                        given capture Records
                          id = "root"
                          items = [{"id":1}]
                        when capture Records
                          id = "root"
                          items = [{"id":1}]
                        then Seen
                """);
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single();
            var specification = slice.Specifications.Single();
            var given = specification.GivenCaptures.Single();
            var action = specification.WhenCapture!;
            var application = model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification with
                {
                    GivenCaptures = [given with { Record = Children(given.Record, previous, false) }],
                    WhenCapture = action with { Record = Children(action.Record, current, duplicate) }
                }] }] }] }]
            };
            var scaled = ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, application);
            var results = Runs(scaled).ToArray();
            results[0].Execution.Kind.ShouldEqual(results[1].Execution.Kind);
            foreach (var run in results)
            {
                if (duplicate) run.Execution.ShouldBeOfExactType<SemanticRejected>();
                else run.Passed.ShouldBeTrue();
                run.Execution.World.Facts.Length.ShouldEqual(duplicate ? 0 : 1);
            }
        }
    }

    static SemanticCaptureRecord Children(SemanticCaptureRecord record, decimal value, bool duplicate)
    {
        var child = new SemanticCaptureRecord([new("id", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Number(value) }]);
        return record with
        {
            Fields = [.. record.Fields.Select(field => field.Name == "items" ? field with
        {
            Records = duplicate ? [child, new([new("id", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Number(decimal.Truncate(value)) }])] : [child]
        } : field)]
        };
    }

    static ExecutableSemanticModel Capture(string record, string type, string field = "payload", string clock = "1980-01-01T00:00:00Z") => Compile($$"""
        module Billing
          feature Import
            slice Translate Records
              capture Records
                key id
                append Recorded
                  value = $.{{field}}
              event Recorded
                value {{type}}
              specification PresentRecord
                given clock "{{clock}}"
                when capture Records
                  id = "root"
                  {{record}}
                then Recorded
                  value = "{{(type.StartsWith("DateTime", StringComparison.Ordinal) ? "2026-10-02T12:00:00Z" : "expected")}}"
        """);

    static ExecutableSemanticModel Compile(string source)
    {
        const string StableKey = "capture-boundaries";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Billing.play", source);
        var result = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        result.Success.ShouldBeTrue();
        return result.Value!.Model;
    }

    static IEnumerable<SemanticSpecificationRun> Runs(ExecutableSemanticModel model)
    {
        var restored = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model));
        restored.Revision.ShouldEqual(model.Revision);
        foreach (var input in new[] { model, restored })
        {
            var plan = SemanticExecutionPlan.Compile(input).Plan!;
            yield return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        }
    }
}
