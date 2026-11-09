// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Ui;

namespace Cratis.Screenplay.for_Samples;

public class when_admitting_the_sample_screens_to_the_executable_model : given.the_sample_workspaces
{
    readonly List<string> _refused = [];
    readonly List<string> _differing = [];
    readonly List<string> _versions = [];
    int _screens;

    void Because()
    {
        // A sample that does not compile to an executable model has nothing to admit screens to.
        foreach (var (name, workspace) in _workspaces.Where(entry => entry.Workspace.Compilation.Success))
        {
            var admitted = ExecutableScreens.Admit(workspace);
            if (!admitted.Success)
            {
                _refused.Add($"{name}: {string.Join("; ", admitted.Refusals)}");
                continue;
            }

            var model = admitted.Compilation!.Model;
            if (model.SemanticVersion != SemanticVersion.V42) _versions.Add($"{name}: {model.SemanticVersion}");
            var read = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model));
            var fromSources = Neutral(UiContractBuilder.For(workspace));
            var fromModel = Neutral(UiContractBuilder.For(read));
            _screens += fromModel.Screens.Count;
            if (fromSources.Screens.Count != fromModel.Screens.Count)
            {
                _differing.Add($"{name}: {fromSources.Screens.Count} screens in the sources, {fromModel.Screens.Count} in the executable model");
                continue;
            }

            var actuals = fromModel.Screens.ToDictionary(screen => (screen.Slice, screen.Name));
            foreach (var expected in fromSources.Screens)
            {
                var same = actuals.TryGetValue((expected.Slice, expected.Name), out var actual) &&
                    UiContractJson.Write(fromModel with { Screens = [expected] }).AsSpan().SequenceEqual(UiContractJson.Write(fromModel with { Screens = [actual] }));
                if (!same) _differing.Add($"{name}: {expected.Slice}.{expected.Name}");
            }
        }
    }

    [Fact] void should_admit_every_sample() => string.Join('\n', _refused).ShouldEqual(string.Empty);
    [Fact] void should_select_esm_v42() => string.Join('\n', _versions).ShouldEqual(string.Empty);
    [Fact] void should_admit_screens_from_the_samples() => (_screens > 100).ShouldBeTrue();
    [Fact] void should_carry_the_same_neutral_contract_as_the_sources() => string.Join('\n', _differing).ShouldEqual(string.Empty);

    // The executable model holds the neutral contract only: no Scene layer and no template name.
    static UiContract Neutral(UiContract contract) => contract with
    {
        Screens = [.. contract.Screens.Select(screen => screen with { Scene = null, Layout = screen.Layout with { Template = null } })]
    };
}
