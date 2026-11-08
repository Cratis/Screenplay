// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_the_model_has_no_executable_model : Specification
{
    // A projection to a read model nobody declares keeps the sources readable while the executable model is refused.
    const string Source = """
        concept BookId : Uuid
        module Lending
          feature Catalog
            slice StateView BookCatalog
              readmodel CatalogEntry
                bookId BookId
              query AllBooks => CatalogEntry[]
              projection Broken => Missing
                from Nothing
                  bookId = $eventSourceId
              screen BookCatalog
                data CatalogEntry[] via query AllBooks
        """;

    UiContract _contract;
    bool _hasModel;

    void Because()
    {
        var workspace = ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace;
        _hasModel = workspace.Compilation.Success;
        _contract = UiContractBuilder.For(workspace);
    }

    [Fact] void should_have_no_executable_model() => _hasModel.ShouldBeFalse();
    [Fact] void should_still_describe_the_screen() => _contract.Screens.Single().Data.Single().Query.ShouldEqual("AllBooks");
    [Fact] void should_carry_no_model_revision() => _contract.ModelRevision.ShouldBeNull();
    [Fact] void should_carry_no_routes() => _contract.Screens.Single().Data.Single().Route.ShouldBeNull();
}
