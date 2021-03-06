﻿using System;
using System.Collections.Generic;
using Unity.Builder;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Policy;
using Unity.Resolution;

namespace Unity.Registration
{
    public interface IRegistration : IPolicySet
    {
        string? Name { get; }

        ResolveDelegate<BuilderContext>? Pipeline { get; set; }

        IEnumerable<Pipeline>? Processors { get; set; }

        InjectionMember[]? InjectionMembers { get; set; }

        bool BuildRequired { get; }

        Converter<Type, Type>? BuildType { get; }

        LifetimeManager? LifetimeManager { get; }

        UnityContainer Owner { get; }
    }
}
