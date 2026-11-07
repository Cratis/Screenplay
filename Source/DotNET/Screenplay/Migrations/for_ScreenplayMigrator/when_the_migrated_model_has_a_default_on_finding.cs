// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrator;

/// <summary>
/// The compile after the migration reports what a normal compile reports, the checks that are on by default included -
/// here a State Change command nothing issues.
/// </summary>
public class when_the_migrated_model_has_a_default_on_finding : Specification
{
    const string Source =
        """
        layout AppShell
          category Shell
          outlet content
          arrangement flow
            row
              content grow

        module Shop
          feature Customers
            slice StateChange RegisterCustomer
              command RegisterCustomer
                name String
        """;

    FileMigration _result;

    void Because()
    {
        var migrator = new ScreenplayMigrator();
        _result = migrator.Migrate([new PlayFileContent("document.play", Source)], migrator.Plan(5, 6)).Files.Single();
    }

    [Fact] void should_migrate_the_document() => _result.Migrated.ShouldContain("  slot content\n");
    [Fact] void should_report_the_command_nothing_issues() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.CommandWithoutInputSurface);
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_locate_it_in_the_document() => (_result.Diagnostics.Single().Location.Path, _result.Diagnostics.Single().Location.Line).ShouldEqual(("document.play", 11));
    [Fact] void should_not_need_review_for_a_warning() => _result.Outcome.ShouldEqual(MigrationOutcome.Migrated);
}
