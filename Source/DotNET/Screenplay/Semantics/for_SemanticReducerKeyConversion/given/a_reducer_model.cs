// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticReducerKeyConversion.given;

public class a_reducer_model : for_SemanticModelBinder.given.a_semantic_binder
{
    protected SemanticApplication Model(string properties) => Application($$"""
        concept TalkId : Uuid
        concept TalkName : String
        concept Sequence : Int
        module Sample
          feature Feature
            slice StateView Reduce
              event Changed
                id Uuid
              readmodel Result
        {{properties}}
              reducer Apply => Result
                on Changed
                  file Handlers/Apply.cs
        """);

    protected SemanticApplication Application(string source)
    {
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model.Application;
    }

    // The binder refuses reducers over keyless or composite-key read models, so those shapes are
    // produced by rewriting the identifier flags of a bound single-key model.
    protected static SemanticApplication WithIdentifiers(SemanticApplication model, params string[] identifiers)
    {
        var module = model.Modules[0];
        var feature = module.Features[0];
        var slice = feature.Slices[0];
        var readModel = slice.ReadModels[0];
        var rewritten = readModel with
        {
            Properties = [.. readModel.Properties.Select(property => property with { IsIdentifier = identifiers.Contains(property.Name) })]
        };
        return model with { Modules = [module with { Features = [feature with { Slices = [slice with { ReadModels = [rewritten] }] }] }] };
    }

    protected static SemanticReducer Reducer(SemanticApplication model) => model.Modules[0].Features[0].Slices[0].Reducers.Single(reducer => reducer.Name == "Apply");

    protected static SemanticTypeReference ConceptReference(SemanticApplication model, string name) =>
        SemanticTypeReference.ForConcept(model.Concepts.Single(concept => concept.Name == name).Id);
}
