// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_an_event_has_a_later_generation : given.a_workspace_with_assigned_events
{
    ScreenplayWorkspace _rehomed = null!;

    void Establish()
    {
        const string evolved = Events + "      event ProjectRegistered generation 2\n        name ProjectName\n        changed Bool\n";
        var bootstrap = ScreenplayWorkspace.Create(Original.IdentityCatalog.Application, "Projects", [Document("memory", "memory.play", Concepts + evolved)], SemanticIdentityCatalog.Empty(Original.IdentityCatalog.Application));
        Assert.True(bootstrap.Compilation.Success, string.Join("; ", bootstrap.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var catalog = SemanticIdentityCatalog.Create(
            bootstrap.IdentityCatalog.Application,
            bootstrap.IdentityCatalog.Documents,
            bootstrap.IdentityCatalog.Semantics,
            [bootstrap.IdentityCatalog.EventContracts.Single() with
            {
                Id = Original.IdentityCatalog.EventContracts.Single().Id,
                Origin = SemanticIdentityOrigin.Persisted
            }]);
        Original = ScreenplayWorkspace.Create(catalog.Application, "Projects", bootstrap.Documents, catalog);
        Split = [Document("application.play", Concepts), Document("Projects/Registration/Registration.play", evolved)];
    }

    void Because() => _rehomed = Original.Rehome(Split);

    [Fact] void should_keep_the_contract_id() => _rehomed.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(Original.IdentityCatalog.EventContracts.Single().Id);
    [Fact] void should_keep_the_later_revision() => _rehomed.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
    [Fact] void should_preserve_generation_scoped_property_ids() => _rehomed.IdentityCatalog.Semantics.ShouldContainOnly(Original.IdentityCatalog.Semantics);
    [Fact] void should_compile() => _rehomed.Compilation.Success.ShouldBeTrue();
}
