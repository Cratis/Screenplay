// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_Samples.given;

public class the_samples : Specification
{
    protected IEnumerable<string> _samples;

    void Establish() => _samples = [.. Directory.GetDirectories(SamplesFolder()).Order(StringComparer.Ordinal)];

    static string SamplesFolder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var samples = Path.Combine(directory.FullName, "Samples");
            if (Directory.Exists(samples) && File.Exists(Path.Combine(directory.FullName, "Screenplay.slnx")))
            {
                return samples;
            }
        }

        throw new DirectoryNotFoundException($"No Samples folder found above '{AppContext.BaseDirectory}'");
    }
}
