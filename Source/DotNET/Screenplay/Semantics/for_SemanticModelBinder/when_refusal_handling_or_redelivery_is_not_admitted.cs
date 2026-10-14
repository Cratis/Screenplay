// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusal_handling_or_redelivery_is_not_admitted : given.a_semantic_binder
{
    const string Source = "module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n        id String\n      command Claim\n        id String identifier\n      reaction Claimer\n        when Approved\n          id\n          invokes Claim\n            id = id\n";

    [Theory]
    [InlineData("            on refused\n              acknowledge\n")]
    [InlineData("            on refused by authorization\n              acknowledge\n")]
    void should_admit_refusal_branches_as_v19(string addition)
    {
        var result = Bind(Source + addition);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V19);
    }

    [Fact]
    void should_refuse_a_refusal_expression_even_without_a_branch()
    {
        var result = Bind(Source.Replace("id = id", "id = $refusal.reason", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidRefusalValue);
    }

    [Fact]
    void should_keep_legacy_bytes_and_versions_for_empty_authoring_defaults()
    {
        var legacy = Bind(Source);
        legacy.Success.ShouldBeTrue();
        var syntax = new ScreenplayCompiler().Parse(Source).Value!;
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string StableKey = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "application.play", Source);
        var documents = SemanticDocumentSet.Create([document], catalog);
        var slice = syntax.Modules.Single().Features.Single().Slices.Single();
        var reaction = slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var invocation = trigger.Invokes!.Single();
        var changedTrigger = trigger with { Invokes = [invocation with { OnRefused = [] }] };
        var changedSlice = slice with { Reactions = [reaction with { Triggers = [changedTrigger] }] };
        var feature = syntax.Modules.Single().Features.Single() with { Slices = [changedSlice] };
        var module = syntax.Modules.Single() with { Features = [feature] };
        var explicitDefaults = _binder.Bind("Projects", syntax with { Modules = [module] }, documents);
        explicitDefaults.Success.ShouldBeTrue();
        SemanticModelCanonicalJson.Serialize(explicitDefaults.Value!.Model).ShouldEqual(SemanticModelCanonicalJson.Serialize(legacy.Value!.Model));
        explicitDefaults.Value.Model.Revision.ShouldEqual(legacy.Value.Model.Revision);
        explicitDefaults.Value.Model.SemanticVersion.ShouldEqual(legacy.Value.Model.SemanticVersion);
    }
}
