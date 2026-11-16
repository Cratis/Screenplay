// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_a_free_screen_with_an_unprovided_need : Specification
{
    const string Source = """
        shape Book
          title String

        screen BookCatalog
          data Book[]
          table Book
            column title

        module Lending
          feature Catalog
            slice StateChange AddBook
              command AddBook
                bookId Uuid identifier
                title String
                produces BookAdded
                  for bookId
                  title = title
              event BookAdded
                title String
              screen AddBook
                title "Add a book"
                action AddBook

            slice StateView Books
              readmodel CatalogEntry
                isbn Uuid key
                title String
              projection CatalogProjection => CatalogEntry
                from BookAdded
                  isbn = $eventSourceId
                  title = title
              query AllBooks => CatalogEntry[]
              uses screen BookCatalog
        """;

    ExecutableScreensResult _result;

    void Because() => _result = ExecutableScreens.Admit(ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace);

    [Fact] void should_refuse() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_screen_and_the_shape() => _result.Refusals.ShouldContainOnly(["Screen 'BookCatalog' adopted by 'Lending.Catalog.Books' needs 'Book' of shape 'Book', and slice 'Books' does not provide it."]);
}
