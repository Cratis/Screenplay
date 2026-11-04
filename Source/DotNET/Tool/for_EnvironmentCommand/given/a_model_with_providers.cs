// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_EnvironmentCommand.given;

public class a_model_with_providers : for_ModelCheck.given.a_model
{
    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), """
        authentication
          provider EntraId
          provider OpenId name Partner
        """);
}
