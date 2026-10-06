// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_executing_generated_values : a_v6_scenario
{
    const string First = "11111111-1111-1111-1111-111111111111";
    const string Second = "22222222-2222-2222-2222-222222222222";
    const string Source = "concept Id : Uuid\npolicy SignedIn\n  require authenticated\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        receipt Id generated\n        name String\n        validate\n          name min 1\n        authorize SignedIn\n        produces event Created\n          id Id = id\n          receipt Id = receipt\n          name String = name\n        returns id";

    void Establish() => Compile(Source);

    SemanticCommand Command => _plan.Commands.Values.Single();
    SemanticPropertyValue Value(string name, SemanticValue value) => new(Command.Properties.Single(property => property.Name == name).Id, value);
    SemanticExecutionRequest Request => SemanticExecutionRequest.Create(Command.Id, [Value("name", SemanticValue.Text("hello"))], []) with
    {
        Caller = new(true, [], []),
        GeneratedValues = [Value("id", SemanticValue.Text(First)), Value("receipt", SemanticValue.Text(Second))]
    };

    SemanticExecutionResult Execute(SemanticExecutionRequest request) => new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request);

    [Fact]
    void should_generate_complete_values_for_productions_and_response()
    {
        var result = (SemanticAccepted)Execute(Request);
        result.Facts.Single().Destination.ShouldEqual(SemanticValue.Text(First));
        result.Facts.Single().Values.Select(value => value.Value).ShouldContain(SemanticValue.Text(Second));
        ((SemanticScalarExecutionResponse)result.Response!).Value.ShouldEqual(SemanticValue.Text(First));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_treat_default_and_empty_fixtures_as_missing(bool defaults)
    {
        var result = Execute(Request with { GeneratedValues = defaults ? default : [] });
        ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
        result.World.Facts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("null", false)]
    [InlineData("null", true)]
    [InlineData("foreign", false)]
    [InlineData("foreign", true)]
    [InlineData("input", false)]
    [InlineData("input", true)]
    [InlineData("type", false)]
    [InlineData("type", true)]
    [InlineData("value", false)]
    [InlineData("value", true)]
    [InlineData("duplicate", false)]
    [InlineData("duplicate", true)]
    void should_validate_all_supplied_fixtures_before_missing_entries_in_either_order(string defect, bool reverse)
    {
        var supplied = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
        supplied.Add(Value(reverse ? "receipt" : "id", SemanticValue.Text(First)));
        if (defect == "type" || defect == "value") supplied.Clear();
        supplied.Add(defect switch
        {
            "null" => null!,
            "foreign" => new(_plan.Model.Application.Concepts.Single().Id, SemanticValue.Text(First)),
            "input" => Value("name", SemanticValue.Text("hello")),
            "type" => Value(reverse ? "receipt" : "id", SemanticValue.Number(4)),
            "value" => Value(reverse ? "receipt" : "id", null!),
            _ => supplied[0]
        });
        var fixtures = supplied.ToImmutable();
        if (reverse) fixtures = [.. fixtures.Reverse()];
        var result = Execute(Request with { GeneratedValues = fixtures });
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.World.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_generate_a_nonidentifier_without_a_generated_identifier()
    {
        Compile("concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        receipt Id generated\n        returns receipt");
        var accepted = (SemanticAccepted)Execute(SemanticExecutionRequest.Create(Command.Id, [], []) with
        {
            GeneratedValues = [Value("receipt", SemanticValue.Text(First))]
        });
        accepted.Facts.ShouldBeEmpty();
        ((SemanticScalarExecutionResponse)accepted.Response!).Value.ShouldEqual(SemanticValue.Text(First));
    }

    [Fact]
    void should_reject_generated_property_as_request_input()
    {
        var result = Execute(Request with { Values = Request.Values.Add(Value("id", SemanticValue.Text(First))) });
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    [Fact]
    void should_deny_before_inspecting_inputs_or_fixtures()
    {
        var result = Execute(Request with { Caller = null, Values = default, GeneratedValues = [null!] });
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }

    [Fact]
    void should_validate_inputs_before_inspecting_fixtures()
    {
        var result = Execute(Request with { Values = [Value("name", SemanticValue.Text(string.Empty))], GeneratedValues = [null!] });
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    }

    [Fact]
    void should_keep_direct_query_failure_inside_the_atomic_boundary()
    {
        var result = Execute(Request with { Queries = [new(Command.Id, SemanticValue.Text("unknown"))] });
        ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.Query);
        result.World.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_not_share_a_generated_identifier_fixture_with_legacy_allocation()
    {
        Compile("concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        produces Created\n        returns id\n      event Created");
        var request = SemanticExecutionRequest.Create(Command.Id, [], []) with { GeneratedValues = [Value("id", SemanticValue.Text(First))] };
        ((SemanticUnsupported)Execute(request)).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
        var accepted = (SemanticAccepted)Execute(request with
        {
            AllocatedIdentities = ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(Command.Id, SemanticValue.Text(Second)),
            AllocatedEventSourceType = Command.Properties.Single().Type
        });
        accepted.Facts.Single().Destination.ShouldEqual(SemanticValue.Text(Second));
        ((SemanticScalarExecutionResponse)accepted.Response!).Value.ShouldEqual(SemanticValue.Text(First));
    }
}
