// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticReducerKeyConversion;

public class when_the_key_is_composite : given.a_reducer_model
{
    SemanticApplication _model;
    SemanticTypeReference _result;

    void Establish() => _model = WithIdentifiers(Model("        first Uuid key\n        second String"), "first", "second");

    void Because() => _result = SemanticReducerKeyConversion.KeyType(_model, Reducer(_model));

    [Fact] void should_not_return_a_key_type() => _result.ShouldBeNull();
}
