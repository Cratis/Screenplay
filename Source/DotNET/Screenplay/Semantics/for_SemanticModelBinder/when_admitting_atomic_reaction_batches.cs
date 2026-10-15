// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_atomic_reaction_batches : Specification
{
    [Theory]
    [InlineData("reaction batches atomic", true)]
    [InlineData("reaction batches atomic\nreaction batches atomic", false)]
    [InlineData("reaction batches atomic\nreaction batches legacy", false)]
    [InlineData("reaction batches legacy", false)]
    public void should_check_the_explicit_root_declaration(string source, bool valid)
    {
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Success.ShouldEqual(valid);
        if (valid)
        {
            var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
            restored.ReactionProductionMode.ShouldEqual(ReactionProductionMode.Atomic);
            new ScreenplayPrinter().Print(restored).ShouldContain("reaction batches atomic");
        }
        else
        {
            parsed.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReactionBatchesDeclaration);
        }
    }

    [Fact]
    void should_require_v22_only_for_an_explicit_mode()
    {
        var legacy = canonical_serialization_golden_vectors.CreateSemanticModelV6();
        var atomic = legacy.Application with { ReactionProductionMode = SemanticReactionProductionMode.Atomic };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V21, SemanticVersion.V21, atomic)).ShouldBeOfExactType<InvalidSemanticContract>();
        ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22, atomic).Application.ReactionProductionMode.ShouldEqual(SemanticReactionProductionMode.Atomic);
        ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22, legacy.Application).Application.ReactionProductionMode.ShouldBeNull();
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22, atomic with { ReactionProductionMode = (SemanticReactionProductionMode)99 })).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
