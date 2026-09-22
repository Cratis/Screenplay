// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_ScreenplayCompiler.given;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_a_real_document : Specification
{
    ApplicationSyntax _original;
    ApplicationSyntax _roundTrip;
    string _json;

    void Establish() => _original = new ScreenplayCompiler().Compile(Samples.Invoicing).Value;

    void Because()
    {
        var value = SyntaxJson.Serialize(_original);
        _json = value.GetRawText();
        _roundTrip = (ApplicationSyntax)SyntaxJson.Deserialize(value);
    }

    [Fact] void should_preserve_the_entire_sample_tree() => SyntaxJson.StructurallyEqual(_original, _roundTrip).ShouldBeTrue();
    [Fact] void should_include_typed_nested_nodes() => _json.ShouldContain("\"kind\":\"EventSyntax\"");
    [Fact] void should_exclude_source_locations() => _json.Contains("\"location\":", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_have_stable_canonical_output() => SyntaxJson.Serialize(_roundTrip).GetRawText().ShouldEqual(_json);
}
