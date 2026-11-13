// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_screen_data : given.a_contract
{
    UiData Catalog => Screen("BookCatalog").Data.Single();
    UiData Details => Screen("BookDetails").Data.Single();

    [Fact] void should_name_the_query() => Catalog.Query.ShouldEqual("AllBooks");
    [Fact] void should_name_the_read_model() => Catalog.ReadModel.ShouldEqual("CatalogEntry");
    [Fact] void should_show_a_list_query_as_many() => Catalog.Many.ShouldBeTrue();
    [Fact] void should_show_a_single_query_as_one() => Details.Many.ShouldBeFalse();
    [Fact] void should_name_the_value_it_is_narrowed_by() => Details.By.ShouldEqual("bookId");
    [Fact] void should_list_the_table_columns_as_fields() => Catalog.Fields.ShouldContainOnly([new UiField("title", "Title")]);
    [Fact] void should_list_the_summary_fields() => Details.Fields.ShouldContainOnly([new UiField("title", "Title")]);
    [Fact] void should_route_the_query() => Catalog.Route.ShouldEqual("/api/lending/catalog/book-catalog/all-books");
    [Fact] void should_carry_the_query_identity() => Catalog.QueryId.ShouldEqual(Workspace.Compilation.Value!.Model.Application.Modules[0].Features[0].Slices[2].Queries[0].Id.ToString());
    [Fact] void should_show_an_observable_query_as_live() => Catalog.Live.ShouldBeTrue();
}
