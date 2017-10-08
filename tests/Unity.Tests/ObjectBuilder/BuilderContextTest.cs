﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;

namespace Microsoft.Practices.ObjectBuilder2.Tests
{
    [TestClass]
    public class BuilderContextTest : IBuilderStrategy
    {
        private IBuilderContext parentContext, childContext, receivedContext;
        private bool throwOnBuildUp;

        [TestInitialize]
        public void SetUp()
        {
            this.throwOnBuildUp = false;
        }

        [TestMethod]
        public void NewBuildSetsChildContextWhileBuilding()
        {
            this.parentContext = new BuilderContext(GetNonThrowingStrategyChain(), null, null, null, null, null);

            this.parentContext.NewBuildUp(null);

            Assert.AreSame(this.childContext, this.receivedContext);
        }

        [TestMethod]
        public void NewBuildClearsTheChildContextOnSuccess()
        {
            this.parentContext = new BuilderContext(GetNonThrowingStrategyChain(), null, null, null, null, null);

            this.parentContext.NewBuildUp(null);

            Assert.IsNull(this.parentContext.ChildContext);
        }

        [TestMethod]
        public void NewBuildDoesNotClearTheChildContextOnFailure()
        {
            this.parentContext = new BuilderContext(GetThrowingStrategyChain(), null, null, null, null, null);

            try
            {
                this.parentContext.NewBuildUp(null);
                Assert.Fail("an exception should have been thrown here");
            }
            catch (Exception)
            {
                Assert.IsNotNull(this.parentContext.ChildContext);
                Assert.AreSame(this.parentContext.ChildContext, this.receivedContext);
            }
        }

        private StrategyChain GetNonThrowingStrategyChain()
        {
            this.throwOnBuildUp = false;
            return new StrategyChain(new[] { this });
        }

        private StrategyChain GetThrowingStrategyChain()
        {
            this.throwOnBuildUp = true;
            return new StrategyChain(new[] { this });
        }

        public void PreBuildUp(IBuilderContext context)
        {
            this.childContext = this.parentContext.ChildContext;
            this.receivedContext = context;

            if (this.throwOnBuildUp)
            {
                throw new Exception();
            }
        }

        public void PostBuildUp(IBuilderContext context)
        {
        }

        public void PreTearDown(IBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public void PostTearDown(IBuilderContext context)
        {
            throw new NotImplementedException();
        }
    }
}
