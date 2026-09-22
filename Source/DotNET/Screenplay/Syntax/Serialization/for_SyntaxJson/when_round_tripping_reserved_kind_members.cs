// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_reserved_kind_members : Specification
{
    JsonElement _sourceJson;
    JsonElement _whenJson;
    CaptureSourceSyntax _source;
    CaptureWhenSyntax _when;

    void Because()
    {
        var location = SourceLocation.Start;
        _sourceJson = SyntaxJson.Serialize(new CaptureSourceSyntax("webhook", [new("route", "/invoices", location)], location));
        _whenJson = SyntaxJson.Serialize(new CaptureWhenSyntax(CaptureWhenKind.LogicalAnd, ["name", "status"], null, null, null, location));
        _source = (CaptureSourceSyntax)SyntaxJson.Deserialize(_sourceJson);
        _when = (CaptureWhenSyntax)SyntaxJson.Deserialize(_whenJson);
    }

    [Fact] void should_reserve_kind_for_the_discriminator() => _sourceJson.GetProperty("kind").GetString().ShouldEqual("CaptureSourceSyntax");
    [Fact] void should_name_the_structural_kind_unambiguously() => _sourceJson.GetProperty("syntaxKind").GetString().ShouldEqual("webhook");
    [Fact] void should_use_enum_names() => _whenJson.GetProperty("syntaxKind").GetString().ShouldEqual("LogicalAnd");
    [Fact] void should_restore_the_source_kind() => _source.Kind.ShouldEqual("webhook");
    [Fact] void should_restore_the_trigger_kind() => _when.Kind.ShouldEqual(CaptureWhenKind.LogicalAnd);
    [Fact] void should_restore_scalar_collections() => _when.Properties.ShouldContainOnly("name", "status");
}
