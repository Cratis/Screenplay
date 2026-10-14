// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening_for_release_baseline_check;

public class with_unchanged_source : given.a_persisted_model_root
{
    SortedDictionary<string, string> _before = null!;
    ScreenplayModelRootOutcome _authoring = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        AdmitBaseline();
        _authoring = ScreenplayModelRoot.Open(RootPath);
        _before = Snapshot();
    }

    void Because() => _outcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(RootPath);

    [Fact] void should_succeed() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_compile_the_source() => _outcome.Workspace!.Compilation.Success.ShouldBeTrue();
    [Fact] void should_compile_the_same_model_revision_as_the_authoring_open() => _outcome.Workspace!.Compilation.Value!.Model.Revision.ShouldEqual(_authoring.Workspace!.Compilation.Value!.Model.Revision);
    [Fact] void should_carry_the_persisted_catalog() => CatalogOf(_outcome).ShouldEqual(PersistedCatalog);
    [Fact] void should_read_the_same_baseline_as_the_authoring_open() => _outcome.ReleaseBaseline!.Revision.ShouldEqual(_authoring.ReleaseBaseline!.Revision);
    [Fact] void should_write_nothing() => Snapshot().ShouldEqual(_before);
}
