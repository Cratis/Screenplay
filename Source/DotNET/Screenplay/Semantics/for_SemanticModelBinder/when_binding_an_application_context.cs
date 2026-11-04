// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_application_context : given.a_semantic_binder
{
    [Theory]
    [InlineData("context\n  businessTimeZone time zone\n  customerTimeZone time zone\n")]
    [InlineData("context\n")]
    void should_refuse_the_declaration_without_emitting_an_executable_model(string source)
    {
        var parsed = new ScreenplayCompiler().Compile(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Location.Line.ShouldEqual(1);
        diagnostic.Message.ShouldContain("0132");
        diagnostic.Message.ShouldContain("(#824)");
    }
}
