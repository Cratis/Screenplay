// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_composite_specification_stream_ids : given.a_compiler
{
    const string Prefix = "concept Key : Uuid\neventsource A\n  identifier Key\n  stream S\n    streamId\n      key Key\n      text String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Key identifier\n        stream A.S\n          streamId\n            text = \"value\"\n            key = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n        produces event Recorded\n          for id\n      specification Expected\n        when C\n          id = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n        then Recorded\n          stream A.S\n            streamId\n";

    [Theory]
    [InlineData("key = \"3FA85F64-5717-4562-B3FC-2C963F66AFA6\"\n              text = \"value\"", false)]
    [InlineData("text = \"other\"\n              key = \"3FA85F64-5717-4562-B3FC-2C963F66AFA6\"", true)]
    [InlineData("text = \"value\"\n              key = \"3fa85f64-5717-4562-b3fc-2c963f66afa7\"", true)]
    void should_compare_each_part_canonically_by_name(string mappings, bool contradicts)
    {
        var result = _compiler.Compile(Prefix + "              " + mappings);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0551").ShouldEqual(contradicts);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0549").ShouldBeFalse();
    }

    [Theory]
    [InlineData("text = \"value\"")]
    [InlineData("key = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n              text = \"value\"\n              unknown = \"value\"")]
    [InlineData("key = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n              text = \"value\"\n              text = \"value\"")]
    [InlineData("key = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n              text = id")]
    [InlineData("key = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n              text = \"\"")]
    [InlineData("key = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n              text = 42")]
    void should_refuse_missing_unknown_duplicate_nonliteral_empty_or_incompatible_parts(string mappings) =>
        _compiler.Compile(Prefix + "              " + mappings).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0549").ShouldBeTrue();
}
