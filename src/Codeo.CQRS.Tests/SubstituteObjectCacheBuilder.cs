using System.Collections.Generic;
using System.Runtime.Caching;
using NSubstitute;
using PeanutButter.RandomGenerators;

namespace Codeo.CQRS.Tests
{
    public class SubstituteObjectCacheBuilder : BuilderBase<SubstituteObjectCacheBuilder, ObjectCache>, IBuilder<ObjectCache>
    {
        private IDictionary<string, object> _backingStore;

        public SubstituteObjectCacheBuilder WithBackingStore(
            IDictionary<string, object> store)
        {
            _backingStore = store;
            return this;
        }

        public ObjectCache Build()
        {
            var backingStore = _backingStore ?? new Dictionary<string, object>();
            var result = Substitute.For<ObjectCache>();
            MockSetMethodOn(result, backingStore);
            MockGetMethodsOn(result, backingStore);
            MockContainsMethodsOn(result, backingStore);
            MockRemoveMethodsOn(result, backingStore);
            MockEnumerableInterfaceOn(result, backingStore);
            return result;
        }

        private static void MockEnumerableInterfaceOn(ObjectCache result, IDictionary<string, object> backingStore)
        {
            var foo = result as IEnumerable<KeyValuePair<string, object>>;
            foo.GetEnumerator().Returns(ci => backingStore.GetEnumerator());
        }

        private static void MockRemoveMethodsOn(ObjectCache result, IDictionary<string, object> backingStore)
        {
            result.When(o => o.Remove(Arg.Any<string>()))
                .Do(ci =>
                {
                    backingStore.Remove(ci.Args()[0] as string);
                });
            result.When(o => o.Remove(Arg.Any<string>(), Arg.Any<string>()))
                .Do(ci =>
                {
                    backingStore.Remove(ci.Args()[0] as string);
                });
        }

        private static void MockContainsMethodsOn(ObjectCache result, IDictionary<string, object> backingStore)
        {
            result.Contains(Arg.Any<string>())
                .Returns(ci => backingStore.ContainsKey(ci.Args()[0] as string));
            result.Contains(Arg.Any<string>(), Arg.Any<string>())
                .Returns(ci => backingStore.ContainsKey(ci.Args()[0] as string));
        }

        private static void MockGetMethodsOn(ObjectCache result, IDictionary<string, object> backingStore)
        {
            result.Get(Arg.Any<string>(), Arg.Any<string>())
                .Returns(ci =>
                {
                    var args = ci.Args();
                    return backingStore.TryGetValue(args[0] as string, out var cached)
                        ? cached
                        : null;
                });
            result.Get(Arg.Any<string>())
                .Returns(ci =>
                {
                    var args = ci.Args();
                    return backingStore.TryGetValue(args[0] as string, out var cached)
                        ? cached
                        : null;
                });
        }

        private static void MockSetMethodOn(ObjectCache result, IDictionary<string, object> backingStore)
        {
            result.When(o => o.Set(
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CacheItemPolicy>())
            ).Do(ci =>
            {
                var args = ci.Args();
                backingStore[args[0] as string] = args[1];
            });
        }
    }
}