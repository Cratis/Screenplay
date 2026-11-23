// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_a_navigation_screen_target : Specification
{
    const string Legacy = "{\"kind\":\"NavigationTargetSyntax\",\"feature\":\"Projects\",\"module\":\"Shop\"}";
    const string Selected = "{\"kind\":\"NavigationTargetSyntax\",\"feature\":\"Projects\",\"module\":\"Shop\",\"screen\":\"Projects\"}";

    [Fact] void should_keep_legacy_bytes() => SyntaxJson.Serialize(SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(Legacy))).GetRawText().ShouldEqual(Legacy);
    [Fact] void should_keep_the_selected_screen() => SyntaxJson.Serialize(SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(Selected))).GetRawText().ShouldEqual(Selected);
    [Fact] void should_not_require_the_new_schema_member() => SyntaxSchema.For(nameof(NavigationTargetSyntax)).GetProperty("required").EnumerateArray().Any(value => value.GetString() == "screen").ShouldBeFalse();
    [Fact] void should_keep_the_constructor() => typeof(NavigationTargetSyntax).GetConstructor([typeof(string), typeof(string), typeof(SourceLocation)]).ShouldNotBeNull();
    [Fact] void should_keep_the_deconstruction() => typeof(NavigationTargetSyntax).GetMethods().Any(method => method.Name == "Deconstruct" && method.GetParameters().Length == 3).ShouldBeTrue();
}
