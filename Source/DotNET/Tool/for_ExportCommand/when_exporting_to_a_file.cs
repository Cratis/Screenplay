// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_to_a_file : Tool.given.a_renderable_model
{
    int _exitCode;
    string _path;

    void Establish() => _path = Path.Combine(Root, "model.esm.json");

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--output", _path], Output, Error);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_write_exactly_the_canonical_bytes() => File.ReadAllBytes(_path).SequenceEqual(SemanticModelSerializer.Serialize(LibraryModel(Root, Source))).ShouldBeTrue();
    [Fact] void should_write_nothing_to_standard_output() => Output.ToString().ShouldBeEmpty();
}
