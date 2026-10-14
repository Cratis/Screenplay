// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateRoot;

public class when_a_marker_is_at_the_boundary : for_ScreenplayValidationGate.given.a_gate_file
{
    string _result;
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(Root, ".git"));
        File.WriteAllText(GatePath, "{}");
        Directory.CreateDirectory(Path.Combine(Root, "child"));
    }

    void Because() => _result = ValidationGateRoot.Resolve(Path.Combine(Root, "child"));
    [Fact] void should_include_the_repository_root_in_the_search() => _result.ShouldEqual(Root);
}
