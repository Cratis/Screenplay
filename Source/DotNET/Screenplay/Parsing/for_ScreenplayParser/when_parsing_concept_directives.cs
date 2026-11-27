// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ScreenplayParser;

public class when_parsing_concept_directives : Specification
{
    const string Source =
        """
        concept Thing : Widget
          frobnicate

        concept Mode : Enum
          validate

        concept Colour : Enum
          red
          green
          Red!!
        """;

    CompilationResult<ApplicationSyntax> _result;
    string[] _messages;

    void Because()
    {
        _result = new ScreenplayCompiler().Compile(Source);
        _messages = [.. _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")];
    }

    [Fact] void should_report_the_unknown_primitive_type() => _messages.ShouldContain($"{DiagnosticCodes.UnknownPrimitiveType} Unknown primitive type 'Widget' - expected {string.Join(", ", ConceptSyntax.PrimitiveTypes)} or Enum");
    [Fact] void should_report_the_unknown_concept_directive() => _messages.ShouldContain($"{DiagnosticCodes.UnknownConceptDirective} Unexpected 'frobnicate' in concept body - expected validate, generate, 'file <path>' or '<attribute> reason \"<text>\"'");
    [Fact] void should_warn_that_validate_reads_as_an_empty_block() => _messages.ShouldContain($"{DiagnosticCodes.ValidateReadAsEnumerationBlock} 'validate' in enumeration concept 'Mode' declares an empty validate block, not a value named 'validate' - write '@validate' for the value");
    [Fact] void should_report_the_invalid_enumeration_value() => _messages.ShouldContain($"{DiagnosticCodes.InvalidEnumerationValue} Invalid enum value 'Red!!' - expected an identifier");
    [Fact] void should_keep_the_valid_enumeration_values_in_order() => _result.Value!.Concepts.Single(concept => concept.Name == "Colour").Values.ShouldEqual("red", "green");
}
