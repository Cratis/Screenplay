// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_closing_persona_callers : given.persona_documents
{
    const string Healthy = """
        policy Member
          require authenticated and role "Member"
        policy Extra
          require claim "dept" matches "Finance"
        policy Other
          require role "Other"
        persona Healthy
          policy Extra
          policy Member
        module M
          authorize Member
          feature F
            authorize Extra
            feature Nested
              slice StateChange Write
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
                command Inherited
                  value String
                  produces Created
                    value = value
                screen Write
                  action Create
                specification InheritedAllowed
                  given caller as Healthy
                  when Inherited value = "ok"
                  then Created value = "ok"
                specification ExplicitAllowed
                  given caller
                    authenticated
                    role "Member"
                    claim "dept" = "Finance"
                  when Create value = "ok"
                  then Created value = "ok"
                specification ExplicitDenied
                  given caller
                    authenticated
                    role "Member"
                  when Create value = "ok"
                  then denied
                specification Allowed
                  given caller as Healthy
                  when Create value = "ok"
                  then Created value = "ok"
                specification Denied
                  given caller as Healthy
                  when Restricted value = "ok"
                  then denied
                BAD_CALLER
              slice StateView Lookup
                readmodel Result
                  value String
                query Find => Result optional
                  authorize Member
                  by value String
                query InheritedFind => Result optional
                  by value String
                query RestrictedFind => Result optional
                  authorize Other
                  by value String
                screen Main
                  toolbar actions
                    button inherited title "Inherited" slice Write
                    button allowed title "Allowed" slice Write
                      authorize Member
                    button restricted title "Restricted" slice Write
                      authorize Other
                specification QueryInheritedAllowed
                  given caller as Healthy
                  when query InheritedFind
                    value = "ok"
                  then no result
                specification QueryDenied
                  given caller as Healthy
                  when query RestrictedFind
                    value = "ok"
                  then denied
                specification QueryExplicitAllowed
                  given caller
                    authenticated
                    role "Member"
                    claim "dept" = "Finance"
                  when query Find
                    value = "ok"
                  then no result
                specification QueryAllowed
                  given caller as Healthy
                  when query Find
                    value = "ok"
                  then no result
        """;
    const string Caller = "specification Removed\n          given caller as Clerk\n          when Create value = \"ok\"\n          then Created value = \"ok\"";
    static readonly string[] PersonaSpecifications = ["InheritedAllowed", "Allowed", "Denied", "QueryInheritedAllowed", "QueryDenied", "QueryAllowed"];
    SemanticCompilation _baseline;
    ApplicationSyntax _baselineSyntax;

    void Establish()
    {
        Source(Healthy.Replace("BAD_CALLER", string.Empty, StringComparison.Ordinal));
        _normal.Success.ShouldBeTrue();
        _baseline = _normal.Value!;
        _baselineSyntax = new ScreenplayCompiler().Compile(Healthy.Replace("BAD_CALLER", string.Empty, StringComparison.Ordinal)).Value!;
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("          parameter value String\n          case One value = \"ok\"\n", false)]
    [InlineData("", true)]
    void should_close_authored_callers_without_changing_healthy_authorization(string outline, bool file)
    {
        var caller = file ? Caller + "\n          file Removed.cs" : Caller.Replace("          when", outline + "          when", StringComparison.Ordinal);
        Source("policy Access\n  require role \"Access\"\npersona Clerk\n  policy Member\n  policy Access\n" + Healthy.Replace("BAD_CALLER", caller, StringComparison.Ordinal));
        _normal.Success.ShouldBeTrue();
        Record((syntax, round) => round == 1 ? PolicyFailure(syntax) : null);
        _result = _compiler.CompileAdmissible("Example", _documents);
        _result.Compilation.Success.ShouldBeTrue();
        _rounds.Count.ShouldEqual(2);
        _rounds[^1].Personas!.Select(persona => persona.Name).ShouldEqual(["Healthy"]);
        _rounds[^1].Modules.Single().Features.Single().Features.Single().Slices.First().Specifications.Select(specification => specification.Name).ShouldNotContain("Removed");
        var exclusion = _result.Exclusions.Single(value => value.Kind == SemanticExclusionKind.Specification);
        exclusion.Address.ShouldEqual("M/F/Nested/Write/Removed");
        exclusion.Cause.ShouldEqual(SemanticExclusionCause.Dependent);
        exclusion.DependsOn.Length.ShouldEqual(1);
        exclusion.DependsOn[0].Kind.ShouldEqual(SemanticExclusionKind.Policy);
        exclusion.DependsOn[0].Address.ShouldEqual("Access");
        exclusion.Diagnostics.SequenceEqual(_result.Exclusions.Single(value => value.Kind == SemanticExclusionKind.Policy).Diagnostics).ShouldBeTrue();
        AssertHealthy();
    }

    [Fact]
    void should_keep_direct_binder_errors_as_roots()
    {
        Source("policy Access\n  require role \"Access\"\npersona Clerk\n  policy Member\n  policy Access\n" + Healthy.Replace("BAD_CALLER", Caller, StringComparison.Ordinal));
        _normal.Success.ShouldBeTrue();
        Record((syntax, round) => round == 1 ? CallerFailure(syntax) : null);
        Admit();
        _result.Compilation.Success.ShouldBeTrue();
        var exclusion = _result.Exclusions.Single(value => value.Kind == SemanticExclusionKind.Specification);
        exclusion.Cause.ShouldEqual(SemanticExclusionCause.Root);
        exclusion.DependsOn.ShouldBeEmpty();
        exclusion.Diagnostics.Length.ShouldEqual(1);
        exclusion.Diagnostics[0].Code.ShouldEqual(DiagnosticCodes.UnsynthesizablePersonaCaller);
        _result.Exclusions.Select(value => value.Kind).ShouldEqual([SemanticExclusionKind.Specification]);
        _rounds.Count.ShouldEqual(2);
        _rounds[^1].Personas!.Select(persona => persona.Name).Order(StringComparer.Ordinal).ShouldEqual(["Clerk", "Healthy"]);
        _rounds[^1].Policies.Select(policy => policy.Name).ShouldContain("Access");
        _rounds[^1].Modules.Single().Features.Single().Features.Single().Slices.First().Specifications.Select(specification => specification.Name).ShouldNotContain("Removed");
        AssertHealthy(sameModelAsBaseline: false);
    }

    // Mirrors the binder's own persona-caller error (SemanticModelBinder.Policies.cs), located at the caller reference.
    // Real policy sources cannot reach it: a synthesizable persona always satisfies its own policies.
    static CompilationResult<SemanticCompilation> CallerFailure(ApplicationSyntax syntax)
    {
        var reference = syntax.Modules.Single().Features.Single().Features.Single().Slices.First().Specifications.Single(specification => specification.Name == "Removed").GivenCallerPersona!;
        return CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(DiagnosticCodes.UnsynthesizablePersonaCaller, "Persona 'Clerk' caller does not satisfy policy 'Access'; use an explicit 'given caller'.", reference.Location)]);
    }

    static CompilationResult<SemanticCompilation> PolicyFailure(ApplicationSyntax syntax) => CompilationResult<SemanticCompilation>.Failed(
        [PolicyError("Policy 'Access' requires exclusion.", syntax.Policies.Single(policy => policy.Name == "Access").Location, "Access")]);

    static string[] Outcomes(SemanticCompilation compilation, IEnumerable<SemanticSpecification> specifications) =>
    [
        .. specifications.OrderBy(specification => specification.Name, StringComparer.Ordinal).Select(specification =>
        {
            var run = new SemanticSpecificationRunner().Run(compilation, specification.Id);
            var reason = run.Execution is SemanticUnsupported unsupported ? unsupported.Details : string.Join("; ", run.Failures);
            return $"{specification.Name}|{run.Passed}|{run.Execution.Kind}|{reason}";
        }),
    ];

    static SemanticSpecification[] Specifications(SemanticCompilation compilation) =>
        [.. compilation.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(SliceSpecifications)];

    static IEnumerable<SemanticSpecification> SliceSpecifications(SemanticFeature feature) =>
        feature.Slices.SelectMany(slice => slice.Specifications).Concat(feature.Features.SelectMany(SliceSpecifications));

    void Admit() => _result = _compiler.CompileAdmissible("Example", _documents);

    void AssertHealthy(bool sameModelAsBaseline = true)
    {
        var bound = _result.Compilation.Value!;
        if (sameModelAsBaseline)
        {
            SemanticModelSerializer.Serialize(bound.Model).ShouldEqual(SemanticModelSerializer.Serialize(_baseline.Model));
            bound.Model.Revision.ShouldEqual(_baseline.Model.Revision);
        }

        bound.SpecificationOrigins.Keys.Select(id => id.ToString()).Order(StringComparer.Ordinal).ShouldEqual(_baseline.SpecificationOrigins.Keys.Select(id => id.ToString()).Order(StringComparer.Ordinal));
        var boundSpecifications = Specifications(bound);
        var baselineSpecifications = Specifications(_baseline);
        boundSpecifications.ShouldNotBeEmpty();
        baselineSpecifications.ShouldNotBeEmpty();
        boundSpecifications.Select(specification => specification.Name).ShouldNotContain("Removed");
        if (sameModelAsBaseline)
        {
            boundSpecifications.Select(specification => specification.Id.ToString()).Order(StringComparer.Ordinal).ShouldEqual(baselineSpecifications.Select(specification => specification.Id.ToString()).Order(StringComparer.Ordinal));
        }

        var baselineOutcomes = Outcomes(_baseline, baselineSpecifications);
        baselineOutcomes.ShouldNotBeEmpty();
        baselineOutcomes.Any(outcome => outcome.Contains("|True|", StringComparison.Ordinal)).ShouldBeTrue();
        Outcomes(bound, boundSpecifications).ShouldEqual(baselineOutcomes);
        var personaSpecifications = boundSpecifications.Where(specification => PersonaSpecifications.Contains(specification.Name)).ToArray();
        personaSpecifications.Length.ShouldEqual(PersonaSpecifications.Length);
        personaSpecifications.ShouldEachConformTo(specification => specification.GivenCaller is { Authenticated: true } caller
            && caller.Roles.Contains("Member")
            && caller.Claims.Any(claim => claim.Type == "dept" && claim.Value == "Finance"));
        AssertExplicitCallers(boundSpecifications, baselineSpecifications);
        AssertExecutableGates(bound);
        AssertScreenGates();
    }

    void AssertExplicitCallers(SemanticSpecification[] bound, SemanticSpecification[] baseline)
    {
        _baselineSyntax.Modules.SelectMany(module => module.Features).SelectMany(SyntaxSpecifications).Count(specification => specification.GivenCaller is not null).ShouldEqual(3);
        var retained = bound.ToDictionary(specification => specification.Id);
        foreach (var specification in baseline)
        {
            CallerKey(retained[specification.Id].GivenCaller).ShouldEqual(CallerKey(specification.GivenCaller));
        }
    }

    static string CallerKey(SemanticCaller? caller) => caller is null
        ? "none"
        : $"{caller.Authenticated}|{string.Join(',', caller.Roles)}|{string.Join(',', caller.Claims.Select(claim => $"{claim.Type}={claim.Value}"))}";

    static IEnumerable<SpecificationSyntax> SyntaxSpecifications(FeatureSyntax feature) =>
        feature.Slices.SelectMany(slice => slice.Specifications).Concat(feature.Features.SelectMany(SyntaxSpecifications));

    void AssertExecutableGates(SemanticCompilation bound)
    {
        var before = SemanticExecutionPlan.Compile(_baseline.Model).Plan!;
        var after = SemanticExecutionPlan.Compile(bound.Model).Plan!;
        var allowed = new SemanticCaller(true, ["Member"], [new("dept", "Finance")]);
        var missingInheritedClaim = new SemanticCaller(true, ["Member"], []);
        foreach (var command in before.Commands.Values)
        {
            var retained = after.Commands[command.Id];
            retained.Authorization.ShouldEqual(command.Authorization);
            AssertDecision(command.Authorization, retained.Authorization, allowed, command.Name == "Restricted" ? SemanticPolicyOutcome.Deny : SemanticPolicyOutcome.Allow);
            AssertDecision(command.Authorization, retained.Authorization, missingInheritedClaim, SemanticPolicyOutcome.Deny);
        }
        foreach (var query in before.Queries.Values)
        {
            var retained = after.Queries[query.Id];
            retained.Authorization.ShouldEqual(query.Authorization);
            AssertDecision(query.Authorization, retained.Authorization, allowed, query.Name == "RestrictedFind" ? SemanticPolicyOutcome.Deny : SemanticPolicyOutcome.Allow);
            AssertDecision(query.Authorization, retained.Authorization, missingInheritedClaim, SemanticPolicyOutcome.Deny);
        }

        void AssertDecision(SemanticAuthorization? original, SemanticAuthorization? retained, SemanticCaller caller, SemanticPolicyOutcome expected)
        {
            var first = SemanticPolicyEvaluation.Evaluate(original, before, caller, new Dictionary<string, SemanticValue>(), null, []).Outcome;
            var second = SemanticPolicyEvaluation.Evaluate(retained, after, caller, new Dictionary<string, SemanticValue>(), null, []).Outcome;
            first.ShouldEqual(expected);
            second.ShouldEqual(first);
        }
    }

    void AssertScreenGates()
    {
        var original = _baselineSyntax.Modules.Single();
        var retained = _rounds[^1].Modules.Single();
        var before = original.Features.Single().Features.Single().Slices.Last().Screens.Single();
        var after = retained.Features.Single().Features.Single().Slices.Last().Screens.Single();
        after.ShouldEqual(_rounds[0].Modules.Single().Features.Single().Features.Single().Slices.Last().Screens.Single());
        var buttons = before.Directives.OfType<ScreenToolbarSyntax>().Single().Buttons.ToArray();
        var retainedButtons = after.Directives.OfType<ScreenToolbarSyntax>().Single().Buttons.ToArray();
        buttons.Length.ShouldEqual(3);
        foreach (var button in buttons)
        {
            var expected = EffectiveAuthorizationScopes.For(original, ["F", "Nested"], button.Authorize).OfType<AuthorizeSyntax>().Select(scope => ((PolicyReferenceSyntax)scope.Requirement).Name).ToArray();
            var actual = EffectiveAuthorizationScopes.For(retained, ["F", "Nested"], retainedButtons.Single(value => value.Id == button.Id).Authorize).OfType<AuthorizeSyntax>().Select(scope => ((PolicyReferenceSyntax)scope.Requirement).Name);
            string[] scopes = button.Id == "inherited" ? ["Member", "Extra"] : ["Member", "Extra", button.Id == "allowed" ? "Member" : "Other"];
            expected.ShouldEqual(scopes);
            actual.ShouldEqual(expected);
        }
    }
}
