// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_adopted_free_screens : Specification
{
    const string Source = """
        shape Book
          title String
          isbn Uuid

        screen BookCatalog
          title "Books"
          data Book[]
          table Book
            column title label "Title"
            on row-click navigate to BookPage by isbn

        screen BookPage
          title "Book"
          data Book
          summary Book
            field title label "Title"

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
                name String
              projection CatalogProjection => CatalogEntry
                from BookAdded
                  isbn = $eventSourceId
                  name = title
              query AllBooks => CatalogEntry[]
              query BookByIsbn => CatalogEntry optional
                by isbn Uuid
              uses screen BookCatalog
                data Book via query AllBooks
                  title from name
              uses screen BookPage
                data Book via query BookByIsbn by isbn
                  title from name
        """;

    UiContract _contract;
    UiContract _fromModel;

    void Because()
    {
        var workspace = ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace;
        _contract = UiContractBuilder.For(workspace);
        _fromModel = UiContractBuilder.For(ExecutableScreens.Admit(workspace).Compilation!.Model);
    }

    UiScreen Catalog => _contract.Screens.Single(screen => screen.Name == "BookCatalog");

    [Fact] void should_read_each_adopted_screen_once() => _contract.Screens.Select(screen => screen.Name).ShouldContainOnly(["AddBook", "BookCatalog", "BookPage"]);
    [Fact] void should_name_the_adopting_slice() => Catalog.Slice.ShouldEqual("Lending.Catalog.Books");
    [Fact] void should_resolve_the_data_from_the_provision() => Catalog.Data.Single().Query.ShouldEqual("AllBooks");
    [Fact] void should_name_the_read_model_of_the_query() => Catalog.Data.Single().ReadModel.ShouldEqual("CatalogEntry");
    [Fact] void should_map_the_field_to_the_read_model_path() => Catalog.Data.Single().Fields.ShouldContainOnly([new UiField("name", "Title")]);
    [Fact] void should_carry_the_value_the_query_is_narrowed_by() => _contract.Screens.Single(screen => screen.Name == "BookPage").Data.Single().By.ShouldEqual("isbn");
    [Fact] void should_lead_the_row_click_to_the_other_free_screen() => Catalog.Navigation.Single().Screen.ShouldEqual("BookPage");
    [Fact] void should_read_the_same_screens_from_the_executable_model() => _fromModel.Screens.Select(screen => screen.Name).ShouldContainOnly(["AddBook", "BookCatalog", "BookPage"]);
    [Fact] void should_read_the_provided_data_from_the_executable_model() => _fromModel.Screens.Single(screen => screen.Name == "BookCatalog").Data.Single().Fields.ShouldContainOnly([new UiField("name", "Title")]);
    [Fact] void should_read_the_adopting_slice_from_the_executable_model() => _fromModel.Screens.Single(screen => screen.Name == "BookCatalog").Slice.ShouldEqual("Lending.Catalog.Books");
}
