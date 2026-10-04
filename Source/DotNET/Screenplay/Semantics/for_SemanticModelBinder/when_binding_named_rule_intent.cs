// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_named_rule_intent
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command Submit\n        label String\n        validate\n          label rule CheckLabel severity warning message \"Invalid label\"\n";
    const string Code = "return context.Value != \"no\";";

    [Theory]
    [InlineData("file", false)]
    [InlineData("file", true)]
    [InlineData("csharp", false)]
    [InlineData("typescript", false)]
    [InlineData("react", false)]
    [InlineData("html", false)]
    [InlineData("sql", false)]
    public void should_preserve_the_existing_bound_contract(string language, bool supplied)
    {
        var payload = language == "file" ? "file Rules/CheckLabel.cs" : $"```{language}\n{Code}\n```";
        var directSource = Prefix + Indent(payload, 12);
        var wrappedSource = Prefix + "            implementation\n              hint \"Preserve criteria\"\n" + Indent(payload, 14);
        var direct = Bind(directSource, supplied);
        var wrapped = Bind(wrappedSource, supplied);
        direct.Success.ShouldBeTrue();
        wrapped.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(wrapped.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(direct.Value!.Model));
        wrapped.Value.Model.Revision.ShouldEqual(direct.Value.Model.Revision);
        SemanticTypedContextSerializer.Serialize(wrapped.TypedContextDescriptors).ShouldEqual(SemanticTypedContextSerializer.Serialize(direct.TypedContextDescriptors));
        var before = direct.ImplementationRequirements.Single();
        var after = wrapped.ImplementationRequirements.Single();
        (after with { Source = before.Source, BodySpan = before.BodySpan, BodyLines = before.BodyLines }).ShouldEqual(before);
        after.ContextVersion.ShouldEqual(1U);
        after.ResultVersion.ShouldEqual(1U);
        after.RequiredCapability.ShouldEqual("pure");
        after.Role.ShouldEqual(SemanticImplementationRole.RulePredicate);
        after.Source.Span.StartLine.ShouldBeGreaterThan(before.Source.Span.StartLine);
        if (language != "file")
        {
            var rule = Rule(new ScreenplayCompiler().Parse(wrappedSource).Value!);
            rule.Code!.Code.ShouldEqual(Code);
            rule.Code.BodyStart!.Line.ShouldEqual(after.BodySpan!.Value.StartLine);
        }

        var changed = Bind(wrappedSource.Replace("hint \"Preserve criteria\"", "hint \"Second\"\n              hint \"First\"", StringComparison.Ordinal), supplied);
        SemanticModelSerializer.Serialize(changed.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(direct.Value.Model));
        SemanticTypedContextSerializer.Serialize(changed.TypedContextDescriptors).ShouldEqual(SemanticTypedContextSerializer.Serialize(direct.TypedContextDescriptors));
        changed.ImplementationRequirements.Single().ContentHash.ShouldEqual(before.ContentHash);
        changed.ImplementationRequirements.Single().RequirementId.ShouldEqual(before.RequirementId);
    }

    [Fact]
    public void should_keep_pending_out_of_attachment_allocation_and_refuse_execution()
    {
        const string attached = Prefix + "            file Rules/CheckLabel.cs\n";
        const string withPending = Prefix + "            implementation\n              hint \"Pending\"\n        validate\n          label rule CheckLabel\n            file Rules/CheckLabel.cs\n        validate\n          label rule CheckLabel\n            file Rules/Other.cs";
        var result = Bind(withPending);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
        result.ImplementationRequirements.Length.ShouldEqual(2);
        result.ImplementationRequirements[0].RequirementId.ShouldEqual(Bind(attached).ImplementationRequirements.Single().RequirementId);
        result.ImplementationRequirements[1].Member.ShouldEqual("label/CheckLabel#1");
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void should_preserve_allocation_before_invalid_property_validation()
    {
        var source = Prefix.Replace("label rule", "missing rule", StringComparison.Ordinal) + "            file A.cs\n          missing rule CheckLabel\n            implementation\n              file B.cs";
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.ImplementationRequirements.Select(value => value.Member).ShouldEqual(["missing/CheckLabel", "missing/CheckLabel#1"]);
    }

    [Fact]
    public void should_keep_distinct_member_identity_order_independent()
    {
        const string source = Prefix + "            file A.cs\n          label rule Other\n            implementation\n              file B.cs";
        var reversed = Prefix.Replace("CheckLabel severity warning message \"Invalid label\"", "Other", StringComparison.Ordinal) + "            implementation\n              file B.cs\n          label rule CheckLabel severity warning message \"Invalid label\"\n            file A.cs";
        var before = Bind(source).ImplementationRequirements.ToDictionary(value => value.Member!);
        foreach (var after in Bind(reversed).ImplementationRequirements) after.RequirementId.ShouldEqual(before[after.Member!].RequirementId);
    }

    [Fact]
    public void should_not_claim_to_execute_an_attached_opaque_predicate()
    {
        const string source = Prefix + "            implementation\n              file Rules/CheckLabel.cs\n      specification Reached\n        when Submit\n          label = \"ok\"\n        then error \"Invalid label\"";
        var compilation = Bind(source, true);
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var result = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        result.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        result.Passed.ShouldBeFalse();
    }

    [Fact]
    public void should_guard_public_typed_binding_and_transport()
    {
        const string source = Prefix + "            implementation\n              file A.cs";
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var rule = Rule(syntax);
        foreach (var invalid in new[]
        {
            rule with { Rule = ValidationRuleKind.NotEmpty },
            rule with { Rule = ValidationRuleKind.NotEmpty, File = null },
            rule with { Value = new PathExpressionSyntax("Bad.Name", rule.Location) },
            rule with { Value = new PathExpressionSyntax(null!, rule.Location) },
            rule with { Value = new PathExpressionSyntax("CheckLabel\n", rule.Location) },
            rule with { Code = new("csharp", Code, rule.Location) },
            rule with { Implementation = new(null!, rule.Location) },
            rule with { Implementation = new([new("\u0085", rule.Location)], rule.Location) },
            rule with { Implementation = new([null!], rule.Location) }
        })
        {
            Catch.Exception(() => SyntaxJson.Serialize(invalid)).ShouldBeOfExactType<InvalidSyntaxJson>();
            var result = Bind(source, false, ReplaceRule(syntax, invalid));
            result.Success.ShouldBeFalse();
            result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
        }

        const string conceptSource = "concept Label : String\n  validate\n    rule CheckLabel\n      file A.cs\n" + Prefix + "            file B.cs";
        var conceptSyntax = new ScreenplayCompiler().Parse(conceptSource).Value!;
        var concept = conceptSyntax.Concepts.Single();
        var block = (DeclarativeValidateSyntax)concept.Validations!.Single();
        var invalidConcept = conceptSyntax with { Concepts = [concept with { Validations = [block with { Rules = [block.Rules.Single() with { Implementation = rule.Implementation }] }] }] };
        Catch.Exception(() => SyntaxJson.Serialize(invalidConcept)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Bind(conceptSource, false, invalidConcept).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
        var pendingConcept = invalidConcept with { Concepts = [concept with { Validations = [block with { Rules = [block.Rules.Single() with { File = null, Implementation = rule.Implementation }] }] }] };
        Catch.Exception(() => SyntaxJson.Serialize(pendingConcept)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Bind(conceptSource, false, pendingConcept).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    }

    static ApplicationSyntax ReplaceRule(ApplicationSyntax syntax, ValidationRuleSyntax rule)
    {
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        return syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command with { Validations = [new DeclarativeValidateSyntax([rule], rule.Location)] }] }] }] }] };
    }

    static ValidationRuleSyntax Rule(ApplicationSyntax syntax) => ((DeclarativeValidateSyntax)syntax.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single()).Rules.Single();

    static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));

    static CompilationResult<SemanticCompilation> Bind(string source, bool supplied = false, ApplicationSyntax? syntax = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Rules"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", source);
        var attachments = supplied ? ImmutableDictionary<string, string>.Empty.Add("Rules/CheckLabel.cs", Code) : null;
        return new SemanticModelBinder().Bind("Rules", syntax ?? new ScreenplayCompiler().Parse(source, "model.play").Value!, SemanticDocumentSet.Create([document], catalog, attachments));
    }
}
