// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_adopted_free_screens : Specification
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

    ExecutableScreensResult _result;
    ExecutableSemanticModel _model;
    SemanticSlice _slice;

    void Because()
    {
        _result = ExecutableScreens.Admit(ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace);
        _model = _result.Compilation!.Model;
        _slice = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Books");
    }

    SemanticScreen Catalog => _model.Application.Screens.Single(screen => screen.Name == "BookCatalog");

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v56() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V56);
    [Fact] void should_hold_both_free_screens_on_the_application() => _model.Application.Screens.Select(screen => screen.Name).ShouldContainOnly(["BookCatalog", "BookPage"]);
    [Fact] void should_declare_no_screen_on_the_slice() => _slice.Screens.ShouldBeEmpty();
    [Fact] void should_adopt_both_screens() => _slice.Adoptions.Select(adoption => adoption.Screen).ShouldContainOnly(_model.Application.Screens.Select(screen => screen.Id));
    [Fact] void should_keep_the_need_of_the_catalog() => (Catalog.Needs.Single().Name, Catalog.Needs.Single().Shape, Catalog.Needs.Single().Many, Catalog.Needs.Single().Fields.Single().Property).ShouldEqual(("Book", "Book", true, "title"));
    [Fact] void should_provide_the_need_from_the_query() => _slice.Adoptions.Single(adoption => adoption.Screen == Catalog.Id).Provisions.Single().Query.ShouldEqual(_slice.Queries.Single(query => query.Name == "AllBooks").Id);
    [Fact] void should_keep_the_field_mapping() => _slice.Adoptions.Single(adoption => adoption.Screen == Catalog.Id).Provisions.Single().Fields.ShouldContainOnly([new SemanticScreenProvisionField("title", "name")]);
    [Fact] void should_keep_the_screen_the_row_click_leads_to() => Catalog.Navigation.Single().Screen.ShouldEqual(_model.Application.Screens.Single(screen => screen.Name == "BookPage").Id);
    [Fact] void should_round_trip_through_the_canonical_form() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_model))).ShouldEqual(SemanticModelSerializer.Serialize(_model));
}
