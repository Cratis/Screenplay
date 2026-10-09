// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness.for_PurposeCompleteness;

public class when_a_query_exposes_personal_data_from_another_slice : Specification
{
    CompilationResult<ApplicationSyntax> _compilation;
    string[] _concepts;

    void Establish() => _compilation = new ScreenplayCompiler().Compile("""
        concept PersonName : String pii
        module M
          feature F
            slice StateView Stored
              readmodel Contact
                name PersonName
            slice StateView Browse
              query Contacts => Contact[]
        """);

    void Because() => _concepts = [.. PurposeCoverage.Concepts(_compilation.Value!, _compilation.Value!.Modules.Single().Features.Single().Slices.Last()).Select(concept => concept.Name)];

    [Fact] void should_resolve_the_returned_read_model_shape() => _concepts.ShouldContainOnly("PersonName");
    [Fact] void should_compile_the_query_reference() => _compilation.Diagnostics.ShouldBeEmpty();
}
