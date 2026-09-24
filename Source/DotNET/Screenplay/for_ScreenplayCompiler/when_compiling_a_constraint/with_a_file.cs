// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

public class with_a_file : given.a_compiler
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateChange IssueInvoice
              constraint UniqueInvoiceNumber
                file Constraints/UniqueInvoiceNumber.cs
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_keep_the_file_constraint_parseable() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Constraints.Single().ShouldBeOfExactType<FileConstraintSyntax>();
    [Fact] void should_compile_despite_the_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_that_only_uniqueness_is_supported() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.FileConstraintOnlySupportsUniqueness);
    [Fact] void should_report_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_point_to_the_file_directive() => _result.Diagnostics.Single().Location.Line.ShouldEqual(5);
}
