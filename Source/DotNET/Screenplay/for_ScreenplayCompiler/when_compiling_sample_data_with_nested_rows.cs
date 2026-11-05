// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_sample_data_with_nested_rows : given.a_compiler
{
    const string Source = """
        type Line
          sku String
          quantity Int
          children Line[]

        module Shop
          feature Baskets
            slice StateView BasketList
              readmodel Basket
                lines Line[]

        sample data Demo
          shape Basket
            row
              lines
                row
                  sku = "BOOK"
                  quantity = 2
                  children
                    row
                      sku = "PEN"
                row
                  sku = "BAG"
            row
              lines
            row
        """;

    CompilationResult<ApplicationSyntax> _result;
    SampleDataRowSyntax[] _rows;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _rows = [.. _result.Value!.SampleDataPacks.Single().Sets.Single().Rows];
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_collection_kind() => _rows[0].Fields.First().Kind.ShouldEqual(SampleDataValueKind.Rows);
    [Fact] void should_keep_the_scalar_value_null() => _rows[0].Fields.First().Value.ShouldBeNull();
    [Fact] void should_parse_both_child_rows() => _rows[0].Fields.First().Rows.Count().ShouldEqual(2);
    [Fact] void should_preserve_child_order() => _rows[0].Fields.First().Rows.Select(row => row.Fields.First().Value).ShouldEqual(["BOOK", "BAG"]);
    [Fact] void should_parse_recursively() => _rows[0].Fields.First().Rows.First().Fields.Last().Rows.Single().Fields.Single().Value.ShouldEqual("PEN");
    [Fact] void should_keep_an_explicit_empty_list() => _rows[1].Fields.Single().Rows.ShouldBeEmpty();
    [Fact] void should_distinguish_an_omitted_field() => _rows[2].Fields.ShouldBeEmpty();
}
