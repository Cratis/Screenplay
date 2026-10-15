// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_sample_data : given.a_compiler
{
    const string Source =
        """
        concept CustomerId : Uuid

        module Crm
          feature Customers
            slice StateView CustomerList
              readmodel Customer
                id CustomerId
                name String
                revenue Decimal
                age Int
                active Boolean
                notes String optional

        sample data NordicDemo
          title "CRM · Nordic SMB"
          locale nb-NO
          shape Customer
            row
              id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              name = "Fjordline \"AS\""
              revenue = 125000.5
              age = 41
              active = true
              notes = null
            row
              name = "Nordlys Handel AS"
              revenue = 0
        """;

    CompilationResult<ApplicationSyntax> _result;
    SampleDataPackSyntax _pack;
    SampleDataSetSyntax _set;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _pack = _result.Value!.SampleDataPacks.Single();
        _set = _pack.Sets.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_pack_name() => _pack.Name.ShouldEqual("NordicDemo");
    [Fact] void should_parse_the_title() => _pack.Title.ShouldEqual("CRM · Nordic SMB");
    [Fact] void should_parse_the_locale() => _pack.Locale.ShouldEqual("nb-NO");
    [Fact] void should_parse_the_shape() => _set.Shape.ShouldEqual("Customer");
    [Fact] void should_parse_both_rows() => _set.Rows.Count().ShouldEqual(2);
    [Fact] void should_parse_the_fields_in_authored_order() => _set.Rows.First().Fields.Select(field => field.Name).ShouldContainOnly("id", "name", "revenue", "age", "active", "notes");
    [Fact] void should_unescape_strings() => _set.Rows.First().Fields.Single(field => field.Name == "name").Value.ShouldEqual("Fjordline \"AS\"");
    [Fact] void should_keep_numbers_as_their_authored_token() => _set.Rows.First().Fields.Single(field => field.Name == "revenue").Value.ShouldEqual("125000.5");
    [Fact] void should_parse_numbers() => _set.Rows.First().Fields.Single(field => field.Name == "age").Kind.ShouldEqual(SampleDataValueKind.Number);
    [Fact] void should_parse_booleans() => _set.Rows.First().Fields.Single(field => field.Name == "active").Kind.ShouldEqual(SampleDataValueKind.Flag);
    [Fact] void should_parse_null() => _set.Rows.First().Fields.Single(field => field.Name == "notes").Kind.ShouldEqual(SampleDataValueKind.Null);
    [Fact] void should_not_make_the_pack_part_of_a_ui_profile() => (_result.Value!.UiProfiles ?? []).ShouldBeEmpty();
}
