using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity.Lifetime;
using Unity.Registration;
using Unity.Storage;

namespace Unity.Tests.v5
{
    [TestClass]
    public class ServiceTitanIssues
    {
        [TestMethod]
        public void ContainerDuplicateRegistration()
        {
            for (var i = 0; i < 10000; i++)
            {
                var container = new UnityContainer()
                    .RegisterType<IFoo, Foo>()
                    .RegisterType<IBar, Bar>()
                    // It's important the name is random
                    .RegisterType<IBar, Bar>(Guid.NewGuid().ToString());

                var child = container
                    .CreateChildContainer()
                    .RegisterType<IFoo, Foo>(new ContainerControlledLifetimeManager());

                var registrations = child.Registrations
                    .Where(r => r.RegisteredType == typeof(IFoo))
                    .ToList();

                Assert.IsNotNull(
                    registrations.FirstOrDefault(r => r.LifetimeManager is ContainerControlledLifetimeManager),
                    "Singleton registration not found on iteration #" + i);

                // This check fails on random iteration, usually i < 300.
                // It passes for v.5.8.13 but fails for v.5.9.0 and later both for .NET Core and for Framework.
                Assert.IsNull(
                    registrations.FirstOrDefault(r => r.LifetimeManager is TransientLifetimeManager),
                    "Transient registration found on iteration #" + i);
            }
        }

        [TestMethod]
        public void RegistrationSetDuplicateRegistration()
        {
            var (s1, s2) = MakeCollision();

            var registrationSet = new RegistrationSet();
            var registration1 = new InternalRegistration();
            var registration2 = new InternalRegistration();
            var registration3 = new InternalRegistration();

            registrationSet.Add(typeof(IFoo), s1, registration1);
            Assert.AreEqual(1, registrationSet.Count);
            registrationSet.Add(typeof(IFoo), s2, registration2);
            Assert.AreEqual(2, registrationSet.Count);
            registrationSet.Add(typeof(IFoo), s1, registration3);
            Assert.AreEqual(2, registrationSet.Count);
        }

        private static (string, string) MakeCollision()
        {
            var strings = new Dictionary<int, string>();
            var random = new Random();
            var size = 10;

            var builder = new StringBuilder(size);
            while (true)
            {
                for (var j = 0; j < size; j++)
                    builder.Append((char) random.Next('a', 'z' + 1));

                var str = builder.ToString();
                var hash = str.GetHashCode();
                if (strings.TryGetValue(hash, out var other))
                    return (str, other);

                strings[hash] = str;
                builder.Clear();
            }
        }

        public interface IFoo { }
        public class Foo : IFoo { }

        public interface IBar { }
        public class Bar : IBar { }
    }
}