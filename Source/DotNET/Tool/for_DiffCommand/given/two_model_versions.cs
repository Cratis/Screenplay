// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_DiffCommand.given;

public class two_model_versions : Tool.given.a_renderable_model
{
    protected string After;

    void Establish()
    {
        After = SpecFixtureDirectory.Create("model-diff");
        Write(After, Source
            .Replace("ProjectRegistered", "ProjectEnrolled", StringComparison.Ordinal)
            .Replace("    slice StateView Preferences", "    slice StateView Preferences\n    slice StateView Themes", StringComparison.Ordinal));
    }

    void Destroy() => SpecFixtureDirectory.Delete(After);
}
