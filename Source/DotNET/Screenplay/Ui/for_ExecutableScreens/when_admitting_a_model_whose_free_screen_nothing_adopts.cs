// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_a_model_whose_free_screen_nothing_adopts : Specification
{
    const string Source = """
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
        """;

    ExecutableScreensResult _result;

    void Because() => _result = ExecutableScreens.Admit(ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace);

    [Fact] void should_not_refuse_the_design_only_screen() => _result.Success.ShouldBeTrue();
    [Fact] void should_leave_it_out_of_the_executable_model() => _result.Compilation!.Model.Application.Screens.ShouldBeEmpty();
}
