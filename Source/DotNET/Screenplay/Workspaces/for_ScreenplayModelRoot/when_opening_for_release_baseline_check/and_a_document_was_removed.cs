// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.when_opening_for_release_baseline_check;

public class and_a_document_was_removed : given.a_persisted_model_root
{
    SortedDictionary<string, string> _before = null!;
    ScreenplayModelRootOutcome _outcome = null!;

    void Establish()
    {
        AdmitBaseline();
        File.Delete(Path.Combine(RootPath, "application.play"));
        _before = Snapshot();
    }

    void Because() => _outcome = ScreenplayModelRoot.OpenForReleaseBaselineCheck(RootPath);

    [Fact] void should_succeed() => _outcome.Success.ShouldBeTrue();
    [Fact] void should_retain_the_removed_contract() => CatalogOf(_outcome).ShouldEqual(PersistedCatalog);
    [Fact] void should_have_no_documents() => _outcome.Workspace!.Documents.ShouldBeEmpty();
    [Fact] void should_read_the_baseline() => _outcome.ReleaseBaseline!.Status.ShouldEqual("valid");
    [Fact] void should_let_a_check_report_the_missing_revision() => ReleaseBaseline.Check(_outcome.Workspace!, _outcome.ReleaseBaseline!).Violations.Single().Kind.ShouldEqual("BaselinedRevisionMissing");
    [Fact] void should_write_nothing() => Snapshot().ShouldEqual(_before);
}
