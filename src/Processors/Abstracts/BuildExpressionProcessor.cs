﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Builder;
using Unity.Injection;
using Unity.Policy;

namespace Unity.Processors
{
    public delegate Expression MemberExpressionFactory(Attribute attribute, Expression member, object info, Type type, string name, object resolver);

    public abstract partial class BuildMemberProcessor<TMemberInfo, TData> : BuildMemberProcessor
                                                         where TMemberInfo : MemberInfo
    {
        #region Fields

        protected (Type type, MemberExpressionFactory factory)[] ExpressionFactories;

        #endregion


        #region Public Methods

        public void Add(Type type, MemberExpressionFactory factory)
        {
            for (var i = 0; i < ExpressionFactories.Length; i++)
            {
                if (ExpressionFactories[i].type != type) continue;
                ExpressionFactories[i].factory = factory;
                return;
            }

            var factories = new (Type type, MemberExpressionFactory factory)[ExpressionFactories.Length + 1];
            Array.Copy(ExpressionFactories, factories, ExpressionFactories.Length);
            factories[ExpressionFactories.Length] = (type, factory);
            ExpressionFactories = factories;
        }

        #endregion


        #region Overrides

        /// <inheritdoc />
        public override IEnumerable<Expression> GetBuildSteps(ref BuilderContext context)
        {
            var selector = GetPolicy<ISelect<TMemberInfo>>(ref context);
            var members = selector.Select(ref context);
            return ExpressionsFromSelected(context.Type, context.Name, members);
        }

        #endregion


        #region Build Expression 

        protected virtual IEnumerable<Expression> ExpressionsFromSelected(Type type, string name, IEnumerable<object> members)
        {
            foreach (var member in members)
            {

                switch (member)
                {
                    case TMemberInfo memberInfo:
                        yield return BuildMemberExpression(memberInfo, name, default);
                        break;

                    case InjectionMember<TMemberInfo, TData> injectionMember:
                        var (info, value) = injectionMember.FromType(type);
                        yield return BuildMemberExpression(info, name, value);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown MemberInfo<{typeof(TMemberInfo)}> type");
                }
            }
        }

        protected virtual Expression BuildMemberExpression(TMemberInfo info, string name, TData resolver)
        {
            var member = CreateMemberExpression(info);

            foreach (var pair in ExpressionFactories)
            {
#if NETSTANDARD1_0 || NETCOREAPP1_0
                var attribute = info.GetCustomAttributes()
                                    .Where(a => a.GetType()
                                                 .GetTypeInfo()
                                                 .IsAssignableFrom(pair.type.GetTypeInfo()))
                                    .FirstOrDefault();
#else
                var attribute = info.GetCustomAttribute(pair.type);
#endif
                if (null == attribute || null == pair.factory)
                    continue;

                return pair.factory(attribute, member, info, MemberType(info), name, resolver);
            }

            return Expression.Assign(member, GetExpression(info, name, resolver));
        }

        protected virtual Expression GetExpression(TMemberInfo info, string name, object resolver) => throw new NotImplementedException();

        protected virtual MemberExpression CreateMemberExpression(TMemberInfo info) => throw new NotImplementedException();

        protected abstract Type MemberType(TMemberInfo info);

        #endregion


        #region Parameter Expression Factories

        // Default expression factory for [Dependency] attribute
        protected virtual Expression DependencyExpressionFactory(Attribute attribute, Expression member, object memberInfo, Type type, string name, object resolver)
        {
            TMemberInfo info = (TMemberInfo)memberInfo;
            return Expression.Assign(member, GetExpression(info, ((DependencyResolutionAttribute)attribute).Name ?? name, resolver));
        }

        // Default expression factory for [OptionalDependency] attribute
        protected virtual Expression OptionalDependencyExpressionFactory(Attribute attribute, Expression member, object memberInfo, Type type, string name, object resolver)
        {
            TMemberInfo info = (TMemberInfo)memberInfo;
            return Expression.TryCatch(
                        Expression.Assign(member, GetExpression(info, ((OptionalDependencyAttribute)attribute).Name ?? name, resolver)),
                    Expression.Catch(typeof(Exception),
                        Expression.Assign(member, Expression.Constant(null, type))));
        }

        #endregion
    }
}
