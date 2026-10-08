// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_composite_stream_ids : given.a_compiler
{
    const string Source = "concept Key : Uuid\nconcept Period : String\neventsource Project\n  identifier Key\n  stream Ledger\n    streamId // parts\n      projectId Key // project\n      period Period // period\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Key identifier\n        month Period\n        stream Project.Ledger\n          streamId // route parts\n            period = month // mapping\n            projectId = id\n        produces event Recorded\n          for id\n          streamId String = month\n      specification History\n        given Recorded\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          stream Project.Ledger\n            streamId\n              period = \"2026-10\"\n              projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          streamId = \"payload\"\n        when append Recorded\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          stream Project.Ledger\n            streamId\n              projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n              period = \"2026-10\"\n          streamId = \"payload\"\n        then Recorded\n          stream Project.Ledger\n            streamId\n              period = \"2026-10\"\n              projectId = \"3FA85F64-5717-4562-B3FC-2C963F66AFA6\"\n          streamId = \"payload\"\n";

    [Fact]
    void should_parse_validate_serialize_and_print_parts_in_authored_order()
    {
        var result = _compiler.Compile(Source);
        result.Diagnostics.ShouldBeEmpty();
        var application = result.Value!;
        var json = SyntaxJson.Serialize(application);
        json.ToString().ShouldContain("EventStreamIdPartSyntax");
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(json);
        SyntaxJson.StructurallyEqual(application, decoded).ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(application);
        SyntaxJson.StructurallyEqual(application, _compiler.Parse(printed).Value!).ShouldBeTrue();
        printed.ShouldContain("streamId // parts\n      projectId Key // project\n      period Period // period");
        printed.ShouldContain("streamId // route parts\n            period = month // mapping\n            projectId = id");
    }

    [Theory]
    [InlineData("streamId\n      one String", "PLAY0503")]
    [InlineData("streamId\n      one String\n      one String", "PLAY0503")]
    [InlineData("streamId\n      one String identifier\n      two String", "PLAY0503")]
    [InlineData("streamId\n      one String\n        child String\n      two String", "PLAY0503")]
    [InlineData("streamId\n      one String optional\n      two String", "PLAY0503")]
    [InlineData("streamId\n      one String[]\n      two String", "PLAY0503")]
    [InlineData("streamId\n      one Decimal\n      two String", "PLAY0506")]
    [InlineData("streamId\n      one String\n      two String\n    streamId String", "PLAY0503")]
    [InlineData("streamId\n      one String\n      two String\n    streamId\n      three String\n      four String", "PLAY0503")]
    [InlineData("streamId", "PLAY0503")]
    void should_refuse_invalid_declaration_parts(string body, string code) => _compiler.Compile("eventsource A\n  stream S\n    " + body).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    [Theory]
    [InlineData("one = value")]
    [InlineData("one = value\n            two = value\n            extra = value")]
    [InlineData("one = value\n            two = value\n            two = value")]
    [InlineData("one = value\n            two = 42")]
    [InlineData("one = value\n            two = \"\"")]
    [InlineData("one = value\n            two = absent")]
    void should_refuse_invalid_route_parts(string mappings)
    {
        var source = "eventsource A\n  stream S\n    streamId\n      one String\n      two String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        stream A.S\n          streamId\n            " + mappings;
        _compiler.Compile(source).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeTrue();
    }
}
