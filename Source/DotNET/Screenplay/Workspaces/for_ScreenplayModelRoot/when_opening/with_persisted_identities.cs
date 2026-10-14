// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class with_persisted_identities : given.a_persisted_model_root
{
    SortedDictionary<string, string> _before = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish() => _before = Snapshot();

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_succeed() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_not_report_a_failure() => _outcome.Reason.ShouldBeNull();
    [Fact] void should_carry_the_persisted_catalog() => CatalogOf(_outcome).ShouldEqual(PersistedCatalog);
    [Fact] void should_compile_the_source() => _outcome.Workspace!.Compilation.Success.ShouldBeTrue();
    [Fact] void should_return_the_persisted_identities() => PersistedIdentityStateSerializer.Serialize(_outcome.Identities!).ShouldEqual(OriginalState);
    [Fact] void should_read_an_absent_baseline() => _outcome.ReleaseBaseline!.Status.ShouldEqual("absent");
    [Fact] void should_write_nothing() => Snapshot().ShouldEqual(_before);
}
