// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder.given;

public class a_lending_model_with_screens : Specification
{
    internal const string Source = """
        concept BookId : Uuid
        concept BookTitle : String
        concept MemberId : Uuid

        policy Librarian
          require role "Librarian"

        module Lending
          feature Catalog
            slice StateChange AddBook
              command AddBook
                bookId BookId identifier
                title BookTitle
                authorize Librarian
                produces BookAdded
                  for bookId
                  title = title
              event BookAdded
                title BookTitle
              screen AddBook
                template Dialog
                  body
                    title "Add a book"
                  actions
                    action AddBook

            slice StateChange BorrowBook
              command BorrowBook
                bookId BookId identifier
                memberId MemberId
                produces BookBorrowed
                  for bookId
                  memberId = memberId
              event BookBorrowed
                memberId MemberId
              screen BorrowBook
                title "Borrow"
                action BorrowBook

            slice StateView BookCatalog
              readmodel CatalogEntry
                bookId BookId
                title BookTitle
              query AllBooks => observable CatalogEntry[]
              query BookById => CatalogEntry optional
                by bookId BookId
              projection CatalogProjection => CatalogEntry
                from BookAdded
                  bookId = $eventSourceId
                  title = title
              screen BookCatalog
                title "Catalog"
                data CatalogEntry[] via query AllBooks
                table CatalogEntry
                  column title label "Title"
                  on row-click navigate to BookDetails by bookId
                action AddBook for screen
                action BorrowBook for item
                  with bookId from item.bookId
              screen BookDetails
                title "Book"
                data CatalogEntry via query BookById by bookId
                summary CatalogEntry
                  field title label "Title"
              screen CatalogCards
                title "Cards"
                data CatalogEntry[] via query AllBooks
                component Cratis.Blueprint.Default.dataList catalogList
                  id "catalogList"
                  property rows from query AllBooks
                  property variant = "cards"
        """;

    protected ScreenplayWorkspace Workspace;

    void Establish() => Workspace = ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace;
}
