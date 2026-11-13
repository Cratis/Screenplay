// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Agents;

namespace Cratis.Screenplay.for_Agents;

public class when_refusing_capability_manifests : given.a_manifest_model
{
    [Theory]
    [InlineData("agents\n  tokens oauth")]
    [InlineData("agents\n  tokens personal\n  query Missing\n    scope read\n    description \"Missing\"")]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read")]
    [InlineData("agents\n  tokens personal\n  query All\n    description \"Missing scope\"")]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description \"\"")]
    [InlineData("agents\n  tokens personal\n  query All\n    scope unknown\n    description \"Invalid scope\"")]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description \"Premature approval\"\n    approve always")]
    [InlineData("agents\n  tokens personal\n  query All\n    scope read\n    description \"First\"\n  query All\n    scope read\n    description \"Duplicate\"")]
    void should_never_emit_a_partial_allowlist(string block) => Refuse(BindableModel + "\n" + block);

    [Fact] void should_refuse_an_unrelated_error() => Refuse(BindableModel + "\n" + Allowlist + "\nunknown root");

    [Fact]
    void should_refuse_an_ambiguous_reference() => Refuse(BindableModel + "\nmodule Other\n  feature Other\n    slice StateView Other\n      readmodel Other\n        value String\n      query All => Other[]\n" + Allowlist.Replace("Sales.Invoices.Listing.All", "All", StringComparison.Ordinal));

    [Fact]
    void should_distinguish_no_block_from_an_explicit_empty_block()
    {
        var absent = AgentCapabilityManifest.From(Bind(BindableModel));
        absent.Manifest.ShouldBeNull();
        absent.Refusal.ShouldEqual(AgentCapabilityManifestRefusal.NoAgentsBlock);
        var empty = AgentCapabilityManifest.From(Bind(BindableModel + "\nagents\n  tokens personal"));
        empty.Refusal.ShouldEqual(AgentCapabilityManifestRefusal.None);
        empty.Manifest!.Operations.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("module Sales\n  query All => Invoice[]", "PLAY0022")]
    [InlineData("module Sales\n  command Create", "PLAY0022")]
    [InlineData("module Sales\n  feature Invoices\n    query All => Invoice[]", "PLAY0024")]
    [InlineData("module Sales\n  feature Invoices\n    command Create", "PLAY0024")]
    [InlineData("module Sales.Other\n  feature Invoices\n    slice StateView Listing", "PLAY0021")]
    void should_require_features_and_slices_and_dot_free_segments(string source, string expectedDiagnostic)
    {
        var compilation = Bind(source);
        compilation.Diagnostics.Any(diagnostic => diagnostic.Code == expectedDiagnostic).ShouldBeTrue();
        AgentCapabilityManifest.From(compilation).Refusal.ShouldEqual(AgentCapabilityManifestRefusal.CompilationErrors);
    }

    static void Refuse(string source)
    {
        var emission = AgentCapabilityManifest.From(Bind(source));
        emission.Manifest.ShouldBeNull();
        emission.Refusal.ShouldEqual(AgentCapabilityManifestRefusal.CompilationErrors);
    }
}
