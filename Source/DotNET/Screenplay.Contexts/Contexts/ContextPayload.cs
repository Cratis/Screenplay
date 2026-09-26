// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

internal static class ContextPayload
{
    internal static T As<T>(object? payload, string member)
    {
        if (payload is T typed)
        {
            return typed;
        }

        throw new ContextPayloadTypeMismatch(member, typeof(T), payload?.GetType());
    }
}
