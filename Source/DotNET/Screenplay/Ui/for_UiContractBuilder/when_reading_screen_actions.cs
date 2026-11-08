// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractBuilder;

public class when_reading_screen_actions : given.a_contract
{
    UiAction Borrow => Screen("BookCatalog").Actions.Single(action => action.Command == "BorrowBook");

    [Fact] void should_list_every_action() => Screen("BookCatalog").Actions.Select(action => action.Command).ShouldContainOnly(["AddBook", "BorrowBook"]);
    [Fact] void should_apply_an_item_action_to_an_item() => Borrow.Subject.ShouldEqual("item");
    [Fact] void should_apply_a_screen_action_to_the_screen() => Screen("BookCatalog").Actions.Single(action => action.Command == "AddBook").Subject.ShouldEqual("screen");
    [Fact] void should_list_the_command_inputs() => Borrow.Inputs.Select(input => input.Name).ShouldContainOnly(["bookId", "memberId"]);
    [Fact] void should_fill_an_input_from_the_screen() => Borrow.Inputs.Single(input => input.Name == "bookId").From.ShouldEqual("item.bookId");
    [Fact] void should_leave_an_entered_input_unfilled() => Borrow.Inputs.Single(input => input.Name == "memberId").From.ShouldBeNull();
    [Fact] void should_type_each_input() => Borrow.Inputs.Single(input => input.Name == "memberId").Type.ShouldEqual("MemberId");
    [Fact] void should_route_the_command() => Borrow.Route.ShouldEqual("/api/lending/catalog/borrow-book");
    [Fact] void should_label_an_action_with_its_command_when_unlabeled() => Borrow.Label.ShouldEqual("BorrowBook");
}
