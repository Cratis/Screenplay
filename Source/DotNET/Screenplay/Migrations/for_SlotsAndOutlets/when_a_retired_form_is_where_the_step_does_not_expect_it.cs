// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

/// <summary>
/// The parser only keeps a retired form where Screenplay 5 allowed it. A tree that holds one anywhere else - from another
/// parser, or a future retired form - is left alone and reported for review rather than rewritten by guesswork.
/// </summary>
public class when_a_retired_form_is_where_the_step_does_not_expect_it : Specification
{
    const string Source = """
        layout AppShell
          category Shell
          content
          arrangement flow
            row
              content grow

        module Shop
          feature Orders
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
        """;

    MigrationResult _result;

    void Because()
    {
        var syntax = new ScreenplayCompiler().Parse(Source, "document.play", PlayPlacement.Document).Value!;
        var toolbar = new RetiredToolbar(new ScreenToolbarSyntax("actions", [], new(1, 1)), new(1, 1));
        var contribution = new RetiredSlotContribution(new SlotSyntax("content", new(3, 3)), "Main", new(3, 3));
        var module = syntax.Modules.Single();
        syntax = syntax with
        {
            RetiredForms = [toolbar],
            Layouts = [syntax.Layouts!.Single() with { RetiredForms = [new RetiredToolbar(new ScreenToolbarSyntax("actions", [], new(2, 3)), new(2, 3))] }],
            Modules = [module with { RetiredForms = [contribution with { Location = new(8, 1) }] }]
        };

        _result = new SlotsAndOutlets().Apply(new("document.play", Source, PlayPlacement.Document, syntax));
    }

    [Fact] void should_change_nothing() => _result.Changes.ShouldBeEmpty();
    [Fact] void should_report_the_unknown_form_on_the_application() => _result.Reviews.Single(review => review.Location.Line == 1).Message.ShouldEqual("a retired form this step does not know - rewrite it by hand");
    [Fact] void should_report_the_form_the_layout_cannot_hold() => _result.Reviews.Single(review => review.Location.Line == 2).Message.ShouldEqual("a layout AppShell cannot hold this retired form - rewrite it by hand");
    [Fact] void should_report_the_form_the_module_cannot_hold() => _result.Reviews.Single(review => review.Location.Line == 8).Message.ShouldEqual("a module cannot hold this retired form - rewrite it by hand");
    [Fact] void should_name_the_file() => _result.Reviews.All(review => review.Location.Path == "document.play").ShouldBeTrue();
}
