// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_advancing_persisted_event_identity_through_three_generations : Specification
{
    SemanticId _initialProperty;
    SemanticId _secondProperty;
    SemanticCompilation _final = null!;
    SemanticAddress _event = null!;

    void Because()
    {
        var app = ApplicationIdentity.Create("Projects");
        const string source1 = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        first String\n";
        const string source2 = source1 + "      event Registered generation 2\n        second String\n";
        const string source3 = source2 + "      event Registered generation 3\n        third String\n";
        var binder = new SemanticModelBinder();
        var empty = SemanticIdentityCatalog.Empty(app);
        var first = Bind(source1, empty);
        var index1 = SemanticCompilationIndex.Create(first.Model.Application, app);
        var catalog1 = SemanticIdentityCatalog.PlanMigration(
            empty,
            empty.Revision,
            ["source"],
            [.. index1.Declarations.Keys],
            [.. index1.Events.Keys],
            [],
            [],
            []).Catalog;
        _event = catalog1.EventContracts.Single().Address;
        _initialProperty = catalog1.Semantics.Single(value => value.Address.Kind == SemanticKind.Property).Id;
        var provisional2 = Bind(source2, empty);
        var index2 = SemanticCompilationIndex.Create(provisional2.Model.Application, app);
        var catalog2 = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            catalog1,
            catalog1.Revision,
            ["source"],
            [.. index2.Declarations.Keys],
            [.. index2.Events.Keys],
            [new(_event, new(2))]).Catalog;
        var second = Bind(source2, catalog2);
        _secondProperty = second.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties.Single().Id;
        var provisional3 = Bind(source3, empty);
        var index3 = SemanticCompilationIndex.Create(provisional3.Model.Application, app);
        var catalog3 = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            catalog2,
            catalog2.Revision,
            ["source"],
            [.. index3.Declarations.Keys],
            [.. index3.Events.Keys],
            [new(_event, new(3))]).Catalog;
        _final = Bind(source3, catalog3);

        SemanticCompilation Bind(string text, SemanticIdentityCatalog catalog)
        {
            var syntax = new ScreenplayCompiler().Parse(text).Value!;
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", text);
            var result = binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
            Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            return result.Value!;
        }
    }

    [Fact] void should_keep_the_first_generation_identity() => _final.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().PriorRevisions[0].Properties.Single().Id.ShouldEqual(_initialProperty);
    [Fact] void should_keep_the_second_generation_identity() => _final.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().PriorRevisions[1].Properties.Single().Id.ShouldEqual(_secondProperty);
    [Fact] void should_record_revision_three_in_the_persisted_catalog() => _final.Documents.IdentityCatalog.ResolveEventContract(_event).Revision.Value.ShouldEqual(3u);
}
