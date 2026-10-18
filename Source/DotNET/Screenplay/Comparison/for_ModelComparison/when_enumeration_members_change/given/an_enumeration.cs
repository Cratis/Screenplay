// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change.given;

public class an_enumeration : for_ModelComparison.given.two_models
{
    protected const string Positional = "draft\n  issued\n  paid";
    protected const string Explicit = "draft = 1\n  issued = 4\n  paid = 7";

    void Establish() => Models(Positional, Positional);

    protected static string Model(string members) => $"concept Status : Enum\n  {members}\nconcept OtherStatus : Enum\n  first\n  second\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        status Status\n";

    protected void Models(string before, string after)
    {
        _before = Create(Model(before));
        Assert.True(_before.Compilation.Success, string.Join("; ", _before.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        ChangeSource(Model(after));
    }

    protected EnumerationMemberChange Member(string name) => _result.Enumerations.Single(change => change.Member == name);
}
