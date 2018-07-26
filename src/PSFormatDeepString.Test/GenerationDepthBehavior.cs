using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoFixture.Kernel;

namespace PSFormatDeepString.Test
{
    internal class GenerationDepthBehavior : ISpecimenBuilderTransformation
    {
        private readonly int _generationDepth;

        public GenerationDepthBehavior(int generationDepth)
        {
            if (generationDepth < 1)
                throw new ArgumentOutOfRangeException(nameof(generationDepth), "Generation depth must be greater than 0.");

            _generationDepth = generationDepth;
        }

        public ISpecimenBuilderNode Transform(ISpecimenBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return new GenerationDepthGuard(builder, _generationDepth);
        }

        private class DepthSeededRequest : SeededRequest
        {
            public int Depth { get; }

            public DepthSeededRequest(object request, object seed, int depth)
                : base(request, seed)
            {
                Depth = depth;
            }
        }

        private sealed class GenerationDepthGuard : ISpecimenBuilderNode
        {
            private readonly ISpecimenBuilder _builder;
            private readonly int _generationDepth;
            private readonly ThreadLocal<Stack<DepthSeededRequest>> _requestsByThread = new ThreadLocal<Stack<DepthSeededRequest>>(() => new Stack<DepthSeededRequest>());

            public GenerationDepthGuard(ISpecimenBuilder builder, int generationDepth)
            {
                if (generationDepth < 1)
                    throw new ArgumentOutOfRangeException(nameof(generationDepth), "Generation depth must be greater than 0.");

                _builder = builder ?? throw new ArgumentNullException(nameof(builder));
                _generationDepth = generationDepth;
            }

            public ISpecimenBuilderNode Compose(IEnumerable<ISpecimenBuilder> builders)
            {
                var composedBuilder = ComposeIfMultiple(builders);
                return new GenerationDepthGuard(composedBuilder, _generationDepth);
            }

            public object Create(object request, ISpecimenContext context)
            {
                if (!(request is SeededRequest seededRequest))
                    return _builder.Create(request, context);

                var currentDepth = -1;

                var requestsForCurrentThread = GetMonitoredRequestsForCurrentThread();

                if (requestsForCurrentThread.Count > 0)
                    currentDepth = requestsForCurrentThread.Max(x => x.Depth) + 1;

                var depthRequest = new DepthSeededRequest(seededRequest.Request, seededRequest.Seed, currentDepth);

                if (depthRequest.Depth >= _generationDepth)
                    return new OmitSpecimen();

                requestsForCurrentThread.Push(depthRequest);
                try
                {
                    return _builder.Create(seededRequest, context);
                }
                finally
                {
                    requestsForCurrentThread.Pop();
                }
            }

            public IEnumerator<ISpecimenBuilder> GetEnumerator()
            {
                yield return _builder;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private Stack<DepthSeededRequest> GetMonitoredRequestsForCurrentThread() => _requestsByThread.Value;

            private static ISpecimenBuilder ComposeIfMultiple(IEnumerable<ISpecimenBuilder> builders)
            {
                ISpecimenBuilder singleItem = null;
                List<ISpecimenBuilder> multipleItems = null;
                var hasItems = false;

                using (var enumerator = builders.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        singleItem = enumerator.Current;
                        hasItems = true;

                        while (enumerator.MoveNext())
                        {
                            if (multipleItems == null)
                                multipleItems = new List<ISpecimenBuilder> { singleItem };

                            multipleItems.Add(enumerator.Current);
                        }
                    }
                }

                if (!hasItems)
                    return new CompositeSpecimenBuilder();

                if (multipleItems == null)
                    return singleItem;

                return new CompositeSpecimenBuilder(multipleItems);
            }
        }
    }
}
