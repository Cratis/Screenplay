// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateRoot;

public class when_resolving_nested_models : for_ScreenplayValidationGate.given.a_gate_file
{
    string _nested;
    string _result;

    void Establish()
    {
        File.WriteAllText(GatePath, "{}");
        _nested = Path.Combine(Root, "nested");
        Directory.CreateDirectory(_nested);
        File.WriteAllText(Path.Combine(_nested, "screenplay.json"), "{}");
    }

    void Because() => _result = ValidationGateRoot.Resolve(_nested);
    [Fact] void should_choose_the_nearest_model() => _result.ShouldEqual(_nested);
}
