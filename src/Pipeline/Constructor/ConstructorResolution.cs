﻿using System;
using System.Linq;
using System.Reflection;
using Unity.Builder;
using Unity.Exceptions;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Policy;
using Unity.Resolution;

namespace Unity
{
    public partial class ConstructorPipeline
    {
        #region PipelineBuilder


        public override ResolveDelegate<BuilderContext>? Build(ref PipelineBuilder builder)
        {
            var pipeline = builder.Pipeline();

            if (null != builder.Seed) return pipeline;

            // Verify if can build
#if NETSTANDARD1_0 || NETCOREAPP1_0
            if (builder.Type.GetTypeInfo().IsGenericTypeDefinition)
#else
            if (builder.Type.IsGenericTypeDefinition)
#endif
            {
                return (ref BuilderContext context) =>
                {
                    if (null == context.Existing)
                        throw new InvalidRegistrationException(
                            $"The type {context.Type} is an open generic. An open generic type cannot be created.");

                    return null == pipeline ? context.Existing : pipeline.Invoke(ref context);
                };
            }

            // Select ConstructorInfo
            var selector = GetOrDefault(builder.Policies);
            var selection = selector.Invoke(builder.Type, builder.InjectionMembers)
                                    .FirstOrDefault();

            // Select constructor for the Type
            ConstructorInfo info;
            object[]? resolvers = null;

            switch (selection)
            {
                case ConstructorInfo memberInfo:
                    info = memberInfo;
                    break;

                case MethodBase<ConstructorInfo> injectionMember:
                    info = injectionMember.MemberInfo(builder.Type);
                    resolvers = injectionMember.Data;
                    break;

                case Exception exception:
                    return (ref BuilderContext c) =>
                    {
                        if (null == c.Existing)
                            throw exception;

                        return null == pipeline ? c.Existing : pipeline.Invoke(ref c);
                    };

                default:
                    return (ref BuilderContext c) =>
                    {
                        if (null == c.Existing)
                            throw new InvalidRegistrationException($"No public constructor is available for type {c.Type}.");

                        return null == pipeline ? c.Existing : pipeline.Invoke(ref c);
                    };
            }

            var lifetimeManager = (LifetimeManager?)builder.Policies?.Get(typeof(LifetimeManager));

            return lifetimeManager is PerResolveLifetimeManager
                ? GetPerResolveDelegate(info, resolvers, pipeline)
                : GetResolverDelegate(info, resolvers, pipeline);
        }

        #endregion


        #region Implementation

        protected virtual ResolveDelegate<BuilderContext> GetResolverDelegate(ConstructorInfo info, object? resolvers, ResolveDelegate<BuilderContext>? pipeline)
        {
            var parameterResolvers = CreateParameterResolvers(info.GetParameters(), resolvers).ToArray();

            return (ref BuilderContext context) =>
            {
                if (null == context.Existing)
                {
                    var dependencies = new object[parameterResolvers.Length];
                    for (var i = 0; i < dependencies.Length; i++)
                        dependencies[i] = parameterResolvers[i](ref context);

                    context.Existing = info.Invoke(dependencies);
                }

                return null == pipeline ? context.Existing : pipeline.Invoke(ref context);
            };
        }

        protected virtual ResolveDelegate<BuilderContext> GetPerResolveDelegate(ConstructorInfo info, object? resolvers, ResolveDelegate<BuilderContext>? pipeline)
        {
            var parameterResolvers = CreateParameterResolvers(info.GetParameters(), resolvers).ToArray();
            // PerResolve lifetime
            return (ref BuilderContext context) =>
            {
                if (null == context.Existing)
                {
                    var dependencies = new object[parameterResolvers.Length];
                    for (var i = 0; i < dependencies.Length; i++)
                        dependencies[i] = parameterResolvers[i](ref context);

                    context.Existing = info.Invoke(dependencies);
                    context.Set(typeof(LifetimeManager),
                          new InternalPerResolveLifetimeManager(context.Existing));
                }

                return null == pipeline ? context.Existing : pipeline.Invoke(ref context);
            };
        }
        
        #endregion
    }
}
