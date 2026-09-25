// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_cataloging_a_dangling_reducer : given.a_semantic_binder
{
    Exception? _error;

    void Because()
    {
        var result = Bind("""
            module Billing
              feature Accounts
                slice StateView Balances
                  event Deposited
                    amount Decimal
                  readmodel Balance
                    id Uuid
                  query ById => Balance?
                    by id Uuid
                  reducer Fold => Balance
                    on Deposited
                      file Reducer.cs
            """);
        var application = result.Value!.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var dangling = application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { ReadModels = [] }] }] }]
        };
        _error = Catch.Exception(() => SemanticTypedContextCatalog.Create(dangling, result.ImplementationRequirements, true));
    }

    [Fact] void should_fail_closed_in_the_catalog_itself() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
