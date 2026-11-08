// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Samples.given;

public class the_sample_workspaces : the_samples
{
    protected IReadOnlyList<(string Name, ScreenplayWorkspace Workspace)> _workspaces;

    void Establish() => _workspaces = [.. _samples.Select(sample => (Path.GetFileName(sample), Workspace(sample)))];

    static ScreenplayWorkspace Workspace(string sample) => ComparedModel.FromSources(
        Path.GetFileName(sample),
        Directory.GetFiles(sample, "*.play", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToDictionary(file => Path.GetRelativePath(sample, file).Replace('\\', '/'), File.ReadAllText)).Workspace;
}
