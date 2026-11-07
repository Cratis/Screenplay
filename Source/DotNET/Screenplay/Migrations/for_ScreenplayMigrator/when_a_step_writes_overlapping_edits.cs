// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrator;

public class when_a_step_writes_overlapping_edits : Specification
{
    const string Source = "module Shop\n";
    MigrationReport _report;

    void Because()
    {
        var migrator = new ScreenplayMigrator([new OverlappingStep()]);
        _report = migrator.Migrate([new PlayFileContent("document.play", Source)], migrator.Plan(5, 6));
    }

    [Fact] void should_fail() => _report.Outcome.ShouldEqual(MigrationOutcome.Failed);
    [Fact] void should_leave_the_text_as_it_was() => _report.Files.Single().Migrated.ShouldEqual(Source);
    [Fact] void should_say_why() => _report.Files.Single().Failure.ShouldContain("Two edits change line 1");

    sealed class OverlappingStep : IScreenplayMigration
    {
        public ScreenplayVersion From => 5;
        public ScreenplayVersion To => 6;
        public MigrationName Name => "overlapping";

        public MigrationResult Apply(DocumentSyntax document)
        {
            var slot = new SlotSyntax("content", SourceLocation.Start);
            return new(document, [new(SourceLocation.Start, "module Shop", "content", [new ReplaceLine(1, [slot]), new RemoveLine(1)])], []);
        }
    }
}
