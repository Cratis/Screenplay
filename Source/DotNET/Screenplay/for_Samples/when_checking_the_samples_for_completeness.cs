// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.for_Samples;

public class when_checking_the_samples_for_completeness : given.the_samples
{
    readonly List<string> _findings = [];
    readonly List<string> _errors = [];

    void Because()
    {
        var compiler = new PlayFileCompiler();
        var checks = new CompletenessChecks([CompletenessCheck.DataBindings, CompletenessCheck.InputSurfaces, CompletenessCheck.FieldOrigins, CompletenessCheck.QueryKeys]);
        foreach (var sample in _samples)
        {
            var compilation = compiler.CompileFolder(sample).Result;
            if (!compilation.Success)
            {
                _errors.Add(Path.GetFileName(sample));
            }
            _findings.AddRange(ModelCompleteness.Check(compilation, checks).Select(finding => $"{Path.GetFileName(sample)}: {finding.Code} {finding.Message}"));
        }
    }

    [Fact] void should_compile_every_sample_before_checking() => _errors.ShouldBeEmpty();
    [Fact] void should_have_no_generic_completeness_findings() => string.Join('\n', _findings).ShouldEqual(string.Empty);
}
