// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_navigation_and_layout : given.a_contract
{
    UiLayout Dialog => Screen("AddBook").Layout;

    [Fact] void should_lead_from_a_row_to_its_details() => Screen("BookCatalog").Navigation.ShouldContainOnly([new UiNavigation("BookDetails", "row") { By = "bookId" }]);
    [Fact] void should_name_the_template() => Dialog.Template.ShouldEqual("Dialog");
    [Fact] void should_lay_out_the_template_slots() => Dialog.Regions.Single().Regions.Select(region => region.Name).ShouldContainOnly(["body", "actions"]);
    [Fact] void should_place_an_action_in_its_slot() => Dialog.Regions.Single().Regions.Single(region => region.Name == "actions").Actions.ShouldContainOnly(["AddBook"]);
    [Fact] void should_show_a_table_as_a_list() => Screen("BookCatalog").Layout.Regions.Single().Kind.ShouldEqual("list");
    [Fact] void should_show_a_summary_as_details() => Screen("BookDetails").Layout.Regions.Single().Shows.ShouldEqual("CatalogEntry");
}
