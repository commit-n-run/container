﻿using Unity.Builder;
using Unity.Resolution;

namespace Unity
{
    public abstract class Pipeline
    {
        #region Public Members

        public virtual ResolveDelegate<BuilderContext>? Build(ref PipelineBuilder builder) => builder.Pipeline();

        #endregion
    }
}
