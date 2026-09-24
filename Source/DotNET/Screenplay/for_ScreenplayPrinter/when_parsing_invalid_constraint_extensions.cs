// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_parsing_invalid_constraint_extensions : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                code String
              constraint MixedKinds
                unique event ProjectRegistered
                unique code on ProjectRegistered
              constraint MalformedComposite
                unique code, on ProjectRegistered
              constraint DuplicateMessage
                unique code on ProjectRegistered
                message "First"
                message "Second"
              constraint MalformedRelease
                unique code on ProjectRegistered
                released by
              constraint RepeatedTarget
                unique code on ProjectRegistered
                unique code on ProjectRegistered
              constraint RepeatedProperty
                unique code, code on ProjectRegistered
              constraint RepeatedRelease
                unique code on ProjectRegistered
                released by ProjectReleased
                released by ProjectReleased
              constraint SameReleaseAndTarget
                unique code on ProjectRegistered
                released by ProjectRegistered
        """;

    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Parse(Source);

    [Fact] void should_reject_mixed_kinds() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DuplicateConstraintBody && _.Message.Contains("mix", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_missing_composite_property() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidConstraintBody && _.Message.Contains("unique code,", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_repeated_message() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DuplicateConstraintBody && _.Message.Contains("message", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_missing_release_event() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidConstraintBody && _.Message.Contains("released by", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_repeated_target() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DuplicateConstraintBody && _.Message.Contains("already targets", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_repeated_property() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DuplicateConstraintBody && _.Message.Contains("repeats a property", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_repeated_release() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DuplicateConstraintBody && _.Message.Contains("already releases", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_target_as_release() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.DuplicateConstraintBody && _.Message.Contains("cannot target and release", StringComparison.Ordinal)).ShouldBeTrue();
}
