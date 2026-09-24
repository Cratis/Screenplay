// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_scoped_authorization : Specification
{
    const string Source =
        """
        module Portal
          authorize Access
          feature Orders
            authorize Staff or Manager
            slice StateChange Submit
              command Submit
        """;

    ApplicationSyntax _original;
    ApplicationSyntax _restored;
    string _json;

    void Establish() => _original = new ScreenplayCompiler().Compile(Source).Value!;

    void Because()
    {
        var serialized = SyntaxJson.Serialize(_original);
        _json = serialized.GetRawText();
        _restored = (ApplicationSyntax)SyntaxJson.Deserialize(serialized);
    }

    [Fact] void should_include_the_init_members() => _json.ShouldContain("\"authorize\":");
    [Fact] void should_preserve_the_tree() => SyntaxJson.StructurallyEqual(_original, _restored).ShouldBeTrue();
    [Fact] void should_restore_the_module_gate() => _restored.Modules.Single().Authorize!.References().Single().Name.ShouldEqual("Access");
    [Fact] void should_restore_the_feature_gate() => _restored.Modules.Single().Features.Single().Authorize!.References().Select(_ => _.Name).ShouldContainOnly("Staff", "Manager");
}
