// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_compiling_a_directory : Specification
{
    DirectoryInfo _root;
    IEnumerable<PlayFileCompilation> _compilations;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("playcompile");
        File.WriteAllText(
            Path.Combine(_root.FullName, "valid.play"),
            """
            module Invoicing
              feature Invoices
                slice StateChange Register
                  event InvoiceRegistered
                    invoiceId InvoiceId
            """);
        File.WriteAllText(Path.Combine(_root.FullName, "invalid.play"), "bogus content");
    }

    void Because() => _compilations = new PlayFileCompiler().CompileIn(_root.FullName);

    [Fact] void should_compile_both_files() => _compilations.Count().ShouldEqual(2);
    [Fact] void should_fail_the_invalid_file() => _compilations.First().Result.Success.ShouldBeFalse();
    [Fact] void should_report_errors_for_the_invalid_file() => _compilations.First().Result.Diagnostics.Any(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_succeed_for_the_valid_file() => _compilations.Last().Result.Success.ShouldBeTrue();
    [Fact] void should_carry_the_source() => _compilations.Last().Source.ShouldContain("InvoiceRegistered");

    void Destroy() => _root.Delete(true);
}
