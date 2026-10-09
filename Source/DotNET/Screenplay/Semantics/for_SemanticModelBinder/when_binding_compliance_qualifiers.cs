// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_compliance_qualifiers : given.a_semantic_binder
{
    [Theory]
    [InlineData("pii", "pii special health")]
    [InlineData("personal", "personal criminal")]
    [InlineData("secret", "secret scope namespace")]
    [InlineData("pii secret", "pii special genetic\n  pii criminal")]
    void should_refuse_compliance_as_nonportable_behavior(string markers, string settings)
    {
        var result = Bind($"concept Protected : String {markers}\n  {settings}");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).Message.ShouldContain("Concept 'Protected' compliance attributes");
    }
}
