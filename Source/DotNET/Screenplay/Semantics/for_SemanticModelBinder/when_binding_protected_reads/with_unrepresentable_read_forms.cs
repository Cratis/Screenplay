// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_unrepresentable_read_forms : given.a_protected_read_model
{
    [Theory]
    [InlineData("reads View optional", DiagnosticCodes.OptionalReadsNotSupported)]
    [InlineData("reads View advisory", DiagnosticCodes.InvalidReadsDeclaration)]
    [InlineData("reads View via query Get", DiagnosticCodes.InvalidReadsDeclaration)]
    public void should_keep_existing_parser_refusals(string declaration, string code) =>
        new ScreenplayCompiler().Parse(Source.Replace("reads View by id", declaration, StringComparison.Ordinal)).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);

    [Fact]
    void should_not_publish_unreachable_refusal_reasons()
    {
        var reasons = Enum.GetNames<SemanticDecisionReadRefusal>();
        reasons.Length.ShouldEqual(14);
        reasons.ShouldNotContain("Derivative");
        reasons.ShouldNotContain("ViaQuery");
        reasons.ShouldNotContain("UnlimitedEvery");
    }
}
