// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModelScope.given;

public class a_model_with_modules : Specification
{
    protected ExecutableSemanticModel Model;

    void Establish() => Model = ComparedModel.FromSources("Scopes", new Dictionary<string, string>
    {
        ["application.play"] = """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    projectId ProjectId identifier
                    produces Registered
                      for projectId
                      projectId = projectId
                  event Registered
                    projectId ProjectId
                slice StateView Listing
              feature Archive
                feature History
                  slice StateView Timeline
            module Administration
              feature Settings
                slice StateView Preferences
            """
    }).Workspace.Compilation.Value!.Model!;
}
