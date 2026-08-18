// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// A path is carried, never resolved. A document is read in a designer, in a build and on a machine where the
/// tree is not present, so a path that has gone stale must not be what makes a valid document invalid.
/// </summary>
public class when_compiling_a_file_reference_nothing_on_disk_answers : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId : Uuid
          file Nowhere/AtAll/NotEvenClose.cs
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_nothing_at_all() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_path_verbatim() => _result.Value!.Concepts.Single().File!.Path.ShouldEqual("Nowhere/AtAll/NotEvenClose.cs");
}
