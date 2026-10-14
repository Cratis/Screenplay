// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class and_a_document_was_removed : given.a_persisted_model_root
{
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish() => File.Delete(Path.Combine(RootPath, "application.play"));

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_refuse() => _outcome.Success.ShouldBeFalse();
    [Fact] void should_report_a_mapping_conflict() => _outcome.FailureKind.ShouldEqual("IdentityMappingConflict");
    [Fact] void should_name_the_mapping_conflict() => _outcome.Reason!.ShouldContain("IdentityMappingConflict:");
    [Fact] void should_preserve_identities() => File.ReadAllBytes(IdentitiesPath).ShouldEqual(OriginalState);
}
