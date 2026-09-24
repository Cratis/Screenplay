// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_a_reducer : Specification
{
    const string Source =
        """
        module Billing
          feature Accounts
            slice StateView Balance
              event AmountDeposited
                amount Decimal
              readmodel AccountBalance
                id Uuid
              query BalanceById => AccountBalance?
                by id Uuid
              reducer BalanceReducer => AccountBalance
                on AmountDeposited
                  file Reducers/Deposited.cs
        """;

    string _reducerJson;
    byte[] _bytes;
    ExecutableSemanticModel _model;
    SemanticImplementationRequirement _requirement;
    int _schemaVersion;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("balance"), "balance", "Balance.play", Source);
        var bound = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        _model = bound.Value!.Model;
        _requirement = bound.ImplementationRequirements.Single();
        _bytes = SemanticModelSerializer.Serialize(_model);
        using var json = JsonDocument.Parse(_bytes);
        _schemaVersion = json.RootElement.GetProperty("schemaVersion").GetInt32();
        _reducerJson = json.RootElement.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0]
            .GetProperty("slices")[0].GetProperty("reducers")[0].GetRawText();
    }

    [Fact] void should_use_schema_v3_only_for_bodied_reducers() => _schemaVersion.ShouldEqual(3);
    [Fact] void should_pin_the_complete_reducer_shape()
    {
        var reducer = _model.Application.Modules.Single().Features.Single().Slices.Single().Reducers.Single();
        var expected = $"{{\"name\":\"BalanceReducer\",\"readModel\":\"{reducer.ReadModel}\",\"key\":\"eventSourceId\",\"initialState\":null,\"result\":\"stateOrDelete\",\"transitions\":[{{\"eventContract\":\"{reducer.Transitions.Single().EventContract}\",\"requirementId\":\"{_requirement.RequirementId}\"}}]}}";
        _reducerJson.ShouldEqual(expected);
    }

    [Fact] void should_round_trip_byte_identically() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).SequenceEqual(_bytes).ShouldBeTrue();
}
