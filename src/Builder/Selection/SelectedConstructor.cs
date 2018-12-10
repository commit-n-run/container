﻿using System.Linq;
using System.Reflection;
using Unity.Injection;

namespace Unity.Builder.Selection
{
    /// <summary>
    /// Objects of this type are the return value from 
    /// <see cref="Unity.Policy.IConstructorSelectorPolicy.SelectConstructor"/>.
    /// It encapsulates the desired <see cref="ConstructorInfo"/> with the string keys
    /// needed to look up the <see cref="IResolverPolicy"/> for each
    /// parameter.
    /// </summary>
    public class SelectedConstructor : SelectedMemberWithParameters<ConstructorInfo>
    {
        /// <summary>
        /// Create a new <see cref="SelectedConstructor"/> instance which
        /// contains the given constructor.
        /// </summary>
        /// <param name="constructor">The constructor to wrap.</param>
        public SelectedConstructor(ConstructorInfo constructor)
            : base(constructor)
        {
        }

        public SelectedConstructor(ConstructorInfo info, object[] parameters)
            : base(info, parameters.Cast<InjectionParameterValue>()
                                   .Select(p => p.GetResolver<BuilderContext>(info.DeclaringType)))
        {
        }

        /// <summary>
        /// The constructor this object wraps.
        /// </summary>
        public ConstructorInfo Constructor => MemberInfo;
    }
}
