// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_expanding_standalone_scoped_examples : Specification
{
    ApplicationSyntax _application;
    SpecificationSyntax _specification;
    CompilationResult<EffectiveSpecification> _result;

    void Establish()
    {
        var compiler = new ScreenplayCompiler();
        _application = compiler.Parse("""
            example Input : M.F.B.Record
              note = "root"
            module M
              feature F
                slice StateChange A
                  command Record
                    amount Int
                slice StateChange B
                  command Record
                    note String
            """).Value!;
        _specification = compiler.CompileSpecification("""
            example Input : Record
              amount = 10
            specification S
              when Input
              then no events
            """).Value!;
    }

    void Because() => _result = SpecificationExamples.Expand(_specification, _application, ["M", "F", "A"]);

    [Fact] void should_resolve_document_example_types_in_the_passed_scope() => _result.Value!.Effective.When!.CommandType.ShouldEqual("M.F.A.Record");
    [Fact] void should_not_collide_with_a_root_example_of_the_same_name() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_select_document_values() => _result.Value!.Effective.When!.Values.Single().Property.ShouldEqual("amount");
}
