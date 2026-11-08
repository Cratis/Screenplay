// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Ui;

namespace Cratis.Screenplay.for_Samples;

public class when_round_tripping_the_sample_screens : given.the_sample_workspaces
{
    readonly List<string> _changed = [];
    readonly List<string> _missing = [];
    readonly List<string> _unresolved = [];
    int _screens;

    void Because()
    {
        foreach (var (name, workspace) in _workspaces)
        {
            var contract = UiContractBuilder.For(workspace);
            var written = UiContractJson.Write(contract);
            if (!UiContractJson.Write(UiContractJson.Read(written)).AsSpan().SequenceEqual(written)) _changed.Add(name);
            var declared = new ScreenCounter();
            foreach (var document in Workspaces.WorkspaceSyntaxIndex.Create(workspace).Entries.Where(entry => entry.Parent is null).Select(entry => entry.Node).OfType<ApplicationSyntax>())
            {
                declared.VisitApplication(document);
            }

            if (declared.Count != contract.Screens.Count) _missing.Add($"{name}: {declared.Count} declared, {contract.Screens.Count} in the contract");
            _screens += contract.Screens.Count;
            if (contract.ModelRevision is null) continue;
            _unresolved.AddRange(contract.Screens.SelectMany(screen => screen.Actions.Where(action => action.Route is null).Select(action => $"{name}: {screen.Name} action {action.Command}")));
            _unresolved.AddRange(contract.Screens.SelectMany(screen => screen.Data.Where(data => data.Route is null).Select(data => $"{name}: {screen.Name} data {data.Query}")));
        }
    }

    [Fact] void should_read_screens_from_the_samples() => (_screens > 100).ShouldBeTrue();
    [Fact] void should_write_the_same_bytes_after_reading_them_back() => string.Join('\n', _changed).ShouldEqual(string.Empty);
    [Fact] void should_carry_every_declared_screen() => string.Join('\n', _missing).ShouldEqual(string.Empty);
    [Fact] void should_route_every_action_and_data_source_of_an_executable_model() => string.Join('\n', _unresolved).ShouldEqual(string.Empty);

    sealed class ScreenCounter : ScreenplaySyntaxWalker
    {
        public int Count { get; private set; }

        public override void VisitScreen(ScreenSyntax syntax) => Count++;
    }
}
