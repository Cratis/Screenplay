// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_absent_read_models : given.a_semantic_binder
{
    internal const string Source =
        """
        module Billing
          feature Invoices
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String
              query InvoiceById => InvoiceView?
                by invoiceId String
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
              specification RemovingOneOfTwoInvoices
                given readmodel InvoiceView
                  invoiceId = "first"
                given readmodel InvoiceView
                  invoiceId = "second"
                when append InvoiceRemoved
                  invoiceId = "first"
                then no readmodel InvoiceView for "first"
                then readmodel InvoiceView
                  invoiceId = "second"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_v5() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
    [Fact] void should_keep_event_lineage_when_v4_and_v5_features_coexist()
    {
        var result = Bind(Source.Replace(
            "      event InvoiceRemoved\n        invoiceId String",
            "      event InvoiceRemoved generation 1\n        invoiceId String\n      event InvoiceRemoved generation 2\n        invoiceId String",
            StringComparison.Ordinal));
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
        result.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().PriorRevisions.Length.ShouldEqual(1);
    }
    [Fact] void should_keep_one_absent_assertion() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenAbsentReadModels.Length.ShouldEqual(1);
    [Fact] void should_round_trip_v5_canonical_bytes()
    {
        var model = _result.Value!.Model;
        var bytes = SemanticModelSerializer.Serialize(model);
        SemanticModelSerializer.Deserialize(bytes).Revision.ShouldEqual(model.Revision);
    }
    [Fact] void should_delete_the_selected_key_and_keep_the_other()
    {
        var model = _result.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id).Passed.ShouldBeTrue();
    }
    [Fact] void should_fail_when_the_key_is_still_present()
    {
        var model = _result.Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var original = plan.Specifications.Values.Single();
        var key = original.ThenReadModels.Single().Key;
        var assertion = original with { ThenAbsentReadModels = [new(original.ThenReadModels.Single().ReadModel, key)], ThenReadModels = [] };
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var changed = slice with { Specifications = [assertion] };
        var feature = model.Application.Modules.Single().Features.Single() with { Slices = [changed] };
        var module = model.Application.Modules.Single() with { Features = [feature] };
        var revised = ExecutableSemanticModel.Create(LanguageVersion.V5, SemanticVersion.V5, model.Application with { Modules = [module] });
        var run = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(revised).Plan!, assertion.Id);
        run.Passed.ShouldBeFalse();
        run.Failures.Single().ShouldContain("to be absent");
    }
    [Fact] void should_accept_whenless_absence_of_a_different_key()
    {
        var result = Bind(Source.Replace("        when append InvoiceRemoved\n          invoiceId = \"first\"", "", StringComparison.Ordinal)
            .Replace("then no readmodel InvoiceView for \"first\"", "then no readmodel InvoiceView for \"third\"", StringComparison.Ordinal));
        result.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(result.Value!.Model).Plan!;
        plan.Specifications.Values.Single().WhenAppended.ShouldBeNull();
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id).Passed.ShouldBeTrue();
    }
    [Fact] void should_not_claim_reducer_deletions_passed()
    {
        var model = _result.Value!.Model;
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var reducer = new SemanticReducer("InvoiceReducer", slice.ReadModels.Single().Id, [new(slice.Events.Single().Id, new string('a', 64))]);
        var feature = model.Application.Modules.Single().Features.Single() with { Slices = [slice with { Projections = [], Reducers = [reducer] }] };
        var module = model.Application.Modules.Single() with { Features = [feature] };
        var revised = ExecutableSemanticModel.Create(LanguageVersion.V5, SemanticVersion.V5, model.Application with { Modules = [module] });
        var plan = SemanticExecutionPlan.Compile(revised).Plan!;
        var run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        run.Passed.ShouldBeFalse();
        (run.Execution is SemanticUnsupported).ShouldBeTrue();
        run.Failures.Single().ShouldContain("InvoiceReducer");
    }
    [Fact] void should_reject_an_unknown_view() => Bind(Source.Replace("then no readmodel InvoiceView", "then no readmodel UnknownView", StringComparison.Ordinal)).Success.ShouldBeFalse();
    [Fact] void should_reject_a_wrongly_typed_key() => Bind(Source.Replace("for \"first\"", "for 1", StringComparison.Ordinal)).Success.ShouldBeFalse();
    [Fact] void should_reject_contradictory_keys() => Bind(Source.Replace("for \"first\"", "for \"second\"", StringComparison.Ordinal)).Success.ShouldBeFalse();
    [Fact] void should_reject_a_query_result_claiming_the_absent_instance_is_present()
    {
        var result = Bind(Source.Replace(
            "        then readmodel InvoiceView\n          invoiceId = \"second\"",
            "        then query InvoiceById\n          arguments\n            invoiceId = \"first\"\n          result\n            invoiceId = \"first\"",
            StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single(_ => _.Message.Contains("presence and absence", StringComparison.Ordinal)).Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    }
    [Fact] void should_reject_duplicate_absences() => Bind(Source.Replace("        then readmodel InvoiceView", "        then no readmodel InvoiceView for \"first\"\n        then readmodel InvoiceView", StringComparison.Ordinal)).Success.ShouldBeFalse();
}
