// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui.for_UiContractJson;

public class when_round_tripping : for_UiContractBuilder.given.a_contract
{
    byte[] _written;
    UiContract _read;

    void Because()
    {
        _written = UiContractJson.Write(Contract);
        _read = UiContractJson.Read(_written);
    }

    [Fact] void should_write_the_same_bytes_again() => UiContractJson.Write(_read).ShouldEqual(_written);
    [Fact] void should_read_every_screen() => _read.Screens.Select(screen => screen.Name).ShouldContainOnly(Contract.Screens.Select(screen => screen.Name));
    [Fact] void should_end_with_a_newline() => _written[^1].ShouldEqual((byte)'\n');
    [Fact] void should_refuse_another_contract_version() => Catch.Exception(() => UiContractJson.Read("{\"contract\":\"screenplay-ui/0\",\"application\":\"A\",\"screens\":[]}"u8)).ShouldBeOfExactType<InvalidDataException>();
}
