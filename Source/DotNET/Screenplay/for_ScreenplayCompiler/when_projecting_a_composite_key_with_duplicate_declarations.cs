// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_projecting_a_composite_key_with_duplicate_declarations : given.a_compiler
{
    Diagnostic[] _diagnostics;

    void Because() => _diagnostics = [.. _compiler.Compile("""
        module M
          feature F
            slice StateView S
              event Recorded
                first String
              readmodel Row
                first String key
                first String key
              projection Rows => Row
                from Recorded
                  key
                    first = first
        """).Diagnostics];

    [Fact] void should_report_the_duplicate_key_instead_of_throwing() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidReadModelKey);
    [Fact] void should_name_the_duplicate_key() => _diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelKey).Message.ShouldContain("'first' must be declared exactly once");
}
