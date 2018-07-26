using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoFixture;
using AutoFixture.Kernel;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace PSFormatDeepString.Test
{
    public class PrettyPrinterTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public PrettyPrinterTest([NotNull] ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        [Fact]
        public void TestPrettyPrintDeepObject()
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new TypeRelay(typeof(IEnumerable),
                                                     typeof(ArrayList)));
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                   .ToList()
                   .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new GenerationDepthBehavior(2));

            var deepObject = fixture.Build<DeepObject>()
                                    .WithAutoProperties()
                                    .With(p => p.EnumerableOfSomething, fixture.Create<List<decimal>>())
                                    .Create();

            _outputHelper.WriteLine(PrettyPrinter.Print(deepObject));
        }

        [Fact]
        public void TestPrettyPrintException()
        {
            try
            {
                ThrowStuff();
            }
            catch (Exception e)
            {
                _outputHelper.WriteLine(PrettyPrinter.Print(e));
            }
        }

        [Fact]
        public void TestPrettyPrintObjectWithException()
        {
            try
            {
                ThrowStuff();
            }
            catch (Exception e)
            {
                _outputHelper.WriteLine(PrettyPrinter.Print(new
                                                            {
                                                                Name = "foo",
                                                                Age = 42,
                                                                Error = e
                                                            }));
            }
        }

        [Fact]
        public void TestPrettyPrintSelfReferencingException()
        {
            try
            {
                var o = new Exception("foo");
                o.Data.Add("Self", o);
                throw o;
            }
            catch (Exception e)
            {
                _outputHelper.WriteLine(PrettyPrinter.Print(e));
            }
        }

        private static void ThrowAtNextLevel()
        {
            ThrowAtNextLevel2();
        }

        private static void ThrowAtNextLevel2()
        {
            ThrowAtNextLevel3();
        }

        private static void ThrowAtNextLevel3()
        {
            try
            {
                throw new Exception("bob");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("foo", ex);
            }
        }

        private static void ThrowStuff()
        {
            ThrowAtNextLevel();
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class DeepObject
        {
            private List<DeepObject> _children;
            public bool Boolean { get; set; }
            public byte[] Bytes { get; set; }

            public List<DeepObject> Children
            {
                get => _children;
                set
                {
                    _children = value;
                    foreach (var child in _children)
                        child.Parent = this;
                }
            }

            public IEnumerable EnumerableOfSomething { get; set; }
            public IEnumerable<string> EnumerableOfStrings { get; set; }
            public HttpStatusCode HttpStatusCode { get; set; }
            public int Int { get; set; }

            public IDictionary<string, DeepObject> Map { get; set; }

            public DeepObject Parent { get; private set; }
            public string String { get; set; }
        }
    }
}
