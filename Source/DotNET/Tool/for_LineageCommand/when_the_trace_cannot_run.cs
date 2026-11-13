// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_LineageCommand;

public class when_the_trace_cannot_run : given.the_library
{
    int _unknownSubject;
    int _unknownDirection;
    int _unknownFormat;
    int _noAddress;

    void Because()
    {
        _unknownSubject = ToolCommands.Run(["lineage", "Lending.Nothing.here", Root], Output, Error);
        _unknownDirection = ToolCommands.Run(["lineage", Title, Root, "--direction", "sideways"], Output, Error);
        _unknownFormat = ToolCommands.Run(["lineage", Title, Root, "--format", "csv"], Output, Error);
        _noAddress = ToolCommands.Run(["lineage"], Output, Error);
    }

    [Fact] void should_refuse_an_unknown_subject() => _unknownSubject.ShouldEqual(2);
    [Fact] void should_refuse_an_unknown_direction() => _unknownDirection.ShouldEqual(2);
    [Fact] void should_refuse_an_unknown_format() => _unknownFormat.ShouldEqual(2);
    [Fact] void should_refuse_a_missing_address() => _noAddress.ShouldEqual(2);
    [Fact] void should_explain_the_subject() => Error.ToString().ShouldContain("screen field address");
}
