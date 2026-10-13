// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_admitting_a_shipped_model : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;
    string _shippedPath = null!;

    void Establish()
    {
        _shippedPath = Path.Combine(RootPath, "shipped.json");
        File.WriteAllBytes(_shippedPath, SemanticModelSerializer.Serialize(Original.Compilation.Value!.Model));
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("name ProjectName", "name ProjectName?", StringComparison.Ordinal));
    }

    void Because() => _outcome = ReleaseBaselineCommands.Admit(RootPath, "shipped", _shippedPath);

    [Fact] void should_admit() => _outcome.ExitCode.ShouldEqual(0);
    [Fact] void should_report_the_count() => _outcome.AdmittedEntryCount.ShouldEqual(1);
    [Fact] void should_record_the_shipped_shape() => ReleaseBaselineStore.ReadBytes(File.ReadAllBytes(BaselinePath), Original.IdentityCatalog.Application).Document!.Entries.Single().Schema.Properties.Single(property => property.Name == "name").Optional.ShouldBeFalse();
    [Fact] void should_detect_later_source_changes() => ReleaseBaselineCommands.Check(RootPath).ExitCode.ShouldEqual(1);
}
