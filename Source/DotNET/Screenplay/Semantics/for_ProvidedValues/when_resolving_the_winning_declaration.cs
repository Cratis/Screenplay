// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_ProvidedValues;

public class when_resolving_the_winning_declaration : Specification
{
    const string Source =
        """
        provide region String from "Nordics"
        module Fleet
          provide region String from "Europe"
          provide entitlements String from host
          feature Quality
            provide limit Int
              file Limit.cs
            slice StateChange Adjust
              provide region String from "Norway"
              command Adjust
                vesselId Uuid
        """;

    ApplicationSyntax _application;
    IReadOnlyDictionary<string, ProvidedValue> _forSlice;
    IReadOnlyDictionary<string, ProvidedValue> _forFeature;

    void Because()
    {
        _application = new ScreenplayCompiler().Compile(Source).Value!;
        _forSlice = ProvidedValues.For(_application, "Fleet", ["Quality"], "Adjust");
        _forFeature = ProvidedValues.For(_application, "Fleet", ["Quality"]);
    }

    [Fact] void should_inherit_every_name_down_the_tree() => _forSlice.Keys.ShouldContainOnly("region", "entitlements", "limit");
    [Fact] void should_let_the_nearest_declaration_win() => _forSlice["region"].Level.ShouldEqual(ProvideLevel.Slice);
    [Fact] void should_keep_the_module_declaration_above_the_slice() => _forFeature["region"].Level.ShouldEqual(ProvideLevel.Module);
    [Fact] void should_keep_the_application_declaration_for_the_module_itself() => ProvidedValues.For(_application, "Fleet", [])["region"].Level.ShouldEqual(ProvideLevel.Module);
    [Fact] void should_report_host_values_as_opaque() => _forSlice["entitlements"].IsOpaque.ShouldBeTrue();
    [Fact] void should_report_code_values_as_opaque() => _forSlice["limit"].IsOpaque.ShouldBeTrue();
    [Fact] void should_not_report_a_literal_as_opaque() => _forSlice["region"].IsOpaque.ShouldBeFalse();
}
