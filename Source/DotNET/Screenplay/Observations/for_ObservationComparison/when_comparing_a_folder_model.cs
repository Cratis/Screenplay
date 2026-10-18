// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_comparing_a_folder_model : given.a_compiled_model
{
    void Establish()
    {
        var workspace = ComparedModel.FromSources("Projects", new Dictionary<string, string>
        {
            ["application.play"] = "import \"register.play\"\n",
            ["register.play"] = Source
        }).Workspace;
        Assert.True(workspace.Compilation.Success, string.Join('\n', workspace.Compilation.Diagnostics.Select(value => value.Message)));
        _compilation = workspace.Compilation.Value!;
    }

    void Because() => _result = Compare();

    [Fact] void should_compare_the_composed_model() => _result.HasDrift.ShouldEqual(false);
    [Fact] void should_retain_slice_identity_and_address() => _result.Slices.Single().Address!.Name.ShouldEqual("Register");
}
