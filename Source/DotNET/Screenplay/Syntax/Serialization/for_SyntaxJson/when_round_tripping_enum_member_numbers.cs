// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_enum_member_numbers : Specification
{
    ConceptSyntax _numbered;
    ConceptSyntax _positional;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _numbered = (ConceptSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(compiler.Parse("concept Status : Enum\n  draft = 0\n  issued = 1").Value!.Concepts.Single()));
        _positional = (ConceptSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(compiler.Parse("concept Status : Enum\n  draft\n  issued").Value!.Concepts.Single()));
    }

    [Fact] void should_preserve_the_authored_numbering() => _numbered.ValueNumbers.ShouldContainOnly(0, 1);
    [Fact] void should_preserve_absence_not_normalize_to_an_empty_array() => _positional.ValueNumbers.ShouldBeNull();
    [Fact] void should_omit_absent_numbers_from_legacy_json() => SyntaxJson.Serialize(_positional).TryGetProperty("valueNumbers", out _).ShouldBeFalse();
    [Fact] void should_distinguish_numbered_from_positional_syntax() => SyntaxJson.StructurallyEqual(_numbered, _positional).ShouldBeFalse();
}
