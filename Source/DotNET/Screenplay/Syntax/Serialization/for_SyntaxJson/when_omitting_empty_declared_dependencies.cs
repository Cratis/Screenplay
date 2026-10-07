// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_omitting_empty_declared_dependencies : Specification
{
    ApplicationSyntax _syntax;
    string _json;
    ApplicationSyntax _restored;

    void Because()
    {
        _syntax = new ScreenplayCompiler().Parse("module M\n  feature F\n").Value!;
        _json = SyntaxJson.Serialize(_syntax).GetRawText();
        _restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_syntax));
    }

    [Fact] void should_not_add_empty_members_to_existing_transport_bytes() => _json.ShouldNotContain("dependsOn");
    [Fact] void should_default_omitted_module_dependencies_to_empty() => _restored.Modules.Single().DependsOn.ShouldBeEmpty();
    [Fact] void should_default_omitted_feature_dependencies_to_empty() => _restored.Modules.Single().Features.Single().DependsOn.ShouldBeEmpty();
    [Fact] void should_preserve_the_whole_syntax() => SyntaxJson.StructurallyEqual(_syntax, _restored).ShouldBeTrue();
}
