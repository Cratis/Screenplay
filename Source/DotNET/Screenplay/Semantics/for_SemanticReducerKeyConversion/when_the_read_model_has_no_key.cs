// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticReducerKeyConversion;

public class when_the_read_model_has_no_key : given.a_reducer_model
{
    SemanticApplication _model;
    SemanticTypeReference _result;

    void Establish() => _model = WithIdentifiers(Model("        id Uuid key"));

    void Because() => _result = SemanticReducerKeyConversion.KeyType(_model, Reducer(_model));

    [Fact] void should_not_return_a_key_type() => _result.ShouldBeNull();
}
