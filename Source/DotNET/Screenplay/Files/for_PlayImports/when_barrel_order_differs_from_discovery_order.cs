// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_barrel_order_differs_from_discovery_order : given.a_folder
{
    void Establish()
    {
        _documents.Add("application.play", "import \"Zulu/Zulu.play\"\nimport \"Alpha/Alpha.play\"\n");
        _documents.Add("Zulu/Zulu.play", "module Zulu\n  import \"Zulu/Zulu.play\"\n  import \"Alpha/Alpha.play\"\n");
        _documents.Add("Zulu/Zulu/Zulu.play", "feature Zulu\n  import \"Second.play\"\n  import \"First.play\"\n");
        _documents.Add("Zulu/Zulu/Second.play", "slice StateView Second\n");
        _documents.Add("Zulu/Zulu/First.play", "slice StateView First\n");
        _documents.Add("Zulu/Alpha/Alpha.play", "feature Alpha\n");
        _documents.Add("Alpha/Alpha.play", "module Alpha\n");
    }

    void Because() => Resolve([.. _documents.Keys.Order(StringComparer.Ordinal)]);

    [Fact] void should_resolve_without_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_follow_authored_import_order_despite_every_file_being_a_root() => _resolved.Select(document => document.Path).ShouldEqual(["application.play", "Zulu/Zulu.play", "Zulu/Zulu/Zulu.play", "Zulu/Zulu/Second.play", "Zulu/Zulu/First.play", "Zulu/Alpha/Alpha.play", "Alpha/Alpha.play"]);
}
