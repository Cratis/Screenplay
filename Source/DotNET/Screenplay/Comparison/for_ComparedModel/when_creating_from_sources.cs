// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ComparedModel;

public class when_creating_from_sources : Specification
{
    ComparedModel _result = null!;

    void Because() => _result = ComparedModel.FromSources("Projects", new Dictionary<string, string> { ["application.play"] = "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n" });

    [Fact] void should_not_claim_persisted_identities() => _result.HasPersistedIdentities.ShouldBeFalse();
    [Fact] void should_compile_the_sources() => _result.Workspace.Compilation.Success.ShouldBeTrue();
    [Fact] void should_retain_the_source_path() => _result.Workspace.Documents.Single().Path.Value.ShouldEqual("application.play");
}
