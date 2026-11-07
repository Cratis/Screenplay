// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

public class when_a_file_needs_review : given.a_screenplay_5_model
{
    const string ToolbarWithCommandItem =
        """
        module Sales
          feature Orders
            toolbar actions
              item cancel action CancelOrder
            slice StateChange CancelOrder
              command CancelOrder
                orderId Uuid identifier
        """;

    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "orders.play"), ToolbarWithCommandItem);

    void Because() => _exitCode = ToolCommands.Run(["migrate", Root, "--from", "5"], Output, Error);

    [Fact] void should_exit_with_two() => _exitCode.ShouldEqual(2);
    [Fact] void should_say_what_needs_review_with_its_line() => Output.ToString().ShouldContain("4:7 needs review: toolbar item 'cancel' (action CancelOrder) has no outlet form");
    [Fact] void should_still_write_what_it_could() => File.ReadAllText(Path.Combine(Root, "orders.play")).ShouldContain("    outlet actions extends\n");
    [Fact] void should_say_review_is_needed() => Output.ToString().ShouldContain("Exit code 2: review needed.");
}
