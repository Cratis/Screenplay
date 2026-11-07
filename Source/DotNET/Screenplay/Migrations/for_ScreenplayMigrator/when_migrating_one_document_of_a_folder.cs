// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrator;

/// <summary>
/// A file an import places in a module holds module content; the migration reads it there, not as a whole document.
/// </summary>
public class when_migrating_one_document_of_a_folder : Specification
{
    const string Root =
        """
        layout AppShell
          category Shell
          content
          arrangement flow
            row
              content grow

        module Shop
          import "screens.play"
          feature Orders
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
              screen OrderList
                data OrderRow[] via query AllOrders
        """;

    const string Screens =
        """
        // The module's screen furniture.
        screen template Board
          fits slot content
          category Workspace
          toolbar contributes BoardActions
          lanes
          arrangement flow
            column
              toolbar height 48
              lanes grow
        """;

    MigrationReport _report;

    void Because()
    {
        var migrator = new ScreenplayMigrator();
        _report = migrator.Migrate([new PlayFileContent("application.play", Root), new PlayFileContent("screens.play", Screens)], migrator.Plan(5, 6), path => path == "screens.play");
    }

    [Fact] void should_report_only_the_selected_document() => _report.Files.Select(file => file.Path).ShouldEqual(["screens.play"]);
    [Fact] void should_read_the_template_where_the_import_places_it() => _report.Files.Single().Changes.Single().Change.After.ShouldEqual("toolbar + outlet BoardActions of navigation in toolbar");
    [Fact] void should_compile_in_its_folder() => _report.Outcome.ShouldEqual(MigrationOutcome.Migrated);
    [Fact] void should_keep_the_comment() => _report.Files.Single().Migrated.StartsWith("// The module's screen furniture.\n", StringComparison.Ordinal).ShouldBeTrue();
}
