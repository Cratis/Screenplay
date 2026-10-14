// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening;

public class with_an_admitted_baseline : given.a_persisted_model_root
{
    SortedDictionary<string, string> _before = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        AdmitBaseline();
        _before = Snapshot();
    }

    void Because() => _outcome = ScreenplayModelRoot.Open(RootPath);

    [Fact] void should_succeed() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_read_a_valid_baseline() => _outcome.ReleaseBaseline!.Status.ShouldEqual("valid");
    [Fact] void should_read_the_admitted_revision() => _outcome.ReleaseBaseline!.Document!.Entries.Single().Event.ShouldEqual(Original.IdentityCatalog.EventContracts.Single().Id.ToString());
    [Fact] void should_carry_the_baseline_file_revision() => _outcome.ReleaseBaseline!.Revision.ShouldEqual(ReleaseBaselineStore.Revision(File.ReadAllBytes(BaselinePath)));
    [Fact] void should_carry_the_persisted_catalog() => CatalogOf(_outcome).ShouldEqual(PersistedCatalog);
    [Fact] void should_write_nothing() => Snapshot().ShouldEqual(_before);
}
