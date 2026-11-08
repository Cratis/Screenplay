// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_screens : given.a_contract
{
    [Fact] void should_name_the_contract_version() => Contract.Contract.ShouldEqual(UiContract.Version);
    [Fact] void should_carry_the_executable_model_revision() => Contract.ModelRevision.ShouldEqual(Workspace.Compilation.Value!.Model.Revision.ToString());
    [Fact] void should_list_every_screen_in_declaration_order() => Contract.Screens.Select(screen => screen.Name).ShouldContainOnly(["AddBook", "BorrowBook", "BookCatalog", "BookDetails", "CatalogCards"]);
    [Fact] void should_name_the_declaring_slice() => Screen("BookDetails").Slice.ShouldEqual("Lending.Catalog.BookCatalog");
    [Fact] void should_carry_the_slice_identity() => Screen("BookDetails").SliceId.ShouldEqual(Workspace.Compilation.Value!.Model.Application.Modules[0].Features[0].Slices[2].Id.ToString());
    [Fact] void should_take_the_title_from_a_template_slot() => Screen("AddBook").Title.ShouldEqual("Add a book");
    [Fact] void should_take_the_top_level_title() => Screen("BookCatalog").Title.ShouldEqual("Catalog");
}
