// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate;

public class when_reading_an_unreadable_file : given.a_gate_file
{
    ValidationGateReadResult _result;

    void Establish()
    {
        File.WriteAllText(GatePath, "{}");
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(GatePath, UnixFileMode.None);
    }

    void Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _result = ScreenplayValidationGate.Read(Root);

        // Restore access here: the base context deletes the folder before a derived cleanup would run.
        File.SetUnixFileMode(GatePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact] void should_refuse_without_falling_back_to_defaults() => (OperatingSystem.IsWindows() || _result.Failure.Kind == ValidationGateFailureKind.Unreadable).ShouldBeTrue();
}
