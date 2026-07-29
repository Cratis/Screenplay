// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_reserved_keywords_as_names;

public class and_a_concept_declares_a_validate_value : given.a_compiler
{
    const string Source =
        """
        concept InvoiceStatus : Enum
          draft
          @validate
          sent

        concept SwallowedStatus : Enum
          draft
          validate
          sent
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_declare_the_escaped_value() => Concept("InvoiceStatus").Values.ShouldContain("validate");
    [Fact] void should_declare_every_escaped_value() => Concept("InvoiceStatus").Values.Count().ShouldEqual(3);
    [Fact] void should_not_declare_a_validate_block_for_the_escaped_value() => Concept("InvoiceStatus").Validations!.ShouldBeEmpty();
    [Fact] void should_warn_about_the_unescaped_value() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_point_at_the_unescaped_value() => _result.Diagnostics.Single().Location.Line.ShouldEqual(8);
    [Fact] void should_keep_the_block_meaning_for_the_unescaped_value() => Concept("SwallowedStatus").Values.Count().ShouldEqual(2);

    ConceptSyntax Concept(string name) => _result.Value!.Concepts.Single(_ => _.Name == name);
}
