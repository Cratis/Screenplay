// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck.given;

public class a_model : Specification
{
    protected string Root;
    protected StringWriter Output;
    protected StringWriter Error;

    void Establish()
    {
        Root = SpecFixtureDirectory.Create("model-check");
        File.WriteAllText(Path.Combine(Root, "application.play"), """
            module M
              feature F
                slice StateChange Clean
                  event CleanEvent
                    value String
                slice StateChange Warning
                  event WarningEvent
                    value UnknownWarningType
                slice StateChange Broken
                  event BrokenEvent
                    missingType
            """);
        Output = new();
        Error = new();
    }

    void Destroy()
    {
        Output.Dispose();
        Error.Dispose();
        SpecFixtureDirectory.Delete(Root);
    }
}
