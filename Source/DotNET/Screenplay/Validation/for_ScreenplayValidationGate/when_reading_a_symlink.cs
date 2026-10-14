// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_a_symlink : given.a_gate_file
{
    ValidationGateReadResult _result;
    void Establish() => File.CreateSymbolicLink(GatePath, Path.Combine(Root, "missing.json"));
    void Because() => _result = ScreenplayValidationGate.Read(Root);
    [Fact] void should_refuse_even_a_dangling_link() => _result.Failure.Kind.ShouldEqual(ValidationGateFailureKind.Unreadable);
}
