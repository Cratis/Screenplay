// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api.for_ApiDescription.given;

public class a_lending_model : Specification
{
    internal const string Source = """
        concept BookId : Uuid
        concept BookTitle : String

        policy Librarian
          require role "Librarian"

        policy Member
          require authenticated

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

            slice StateChange ShelveBook
              command ShelveBook
                bookId BookId identifier
                produces BookShelved
                  for bookId
              command RetireBook
                bookId BookId identifier
                authorize Librarian or Member
                produces BookRetired
                  for bookId
              event BookShelved
              event BookRetired

            slice StateView CatalogList
              readmodel CatalogEntry
                bookId BookId
                title BookTitle
              query AllBooks => observable CatalogEntry[]
                authorize Member
              query BookById => CatalogEntry optional
                by bookId BookId
              projection CatalogProjection => CatalogEntry
                from BookAdded
                  bookId = $eventSourceId
                  title = title
        """;

    protected ExecutableSemanticModel Model;

    void Establish() => Model = ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source })
        .Workspace.Compilation.Value!.Model!;
}
