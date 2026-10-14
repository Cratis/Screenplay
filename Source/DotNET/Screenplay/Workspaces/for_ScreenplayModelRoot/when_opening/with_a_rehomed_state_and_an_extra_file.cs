// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class with_a_rehomed_state_and_an_extra_file : given.a_persisted_model_root
{
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        var path = PortablePlayPath.Parse("Projects/Registration/Registration.play");
        var rehomed = Original.Rehome([WorkspaceDocument.Create(ModelRootDocumentKeys.For(path), path, Encoding.UTF8.GetBytes(Source))]);
        Directory.CreateDirectory(Path.Combine(RootPath, "Projects", "Registration"));
        File.WriteAllBytes(Path.Combine(RootPath, path.Value), [.. rehomed.Documents.Single().Bytes]);
        File.Delete(Path.Combine(RootPath, "application.play"));
        File.WriteAllBytes(IdentitiesPath, PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(rehomed)));
        File.WriteAllText(Path.Combine(RootPath, "extra.play"), "concept Extra : String\n");
    }

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_refuse_unmapped_files() => _outcome.Success.ShouldBeFalse();
    [Fact] void should_report_the_mapping_conflict() => _outcome.FailureKind.ShouldEqual("IdentityMappingConflict");
}
