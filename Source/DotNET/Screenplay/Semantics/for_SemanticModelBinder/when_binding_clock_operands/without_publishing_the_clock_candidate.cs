// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class without_publishing_the_clock_candidate : given.a_clock_binder
{
    const string Body = "        validate\n          require dueDate >= today\n";

    [Fact]
    void should_refuse_to_write_a_clock_graph_at_the_working_major()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(binding.LanguageVersion, binding.SemanticVersion, binding.Application));
    }

    [Fact]
    void should_refuse_to_write_a_clock_graph_below_the_working_major()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V34, SemanticVersion.V34, binding.Application));
    }

    [Fact]
    void should_keep_the_public_factory_closed_to_the_working_major()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(binding.LanguageVersion, binding.SemanticVersion, binding.Application));
    }

    [Fact]
    void should_not_register_the_working_major_in_the_frozen_tables()
    {
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(new LanguageVersion(36, 0), new SemanticVersion(36, 0), BindClock(Source(string.Empty)).Value!.Application));
    }
}
