// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

public class when_migrating_a_file_with_a_byte_order_mark : given.a_screenplay_5_model
{
    int _exitCode;
    byte[] _bytes;

    void Establish() => File.WriteAllText(Document, Screenplay5, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    void Because()
    {
        _exitCode = ToolCommands.Run(["migrate", Root, "--from", "5"], Output, Error);
        _bytes = File.ReadAllBytes(Document);
    }

    [Fact] void should_migrate() => _exitCode.ShouldEqual(0);
    static readonly byte[] _mark = [0xEF, 0xBB, 0xBF];

    [Fact] void should_keep_the_byte_order_mark() => _bytes.Take(3).ToArray().ShouldEqual(_mark);
    [Fact] void should_write_it_once() => _bytes.Skip(3).Take(3).ToArray().ShouldNotEqual(_mark);
    [Fact] void should_write_the_migrated_text() => Encoding.UTF8.GetString(_bytes, 3, _bytes.Length - 3).ShouldContain("  navigation\n  outlet Navigation of navigation in navigation\n");
    [Fact] void should_leave_no_temporary_file() => Directory.GetFiles(Root).Select(Path.GetFileName).ShouldContainOnly("application.play");
}
