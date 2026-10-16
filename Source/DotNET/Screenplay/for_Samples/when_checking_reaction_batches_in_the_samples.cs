// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Samples;

public class when_checking_reaction_batches_in_the_samples : given.the_samples
{
    readonly List<string> _errors = [];
    readonly List<string> _samplesWithReactions = [];
    readonly List<string> _samplesWithoutAtomicBatches = [];

    void Because()
    {
        var compiler = new PlayFileCompiler();
        foreach (var sample in _samples)
        {
            var name = Path.GetFileName(sample);
            var compilation = compiler.CompileFolder(sample).Result;
            if (!compilation.Success)
            {
                _errors.Add(name);
            }
            if (compilation.Value is { } application && SyntaxNodes.Under(application).OfType<ReactionSyntax>().Any())
            {
                _samplesWithReactions.Add(name);
                if (application.ReactionProductionMode != ReactionProductionMode.Atomic)
                {
                    _samplesWithoutAtomicBatches.Add(name);
                }
            }
        }
    }

    [Fact] void should_compile_every_sample_before_checking() => _errors.ShouldBeEmpty();
    [Fact] void should_find_samples_with_reactions() => _samplesWithReactions.ShouldNotBeEmpty();
    [Fact] void should_declare_atomic_batches_in_every_sample_with_reactions() => _samplesWithoutAtomicBatches.ShouldBeEmpty();
}
