// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticReducerKeyConversion;

public class when_admitting_a_key_in_a_nested_feature : given.a_reducer_model
{
    SemanticTypeReference _type;
    SemanticTypeReference _declaredType;

    void Because()
    {
        var model = Application("""
            concept InnerId : Uuid
            module Sample
              feature Outer
                feature Inner
                  slice StateView Reduce
                    event Changed
                      id InnerId
                    readmodel Result
                      id InnerId key
                    reducer Apply => Result
                      on Changed
                        file Handlers/Apply.cs
            """);
        var slice = model.Modules[0].Features[0].Features[0].Slices[0];
        _declaredType = slice.ReadModels[0].Properties[0].Type;
        _type = SemanticReducerKeyConversion.KeyType(model, slice.Reducers[0]);
    }

    [Fact] void should_preserve_the_declared_type_reference() => _type.ShouldEqual(_declaredType);
}
