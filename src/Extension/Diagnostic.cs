﻿using Unity.Policy;

namespace Unity.Extension
{
    public class Diagnostic : UnityContainerExtension
    {
        protected override void Initialize()
        {
            ((UnityContainer)Container).SetDiagnosticPolicies();

            Context.ChildContainerCreated += (s, e) => ((UnityContainer)e.ChildContainer).SetDiagnosticPolicies(); 
        }
    }
}
