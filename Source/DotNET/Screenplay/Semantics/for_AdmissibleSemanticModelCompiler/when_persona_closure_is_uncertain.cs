// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_persona_closure_is_uncertain : given.persona_documents
{
    [Theory]
    [InlineData("persona Clerk\n", 1)]
    [InlineData("authentication\n  provider EntraId\nonboarding\n  invite tenant as Missing\n", 0)]
    [InlineData("policy Access\n  require role \"duplicate\"\n", 0)]
    void should_refuse_invalid_security_sources(string invalid, int boundRounds)
    {
        // Authorized makes the shared Access policy fail strictly, so the duplicate persona is a genuine refusal of an attributable closure.
        Source(Policy + Personas + invalid + Authorized);
        _normal.Success.ShouldBeFalse();
        Record();
        _result = _compiler.CompileAdmissible("Example", _documents);
        AssertRefusal();
        _rounds.Count.ShouldEqual(boundRounds);
    }

    [Fact]
    void should_roll_back_mixed_unattributable_errors()
    {
        Source(Policy + Personas + Authorized);
        _normal.Success.ShouldBeFalse();
        var unknown = Diagnostic.Error(DiagnosticCodes.InvalidSemanticBinding, "Unknown closure.", SourceLocation.Start.In("missing.play"));
        Record((syntax, round) => round == 1 ? CompilationResult<SemanticCompilation>.Failed(_normal.Diagnostics.Concat([unknown])) : null);
        var expected = _normal.Diagnostics;
        Admit();
        _result.Compilation.Success.ShouldBeFalse();
        _result.Compilation.Value.ShouldBeNull();
        _result.Exclusions.ShouldBeEmpty();
        _result.Compilation.Diagnostics.ShouldContain(diagnostic => diagnostic.Location.Path == "missing.play");
        _result.Compilation.Diagnostics.ShouldContain(expected.First());
        _rounds.Count.ShouldEqual(1);
    }

    [Fact]
    void should_restore_the_original_failure_when_the_second_round_is_unattributable()
    {
        Source(Policy + Personas + "authentication\n  provider EntraId\nonboarding\n  invite tenant as Clerk\n" + Authorized);
        _normal.Success.ShouldBeFalse();
        var original = _normal.Diagnostics.ToArray();
        var later = PolicyError("Unknown second-round closure.", SourceLocation.Start.In("missing.play"), "Missing");
        Record((syntax, round) => CompilationResult<SemanticCompilation>.Failed(round == 1 ? original : [later]));
        Admit();
        _rounds.Count.ShouldEqual(2);
        _rounds[0].Personas!.Select(persona => persona.Name).ShouldEqual(["Clerk", "Healthy"]);
        _rounds[1].Personas!.Select(persona => persona.Name).ShouldEqual(["Healthy"]);
        _rounds[1].Personas!.Single().Policies.ShouldEqual(["Extra", "Member"]);
        _rounds[0].Onboarding!.Invitations.ShouldNotBeEmpty();
        _rounds[1].Onboarding!.Invitations.ShouldBeEmpty();
        _rounds[1].Policies.Select(policy => policy.Name).ShouldNotContain("Access");
        _result.Compilation.Success.ShouldBeFalse();
        _result.Compilation.Value.ShouldBeNull();
        _result.Compilation.Diagnostics.ShouldEqual(original);
        _result.Compilation.Diagnostics.ShouldNotContain(later);
        _result.Exclusions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_a_surviving_unresolved_persona_reference(bool invitation)
    {
        Source(Policy + Personas + Valid);
        var source = Policy + Personas + (invitation
            ? "authentication\n  provider EntraId\nonboarding\n  invite tenant as Missing\n" + Valid
            : Valid + "      specification Unknown\n        given caller as Missing\n        when Record id = \"one\"\n        then Recorded id = \"one\"\n");
        var syntax = new ScreenplayCompiler().Parse(source, "main.play").Value!;
        var index = new AdmissibleSyntax(syntax, _documents.IdentityCatalog);
        index.Index();
        var plan = new AdmissibleExclusions(index);
        var diagnostic = PolicyError("Exclude Access.", syntax.Policies.First().Location, "Access");
        new AdmissibleSecurity(syntax, index).TryPlan([diagnostic], plan).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_duplicate_affected_persona_identity()
    {
        Source(Policy + Personas + Valid);
        var syntax = new ScreenplayCompiler().Parse(Policy + Personas + "persona Clerk\n" + Valid, "main.play").Value!;
        var index = new AdmissibleSyntax(syntax, _documents.IdentityCatalog);
        index.Index();
        var plan = new AdmissibleExclusions(index);
        var diagnostic = PolicyError("Exclude Access.", syntax.Policies.First().Location, "Access");
        new AdmissibleSecurity(syntax, index).TryPlan([diagnostic], plan).ShouldBeFalse();
        plan.Removed.ShouldBeEmpty();
    }

    void Admit() => _result = _compiler.CompileAdmissible("Example", _documents);

    void AssertRefusal()
    {
        _result.Compilation.Success.ShouldBeFalse();
        _result.Compilation.Value.ShouldBeNull();
        _result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
        _result.Exclusions.ShouldBeEmpty();
    }
}
