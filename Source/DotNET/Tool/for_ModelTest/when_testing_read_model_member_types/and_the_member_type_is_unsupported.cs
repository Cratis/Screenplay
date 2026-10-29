// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelTest.when_testing_read_model_member_types;

public class and_the_member_type_is_unsupported : given.a_model
{
    int _exit;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n  feature F\n    slice StateView View\n      readmodel Parent\n        child Child[]\n      readmodel Child\n        value String");
    void Because() => _exit = ModelTest.Run([Root], Output, Error);

    [Fact] void should_return_the_unbound_exit_code() => _exit.ShouldEqual(3);
    [Fact] void should_report_the_native_code_and_severity() => Output.ToString().ShouldContain("Error PLAY0273");
    [Fact] void should_report_the_member_coordinates() => Output.ToString().ShouldContain("application.play(5,15)");
    [Fact] void should_explain_the_supported_alternative() => Output.ToString().ShouldContain("Read model 'Parent' member 'child' references unsupported type 'Child'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.");
    [Fact] void should_not_misreport_an_unbound_model_as_passed() => Output.ToString().ShouldContain("0 executed");
    [Fact] void should_not_crash() => Error.ToString().ShouldBeEmpty();
}
