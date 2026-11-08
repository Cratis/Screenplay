// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Api.for_ApiNames;

public class when_spelling_route_segments : Specification
{
    [Fact] void should_put_a_dash_between_words() => ApiNames.Segment("AllBooks").ShouldEqual("all-books");
    [Fact] void should_capitalize_a_lower_case_name_first() => ApiNames.Segment("allBooks").ShouldEqual("all-books");
    [Fact] void should_put_a_dash_before_every_acronym_letter() => ApiNames.Segment("GetHTMLPage").ShouldEqual("get-h-t-m-l-page");
    [Fact] void should_join_authored_word_separators() => ApiNames.Segment("book_shelf").ShouldEqual("book-shelf");
    [Fact] void should_spell_json_names_in_camel_case() => ApiNames.Camel("BookId").ShouldEqual("bookId");
    [Fact] void should_join_the_prefix_and_segments() => ApiNames.Route("/api/", ["Lending", "Catalog"]).ShouldEqual("/api/lending/catalog");
}
