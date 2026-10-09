// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_persona_callers : given.a_semantic_binder
{
    const string Source = """
        policy Member
          require authenticated and role "A" and claim "dept" matches "Finance"
        policy Other
          require role "B"
        persona Accountant
          policy Member
        module M
          feature F
            slice StateChange S
              event Created
                value String
              command Create
                value String
                authorize Member
                produces Created
                  value = value
              command Restricted
                value String
                authorize Other
                produces Created
                  value = value
              specification Allowed
                CALLER
                when Create value = "ok"
                then Created value = "ok"
              specification Denied
                CALLER
                when Restricted value = "ok"
                then denied
        """;
    CompilationResult<SemanticCompilation> _persona;
    CompilationResult<SemanticCompilation> _explicit;

    void Because()
    {
        _persona = Bind(Source.Replace("CALLER", "given caller as Accountant", StringComparison.Ordinal));
        _explicit = Bind(Source.Replace("CALLER", "given caller\n          authenticated\n          role \"A\"\n          claim \"dept\" = \"Finance\"", StringComparison.Ordinal));
    }

    [Fact] void should_bind_the_persona_caller() => _persona.Success.ShouldBeTrue();
    [Fact] void should_bind_the_explicit_caller() => _explicit.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_canonical_bytes() => SemanticModelSerializer.Serialize(_persona.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_explicit.Value!.Model)).ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => _persona.Value!.Model.Revision.ShouldEqual(_explicit.Value!.Model.Revision);
    [Fact] void should_run_allow_and_denial() => _persona.Value!.SpecificationOrigins.Keys.All(id => new SemanticSpecificationRunner().Run(_persona.Value, id).Passed).ShouldBeTrue();
    [Fact] void should_keep_persona_provenance() => _persona.Value!.SpecificationOrigins.Values.SelectMany(specification => specification.Steps).Where(step => step.Role == "given caller").SelectMany(step => step.Values).All(value => value.Origin == SpecificationValueOrigin.Persona && value.Persona == "Accountant" && value.Policy == "Member").ShouldBeTrue();
    [Fact] void should_refuse_negation_at_binding() => Refusal("not role \"A\"").Message.ShouldContain("negation");
    [Fact] void should_refuse_needed_nonliteral_claims_at_binding() => Refusal("claim \"owner\" matches subject").Message.ShouldContain("nonLiteralClaim");
    [Fact] void should_refuse_role_claims_at_binding() => Refusal("claim \"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\" matches \"A\"").Message.ShouldContain("roleClaim");
    [Fact] void should_refuse_a_policyless_persona_at_binding() => Bind(Source.Replace("  policy Member\n", string.Empty, StringComparison.Ordinal).Replace("CALLER", "given caller as Accountant", StringComparison.Ordinal)).Diagnostics.First(diagnostic => diagnostic.Code == DiagnosticCodes.UnsynthesizablePersonaCaller).Message.ShouldContain("noPolicies");
    [Fact] void should_refuse_a_file_policy_at_binding() => Bind(Source.Replace("require authenticated and role \"A\" and claim \"dept\" matches \"Finance\"", "file Policy.cs", StringComparison.Ordinal).Replace("CALLER", "given caller as Accountant", StringComparison.Ordinal)).Diagnostics.First(diagnostic => diagnostic.Code == DiagnosticCodes.UnsynthesizablePersonaCaller).Message.ShouldContain("opaqueImplementation");
    [Fact] void should_offer_an_explicit_caller_remedy() => Refusal("not role \"A\"").Message.ShouldContain("explicit 'given caller'");

    Diagnostic Refusal(string condition) => Bind(Source.Replace("authenticated and role \"A\" and claim \"dept\" matches \"Finance\"", condition, StringComparison.Ordinal).Replace("CALLER", "given caller as Accountant", StringComparison.Ordinal)).Diagnostics.First(diagnostic => diagnostic.Code == DiagnosticCodes.UnsynthesizablePersonaCaller);
}
