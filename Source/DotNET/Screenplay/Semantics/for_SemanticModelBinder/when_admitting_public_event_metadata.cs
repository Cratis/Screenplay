// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_public_event_metadata : given.a_semantic_binder
{
    [Theory]
    [InlineData("module Shipping\n  feature Orders\n    slice StateChange Ship\n      public event Shipped\n")]
    [InlineData("module Shipping\n  feature Orders\n    slice StateChange Ship\n      event Shipped from \"store\"\n")]
    [InlineData("module Shipping\n  feature Orders\n    slice Translate Transfer\n      direction inbound\n")]
    [InlineData("module Shipping\n  feature Orders\n    slice StateChange Ship\n      public event Shipped generation 1\n      public event Shipped generation 2\n")]
    void should_admit_public_metadata_at_the_claimed_version(string source)
    {
        new ScreenplayCompiler().Compile(source).Success.ShouldBeTrue();
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(value => value.Message)));
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    }

    [Fact]
    void should_refuse_an_import_because_it_has_no_local_shape()
    {
        const string Source = "import Fulfillment.Shipped from \"store\"\n";
        new ScreenplayCompiler().Compile(Source).Success.ShouldBeTrue();
        var result = Bind(Source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var diagnostic = result.Diagnostics.First(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldContain("no local shape");
    }

    [Fact]
    void should_refuse_an_outbound_translation_that_declares_no_public_event_in_the_model()
    {
        var result = Bind("module Shipping\n  feature Orders\n    slice Translate Transfer\n      direction outbound\n      event Changed\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.OutboundPublicEventCount).ShouldBeTrue();
    }

    [Fact]
    void should_keep_legacy_private_event_semantics()
    {
        var result = Bind("module Shipping\n  feature Orders\n    slice StateChange Ship\n      event Shipped\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }
}
