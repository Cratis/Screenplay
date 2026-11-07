// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets.given;

/// <summary>
/// Migrates a Screenplay 5 fixture from <c>fixtures/&lt;name&gt;.before.play.txt</c> and reads the Screenplay 6 text it
/// should become from <c>fixtures/&lt;name&gt;.after.play.txt</c> - a fixture without one is a document the step only
/// reviews.
/// </summary>
public class a_fixture : Specification
{
    protected FileMigration _result;
    protected string _expected;

    protected void Migrate(string fixture)
    {
        var before = File.ReadAllText(FixturePath($"{fixture}.before.play.txt"));
        var after = FixturePath($"{fixture}.after.play.txt");
        _expected = File.Exists(after) ? File.ReadAllText(after) : before;
        var migrator = new ScreenplayMigrator();
        _result = migrator.Migrate([new PlayFileContent("document.play", before)], migrator.Plan(5, 6)).Files.Single();
    }

    protected void MigrateSource(string source)
    {
        _expected = source;
        var migrator = new ScreenplayMigrator();
        _result = migrator.Migrate([new PlayFileContent("document.play", source)], migrator.Plan(5, 6)).Files.Single();
    }

    protected MigrationReview ReviewAt(int line) => _result.Reviews.Single(review => review.Location.Line == line);

    static string FixturePath(string name, [CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "fixtures", name));
}
