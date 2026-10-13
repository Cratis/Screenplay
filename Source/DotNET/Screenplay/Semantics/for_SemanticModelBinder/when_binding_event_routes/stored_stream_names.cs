// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_event_routes;

public class stored_stream_names : given.a_semantic_binder
{
    [Theory]
    [InlineData("  stream All\n", "All")]
    [InlineData("  stream Archive\n    id \"All\"\n", "Archive")]
    void should_refuse_the_reserved_stored_name_only_at_binding(string declaration, string name)
    {
        var source = "eventsource Account\n" + declaration;
        var authored = new ScreenplayCompiler().Compile(source);
        authored.Success.ShouldBeTrue();
        authored.Diagnostics.ShouldBeEmpty();
        new ScreenplayCompiler().Compile(new ScreenplayPrinter().Print(authored.Value!)).Success.ShouldBeTrue();
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
        diagnostic.Message.ShouldEqual($"Stream 'Account.{name}' has the reserved stored name 'All'; choose a stored name other than 'All'.");
        diagnostic.Location.Line.ShouldEqual(2);
    }

    [Theory]
    [InlineData("all", "all")]
    [InlineData("ALL", "ALL")]
    [InlineData("Default", "Default")]
    [InlineData("All\n    id \"Archive\"", "Archive")]
    [InlineData("Archive\n    id \"all\"", "all")]
    void should_bind_other_stored_names_ordinally(string declaration, string storedName)
    {
        var result = Bind("eventsource Account\n  stream " + declaration + "\n");
        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Model.Application.EventSources.Single().Streams.Single().StreamKind.ShouldEqual(storedName);
    }
}
