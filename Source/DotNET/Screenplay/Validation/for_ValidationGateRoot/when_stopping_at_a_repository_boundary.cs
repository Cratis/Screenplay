// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateRoot;

public class when_stopping_at_a_repository_boundary : for_ScreenplayValidationGate.given.a_gate_file
{
    string _start;
    string _result;

    void Establish()
    {
        File.WriteAllText(GatePath, "{}");
        var repository = Path.Combine(Root, "repository");
        _start = Path.Combine(repository, "model");
        Directory.CreateDirectory(_start);
        File.WriteAllText(Path.Combine(repository, ".git"), "gitdir: unused");
    }

    void Because() => _result = ValidationGateRoot.Resolve(_start);
    [Fact] void should_not_consume_a_gate_outside_the_repository() => _result.ShouldEqual(_start);
}
