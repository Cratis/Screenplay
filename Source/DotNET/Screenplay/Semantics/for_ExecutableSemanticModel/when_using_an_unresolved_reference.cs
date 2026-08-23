// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_an_unresolved_reference : a_valid_semantic_model
{
    Exception _exception;

    void Because()
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var viewSlice = feature.Slices.Single(_ => _.Queries.Length > 0);
        var viewSliceIndex = feature.Slices.IndexOf(viewSlice);
        var query = viewSlice.Queries.Single() with { ReadModel = Id(SemanticKind.ReadModel, "Missing") };
        var invalidSlice = viewSlice with { Queries = [query] };
        var invalidFeature = feature with { Slices = feature.Slices.SetItem(viewSliceIndex, invalidSlice) };
        var invalidModule = module with { Features = module.Features.SetItem(0, invalidFeature) };
        var invalidApplication = _application with { Modules = _application.Modules.SetItem(0, invalidModule) };
        _exception = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, invalidApplication));
    }

    [Fact] void should_reject_the_model() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
