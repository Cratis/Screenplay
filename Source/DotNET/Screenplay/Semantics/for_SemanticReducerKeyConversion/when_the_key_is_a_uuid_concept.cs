// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticReducerKeyConversion;

public class when_the_key_is_a_uuid_concept : given.a_reducer_model
{
    SemanticApplication _model;
    SemanticTypeReference _result;

    void Establish() => _model = Model("        id TalkId key");

    void Because() => _result = SemanticReducerKeyConversion.KeyType(_model, Reducer(_model));

    [Fact] void should_return_the_concept() => _result.ShouldEqual(ConceptReference(_model, "TalkId"));
}
