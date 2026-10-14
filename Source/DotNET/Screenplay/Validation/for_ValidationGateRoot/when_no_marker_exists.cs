// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateRoot;

public class when_no_marker_exists : for_ScreenplayValidationGate.given.a_gate_file
{
    string _result;
    void Establish() => Directory.CreateDirectory(Path.Combine(Root, ".git"));
    void Because() => _result = ValidationGateRoot.Resolve(Root);
    [Fact] void should_retain_the_start_folder() => _result.ShouldEqual(Root);
}
