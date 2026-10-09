// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_public_events : given.a_compiler
{
    const string Prefix = "module Shipping\n  feature Orders\n    slice StateChange Ship\n";

    [Theory]
    [InlineData("event Shipped", EventVisibility.Private, null)]
    [InlineData("public event Shipped", EventVisibility.Public, null)]
    [InlineData("public\tevent Shipped", EventVisibility.Public, null)]
    [InlineData("event Shipped from \"fulfillment/store\"", EventVisibility.Public, "fulfillment/store")]
    [InlineData("public event Shipped generation 1 from \"fulfillment/store\"", EventVisibility.Public, "fulfillment/store")]
    void should_accept_event_contract_headers(string header, EventVisibility visibility, string? origin)
    {
        var result = _compiler.Compile(Prefix + $"      {header}\n        name String\n");
        result.Success.ShouldBeTrue();
        var @event = result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single();
        @event.Name.ShouldEqual("Shipped");
        @event.Visibility.ShouldEqual(visibility);
        @event.Origin.ShouldEqual(origin);
        @event.Generation.ShouldEqual(1u);
    }

    [Theory]
    [InlineData("public event")]
    [InlineData("public Shipped")]
    [InlineData("event Shipped from fulfillment")]
    [InlineData("event Shipped from \"unterminated")]
    [InlineData("event Shipped from \"\"")]
    [InlineData("event Shipped from \"one\" from \"two\"")]
    [InlineData("public event Shipped generation 0")]
    void should_reject_invalid_event_headers(string header) => _compiler.Compile(Prefix + $"      {header}\n").Success.ShouldBeFalse();

    [Theory]
    [InlineData("import Fulfillment.Shipped", EventVisibility.Private, null)]
    [InlineData("import Fulfillment.Shipped from \"../not-a-file/*.play\"", EventVisibility.Public, "../not-a-file/*.play")]
    void should_keep_contract_imports_distinct_from_file_imports(string header, EventVisibility visibility, string? origin)
    {
        var result = _compiler.Compile(header + "\n");
        result.Success.ShouldBeTrue();
        result.Value!.FileImports.ShouldBeEmpty();
        var import = result.Value.Imports.Single();
        import.QualifiedName.ShouldEqual("Fulfillment.Shipped");
        import.Visibility.ShouldEqual(visibility);
        import.Origin.ShouldEqual(origin);
    }

    [Theory]
    [InlineData("import Fulfillment.Shipped from fulfillment")]
    [InlineData("import Fulfillment.Shipped from \"\"")]
    [InlineData("import Fulfillment.Shipped from \"one\" trailing")]
    [InlineData("import \"files.play\" from \"store\"")]
    void should_reject_invalid_import_origins(string header) => _compiler.Compile(header + "\n").Success.ShouldBeFalse();

    [Theory]
    [InlineData("Translate", "", null)]
    [InlineData("Translate", "      direction inbound\n", TranslationDirection.Inbound)]
    [InlineData("Translate", "      direction outbound\n      event Changed\n      public event Published\n      reaction Publish\n        when Changed\n          produces Published\n", TranslationDirection.Outbound)]
    [InlineData("Translate", "      direction\t  inbound\n", TranslationDirection.Inbound)]
    [InlineData("Automation", "", null)]
    void should_preserve_explicit_direction_and_interpret_legacy_translate_as_inbound(string kind, string body, TranslationDirection? direction)
    {
        var result = _compiler.Compile($"module Shipping\n  feature Orders\n    slice {kind} Transfer\n" + body);
        result.Success.ShouldBeTrue();
        var slice = result.Value!.Modules.Single().Features.Single().Slices.Single();
        slice.Direction.ShouldEqual(direction);
        slice.EffectiveDirection.ShouldEqual(kind == "Translate" ? direction ?? TranslationDirection.Inbound : null);
    }

    [Theory]
    [InlineData("Translate", "direction")]
    [InlineData("Translate", "direction sideways")]
    [InlineData("Translate", "direction Inbound")]
    [InlineData("Translate", "direction inbound extra")]
    [InlineData("Translate", "direction inbound\n      direction outbound")]
    [InlineData("StateChange", "direction inbound")]
    [InlineData("StateView", "direction outbound")]
    [InlineData("Automation", "direction inbound")]
    void should_reject_malformed_duplicate_or_misplaced_directions(string kind, string body)
    {
        var result = _compiler.Compile($"module Shipping\n  feature Orders\n    slice {kind} Transfer\n      {body}\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidSliceDeclaration).ShouldBeTrue();
    }
}
