// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_passive_read_models : Specification
{
    readonly ReadModelSyntax _model = new("Row", [], SourceLocation.Start);

    [Fact]
    void should_omit_false_from_legacy_json() => SyntaxJson.Serialize(_model).TryGetProperty("isPassive", out _).ShouldBeFalse();

    [Fact]
    void should_default_missing_to_false() => ((ReadModelSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_model))).IsPassive.ShouldBeFalse();

    [Fact]
    void should_preserve_true()
    {
        var json = SyntaxJson.Serialize(_model with { IsPassive = true });
        json.GetProperty("isPassive").GetBoolean().ShouldBeTrue();
        ((ReadModelSyntax)SyntaxJson.Deserialize(json)).IsPassive.ShouldBeTrue();
    }
}
