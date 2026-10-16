// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_composite_read_model_keys : Specification
{
    public const string Source = """
        module Billing
          feature Totals
            slice StateView Payments
              event Recorded
                invoice String
                method String
                amount Int
              event Removed
                invoice String
                method String
              readmodel Totals
                invoice String key
                method String key
                amount Int
              query Total => Totals optional
                by
                  method String
                  invoice String
              query ForInvoice => Totals[]
                filter invoice String
              projection Totals => Totals
                no automap
                from Recorded
                  key
                    method = method
                    invoice = invoice
                  amount = amount
                remove with Removed
                  key
                    invoice = invoice
                    method = method
              specification Lookup
                given readmodel Totals
                  invoice = "first"
                  method = "card"
                  amount = 5
                given readmodel Totals
                  invoice = "first"
                  method = "cash"
                  amount = 3
                when query Total
                  method = "cash"
                  invoice = "first"
                then result
                  amount = 3
              specification Remove
                given readmodel Totals
                  invoice = "first"
                  method = "card"
                  amount = 5
                given readmodel Totals
                  invoice = "first"
                  method = "cash"
                  amount = 3
                when append Removed
                  invoice = "first"
                  method = "card"
                then no readmodel Totals for {"method":"card","invoice":"first"}
                then readmodel Totals
                  invoice = "first"
                  method = "cash"
                  amount = 3
              specification Recreate
                given Recorded
                  invoice = "first"
                  method = "card"
                  amount = 5
                given Removed
                  invoice = "first"
                  method = "card"
                when append Recorded
                  invoice = "first"
                  method = "card"
                  amount = 7
                then readmodel Totals
                  invoice = "first"
                  method = "card"
                  amount = 7
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because()
    {
        var identity = ApplicationIdentity.Create("CompositeKeys");
        var catalog = SemanticIdentityCatalog.Empty(identity);
        var source = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", Source);
        _result = new SemanticModelCompiler().Compile("CompositeKeys", SemanticDocumentSet.Create([source], catalog));
    }

    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_the_admitting_version() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V27);
    [Fact] void should_mark_both_parts_as_identifiers() => Slice.ReadModels.Single().Properties.Count(property => property.IsIdentifier).ShouldEqual(2);
    [Fact] void should_bind_the_untyped_from_key() => Slice.Projections.Single().Scope!.From.Single().Key.ShouldBeOfExactType<SemanticProjectionPartsKey>();
    [Fact] void should_bind_the_untyped_removal_key() => Slice.Projections.Single().Scope!.Removals.Single().Key.ShouldBeOfExactType<SemanticProjectionPartsKey>();
    [Fact] void should_keep_the_query_parts_distinct() => Slice.Queries.Single(query => query.Name == "Total").KeyParts.Select(part => part.Name).ShouldContainOnly("invoice", "method");

    [Fact]
    void should_execute_lookup_removal_and_recreation()
    {
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        Slice.Specifications.Length.ShouldEqual(3);
        foreach (var specification in Slice.Specifications)
        {
            var run = new SemanticSpecificationRunner().Run(plan, specification.Id);
            Assert.True(run.Passed, $"{specification.Name}: {string.Join('\n', run.Failures)}");
        }
    }

    [Fact]
    void should_round_trip_the_canonical_model()
    {
        var bytes = SemanticModelSerializer.Serialize(_result.Value!.Model);
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).SequenceEqual(bytes).ShouldBeTrue();
    }

    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
}
