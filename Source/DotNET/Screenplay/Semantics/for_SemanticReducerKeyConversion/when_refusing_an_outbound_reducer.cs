// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticReducerKeyConversion;

public class when_refusing_an_outbound_reducer : given.a_reducer_model
{
    SemanticTypeReference _type;

    void Because()
    {
        var model = Bind("""
            module Sample
              feature Feature
                slice StateView Source
                  event Changed
                    id Uuid
                slice Translate Publish
                  direction outbound
                  public event Published
                    id Uuid
                  reducer Publish => Published
                    on Changed
                      file Handlers/Publish.cs
            """).Value!.Model.Application;
        var reducer = model.Modules[0].Features[0].Slices[1].Reducers.Single(reducer => reducer.Name == "Publish");
        _type = SemanticReducerKeyConversion.KeyType(model, reducer);
    }

    [Fact] void should_not_return_a_key_type() => _type.ShouldBeNull();
}
