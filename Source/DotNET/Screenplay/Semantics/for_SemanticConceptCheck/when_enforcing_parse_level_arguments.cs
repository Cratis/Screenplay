// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_enforcing_parse_level_arguments : given.a_declaration
{
    [Fact] void should_refuse_an_unknown_strategy() => Check(new("unknown", SourceLocation.Start)).Findings.Single().Code.ShouldEqual(DiagnosticCodes.UnknownGenerationStrategy);
    [Fact] void should_refuse_an_incompatible_primitive() => SemanticConceptCheck.Check(Concept("Int", new("ulid", SourceLocation.Start)), 2026).Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidGenerationPrimitive);
    [Fact] void should_require_a_typeid_prefix() => Check(new("typeid", SourceLocation.Start)).Findings.ShouldContain(finding => finding.Code == DiagnosticCodes.InvalidGenerationArgument && finding.Option == "prefix");
    [Fact] void should_check_typeid_prefix_shape() => Check(new("typeid", SourceLocation.Start) { Prefix = "Bad_" }).Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidTypeIdPrefix);
    [Fact] void should_check_prefix_length() => Check(new("ulid", SourceLocation.Start) { Prefix = new string('a', 33) }).Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidGenerationPrefix);
    [Fact] void should_check_bytes_count() => Check(new("bytes", SourceLocation.Start) { Count = 513, Encoding = "hex" }).Findings.Single().Code.ShouldEqual(DiagnosticCodes.GenerationSizeOutsideRange);
    [Fact] void should_check_bytes_encoding() => Check(new("bytes", SourceLocation.Start) { Count = 16, Encoding = "binary" }).Findings.Single().Option.ShouldEqual("encoding");
    [Fact] void should_check_case_depends_on_encoding() => Check(new("bytes", SourceLocation.Start) { Count = 16, Encoding = "base64url", Case = "upper" }).Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidGenerationModifier);
    [Fact] void should_check_case_choices() => Check(new("ulid", SourceLocation.Start) { Case = "mixed" }).Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidGenerationArgument);
    [Fact] void should_check_rule_name_shape() => Check(new("rule", SourceLocation.Start) { Rule = "not-a-name" }).Findings.Single().Option.ShouldEqual("rule");
    [Fact] void should_require_a_rule_body() => Check(new("rule", SourceLocation.Start) { Rule = "MakeReference" }).Findings.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_refuse_a_reversed_integer_range() => SemanticConceptCheck.Check(Concept("Int", new("random", SourceLocation.Start) { Range = "2..1" }), 2026).Findings.Single().Option.ShouldEqual("range");
    [Fact] void should_refuse_invalid_pattern_syntax() => Check(new("pattern", SourceLocation.Start) { Pattern = "[" }).Findings.Single().Option.ShouldEqual("pattern");
    [Fact] void should_refuse_an_invalid_assessment_horizon() => SemanticConceptCheck.Check(Concept("String"), 9900).Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidGenerationArgument);
    [Fact] void should_keep_new_enum_defaults_nonadmitting() => default(GenerationStrategyStatus).ShouldEqual(GenerationStrategyStatus.Unknown);
    [Fact] void should_keep_unknown_finding_targets_unknown() => default(SemanticConceptFindingTarget).ShouldEqual(SemanticConceptFindingTarget.Unknown);

    static SemanticConceptCheckResult Check(GenerationSyntax generation) => SemanticConceptCheck.Check(Concept("String", generation), 2026);
}
