// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateRoot;

public class when_resolving_a_state_marker : for_ScreenplayValidationGate.given.a_gate_file
{
    string _result;
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(Root, ".screenplay"));
        Directory.CreateDirectory(Path.Combine(Root, "child"));
    }

    void Because() => _result = ValidationGateRoot.Resolve(Path.Combine(Root, "child"));
    [Fact] void should_use_the_state_directory_even_without_a_gate() => _result.ShouldEqual(Root);
}
