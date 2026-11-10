// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Tool.for_DiffCommand;

public class when_comparing_as_json : given.two_model_versions
{
    int _exitCode;
    JsonElement _report;
    ModelDifference _library;

    void Because()
    {
        _exitCode = ToolCommands.Run(["diff", Root, After, "--format", "json"], Output, Error);
        _report = JsonElement.Parse(Output.ToString());
        _library = ModelComparison.Compare(
            ComparedModel.WithoutIdentities(ModelSource.Load(Root).Workspace),
            ComparedModel.WithoutIdentities(ModelSource.Load(After).Workspace));
    }

    JsonElement Difference => _report.GetProperty("difference");

    string[] Declarations => [.. Difference.GetProperty("declarations").EnumerateArray().Select(change =>
        change.GetProperty("change").GetString() + " " + (change.GetProperty("declaration").GetProperty("afterAddress").GetString() ?? change.GetProperty("declaration").GetProperty("beforeAddress").GetString()))];

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_name_both_versions() => _report.GetProperty("after").GetProperty("path").GetString().ShouldEqual(After);
    [Fact] void should_report_the_matching() => Difference.GetProperty("matching").GetString().ShouldEqual("address");
    [Fact]
    void should_report_the_library_declaration_changes() => Declarations.ShouldContainOnly(_library.Declarations.Select(change =>
        JsonNamingPolicy.CamelCase.ConvertName(change.Change.ToString()) + " " + (change.Declaration.AfterAddress ?? change.Declaration.BeforeAddress)));
    [Fact] void should_report_the_library_member_changes() => Difference.GetProperty("members").GetArrayLength().ShouldEqual(_library.Members.Count);
    [Fact] void should_report_the_library_dependants() => Difference.GetProperty("dependants").GetArrayLength().ShouldEqual(_library.Dependants.Count);
    [Fact]
    void should_report_the_library_slice_changes() => Difference.GetProperty("slices").GetProperty("slices").EnumerateArray().Select(slice => slice.GetProperty("change").GetString() + " " + slice.GetProperty("address").GetString())
        .ShouldContainOnly(_library.Slices.Slices.Select(slice => JsonNamingPolicy.CamelCase.ConvertName(slice.Change.ToString()) + " " + slice.Address));
    [Fact] void should_report_slice_changes() => Difference.GetProperty("slices").GetProperty("slices").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_report_the_semantic_change() => Difference.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
