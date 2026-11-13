// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractBuilder.given;

public class a_contract : a_lending_model_with_screens
{
    protected UiContract Contract;

    void Establish() => Contract = UiContractBuilder.For(Workspace);

    protected UiScreen Screen(string name) => Contract.Screens.Single(screen => screen.Name == name);
}
