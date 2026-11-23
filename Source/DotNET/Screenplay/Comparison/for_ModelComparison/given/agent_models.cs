// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.given;

public class agent_models : two_models
{
    protected const string Query = "query Projects.Registration.List.All\n    scope read\n    description \"List projects\"";
    protected const string Command = "command Projects.Registration.Register.Register\n    scope write\n    description \"Register projects\"";

    protected static string Agents(params string[] entries) => "\nagents\n  tokens personal\n" + string.Join('\n', entries.Select(entry => "  " + entry));

    protected AgentExposureDifference Compare(string before, string after, bool identities = false)
    {
        _before = Create(before);
        _after = Create(after, _before.IdentityCatalog);
        _result = ModelComparison.Compare(Input(_before), Input(_after));
        return _result.AgentExposure;

        ComparedModel Input(Workspaces.ScreenplayWorkspace workspace) => identities ? ComparedModel.WithIdentities(workspace) : ComparedModel.WithoutIdentities(workspace);
    }
}
