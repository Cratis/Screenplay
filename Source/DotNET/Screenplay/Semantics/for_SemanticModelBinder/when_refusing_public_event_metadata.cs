// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_public_event_metadata : given.a_semantic_binder
{
    [Theory]
    [InlineData("module Shipping\n  feature Orders\n    slice StateChange Ship\n      public event Shipped\n", "#481")]
    [InlineData("module Shipping\n  feature Orders\n    slice StateChange Ship\n      event Shipped from \"store\"\n", "#481")]
    [InlineData("import Fulfillment.Shipped from \"store\"\n", "#481")]
    [InlineData("module Shipping\n  feature Orders\n    slice Translate Transfer\n      direction inbound\n", "#480")]
    [InlineData("module Shipping\n  feature Orders\n    slice Translate Transfer\n      direction outbound\n      event Changed\n      public event Published\n      reaction Publish\n        when Changed\n          produces Published\n", "#480")]
    [InlineData("module Shipping\n  feature Orders\n    slice StateChange Ship\n      public event Shipped generation 1\n      public event Shipped generation 2\n", "#481")]
    void should_accept_source_but_never_return_a_runnable_old_model(string source, string issue)
    {
        new ScreenplayCompiler().Compile(source).Success.ShouldBeTrue();
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var diagnostic = result.Diagnostics.First(value => value.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldContain(issue);
        diagnostic.Message.ShouldContain("source only");
    }

    [Fact]
    void should_keep_legacy_private_event_semantics()
    {
        var result = Bind("module Shipping\n  feature Orders\n    slice StateChange Ship\n      event Shipped\n");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    }
}
