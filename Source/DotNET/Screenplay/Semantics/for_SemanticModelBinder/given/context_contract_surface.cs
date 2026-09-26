// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

public static class context_contract_surface
{
    static readonly IReadOnlyDictionary<Type, string> Tokens = new Dictionary<Type, string>
    {
        [typeof(string)] = SemanticContextRuntimeTokens.Text,
        [typeof(long)] = SemanticContextRuntimeTokens.WholeNumber,
        [typeof(bool)] = SemanticContextRuntimeTokens.Boolean,
        [typeof(DateTimeOffset)] = SemanticContextRuntimeTokens.DateTime,
        [typeof(TenantId)] = SemanticContextRuntimeTokens.TenantId,
        [typeof(Identity)] = SemanticContextRuntimeTokens.Identity,
        [typeof(CausedBy)] = SemanticContextRuntimeTokens.CausedBy,
        [typeof(Causation)] = SemanticContextRuntimeTokens.Causation
    };

    public static void AssertMatches(Type context, SemanticTypedContextDescriptor descriptor)
    {
        var parameters = context.GetConstructors().Single().GetParameters();
        var stored = descriptor.Members.Where(member => !member.IsDerived).ToArray();
        parameters.Length.ShouldEqual(stored.Length);
        var nullability = new NullabilityInfoContext();
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            var member = stored[index];
            parameter.Name.ShouldEqual(member.Name);
            var isDynamic = parameter.GetCustomAttributes<DynamicAttribute>().Any();
            if (isDynamic)
            {
                (member.Type.Kind == SemanticContextTypeKinds.Shape || member.Type.Kind == SemanticContextTypeKinds.Model).ShouldBeTrue();
                (member.Type.Shape is not null || member.Type.ModelType is not null).ShouldBeTrue();
            }
            else
            {
                Tokens.TryGetValue(parameter.ParameterType, out var token).ShouldBeTrue();
                member.Type.Kind.ShouldEqual(SemanticContextTypeKinds.Runtime);
                member.Type.RuntimeToken.ShouldEqual(token);
            }
            member.IsNullable.ShouldEqual(nullability.Create(parameter).ReadState == NullabilityState.Nullable);
        }

        var derived = descriptor.Members.Where(member => member.IsDerived).ToArray();
        var properties = context.GetProperties().Where(property => !parameters.Any(parameter => parameter.Name == property.Name)).ToArray();
        properties.Length.ShouldEqual(derived.Length);
        for (var index = 0; index < properties.Length; index++)
        {
            properties[index].Name.ShouldEqual(derived[index].Name);
            Tokens.TryGetValue(properties[index].PropertyType, out var token).ShouldBeTrue();
            derived[index].Type.RuntimeToken.ShouldEqual(token);
        }
    }
}
