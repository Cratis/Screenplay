// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_enum_member_numbers : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile("concept Status : Enum\n  description \"Stored identity\"\n  draft = -2147483648\n  issued = 2147483647\n  @validate = -0\n  validate\n    not empty");

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_member_names() => Concept.Values.ShouldContainOnly("draft", "issued", "validate");
    [Fact] void should_keep_numbers_parallel_to_names() => Concept.ValueNumbers.ShouldContainOnly(int.MinValue, int.MaxValue, 0);
    [Fact] void should_keep_validation_blocks() => Concept.Validations!.Count().ShouldEqual(1);
    [Fact] void should_keep_descriptions() => Concept.Description.ShouldEqual("Stored identity");

    ConceptSyntax Concept => _result.Value!.Concepts.Single();
}
