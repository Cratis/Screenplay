// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_a_directory : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => Directory.CreateDirectory(GatePath);
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_refuse_as_unreadable() => _result.Failure.Kind.ShouldEqual(ValidationGateFailureKind.Unreadable);
    [Fact] void should_locate_the_refused_path() => _result.Failure.Path.ShouldEqual(GatePath);
}
