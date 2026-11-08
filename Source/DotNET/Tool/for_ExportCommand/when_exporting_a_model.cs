// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_a_model : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--format", "esm"], Output, Error);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_write_the_library_serializer_bytes() => Output.ToString().ShouldEqual(Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(LibraryModel(Root, Source))));
    [Fact] void should_carry_the_schema_version() => Output.ToString().ShouldContain("\"schemaVersion\":");
    [Fact] void should_carry_the_revision() => Output.ToString().ShouldContain("\"revision\":\"rev1:");
    [Fact] void should_report_nothing_on_the_error_stream() => Error.ToString().ShouldBeEmpty();
}
