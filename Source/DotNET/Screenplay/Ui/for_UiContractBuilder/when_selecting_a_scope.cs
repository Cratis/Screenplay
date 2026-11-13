// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_selecting_a_scope : given.a_contract
{
    [Fact] void should_keep_the_screens_of_a_slice() => UiContractBuilder.Select(Contract, "Lending.Catalog.BookCatalog").Screens.Select(screen => screen.Name).ShouldContainOnly(["BookCatalog", "BookDetails", "CatalogCards"]);
    [Fact] void should_keep_every_screen_of_a_module() => UiContractBuilder.Select(Contract, "Lending").Screens.Count.ShouldEqual(5);
    [Fact] void should_not_match_a_name_prefix() => Catch.Exception(() => UiContractBuilder.Select(Contract, "Lending.Catalog.Book")).ShouldBeOfExactType<InvalidSemanticModelScope>();
}
