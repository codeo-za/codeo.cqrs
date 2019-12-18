using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Tests.Models;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;
using MemoryCache = Codeo.CQRS.Caching.MemoryCache;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestMemoryCache
    {
        [Test]
        public void ShouldImplementICache()
        {
            // Arrange
            // Act
            Expect(typeof(NoCache))
                .To.Implement<ICache>();
            // Assert
        }

        [TestFixture]
        public class DefaultCachePolicy : TestMemoryCache
        {
            [Test]
            public void ShouldHaveDefaultPropertyValues()
            {
                // Arrange
                var expected = new CacheItemPolicy();
                // Act
                var result = MemoryCache.DefaultCachePolicy;
                // Assert
                Expect(result)
                    .To.Deep.Equal(expected);
            }
        }

        [TestFixture]
        public class ConstructedWithNoParameters : TestMemoryCache
        {
            [Test]
            public void ShouldUseDefaultMemoryCache()
            {
                // Arrange
                var sut = new MemoryCache();
                // Act
                var result = sut.Cache;
                // Assert
                Expect(result)
                    .To.Be(System.Runtime.Caching.MemoryCache.Default);
            }
        }

        [TestFixture]
        public class ContainsKey : TestMemoryCache
        {
            [Test]
            public void ShouldReturnFromUnderlying_Contains()
            {
                // Arrange
                var actual = CreateSubstituteObjectCache();
                var sut = Create(actual);
                var key = GetRandomString();
                var expected = GetRandomBoolean();
                actual.Contains(key).Returns(expected);
                // Act
                var result = sut.ContainsKey(key);
                // Assert
                Expect(result).To.Equal(expected);
                Expect(actual)
                    .To.Have.Received(1)
                    .Contains(key);
            }
        }

        [TestFixture]
        public class Set : TestMemoryCache
        {
            [TestFixture]
            public class GivenKey : Set
            {
                [TestFixture]
                public class AndValue : GivenKey
                {
                    [Test]
                    public void ShouldSet()
                    {
                        // Arrange
                        var actual = CreateSubstituteObjectCache();
                        var sut = Create(actual);
                        var key = GetRandomString();
                        var value = GetRandom<Person>();
                        // Act
                        sut.Set(
                            key,
                            value
                        );
                        // Assert
                        Expect(actual)
                            .To.Have.Received(1)
                            .Set(key, value, MemoryCache.DefaultCachePolicy);
                    }

                    [TestFixture]
                    public class AndAbsoluteExpiration : AndValue
                    {
                        [Test]
                        public void ShouldSetWithExpiration()
                        {
                            // Arrange
                            var actual = CreateSubstituteObjectCache();
                            var sut = Create(actual);
                            var key = GetRandomString();
                            var value = GetRandom<Person>();
                            var expiration = GetRandom<DateTime>();
                            // Act
                            sut.Set(
                                key,
                                value,
                                expiration
                            );
                            // Assert
                            Expect(actual)
                                .To.Have.Received(1)
                                .Set(key, value, Arg.Is<CacheItemPolicy>(
                                    o => o.AbsoluteExpiration == expiration &&
                                        o.SlidingExpiration == ObjectCache.NoSlidingExpiration
                                ));
                        }
                    }

                    [TestFixture]
                    public class AndSlidingExpiration : AndValue
                    {
                        [Test]
                        public void ShouldSetWithExpiration()
                        {
                            // Arrange
                            var actual = CreateSubstituteObjectCache();
                            var sut = Create(actual);
                            var key = GetRandomString();
                            var value = GetRandom<Person>();
                            var slidingExpiration = GetRandom<TimeSpan>();
                            // Act
                            sut.Set(
                                key,
                                value,
                                slidingExpiration
                            );
                            // Assert
                            Expect(actual)
                                .To.Have.Received(1)
                                .Set(key, value, Arg.Is<CacheItemPolicy>(
                                    o => o.SlidingExpiration == slidingExpiration &&
                                        // can only set one!
                                        o.AbsoluteExpiration == ObjectCache.InfiniteAbsoluteExpiration
                                ));
                        }
                    }
                }
            }
        }

        [TestFixture]
        public class Get : TestMemoryCache
        {
            [TestFixture]
            public class Untyped : Get
            {
                [Test]
                public void ShouldReturnNull()
                {
                    // Arrange
                    var actual = CreateSubstituteObjectCache();
                    var sut = Create(actual);
                    var key = GetRandomString();
                    // Act
                    var result = sut.Get(key);
                    // Assert
                    Expect(result)
                        .To.Be.Null();
                    Expect(actual)
                        .To.Have.Received(1)
                        .Get(key);
                }
            }

            [TestFixture]
            public class Typed : Get
            {
                [TestCase(typeof(int), default(int))]
                [TestCase(typeof(bool), default(bool))]
                public void ShouldReturnDefaultValueForType(
                    Type type,
                    object expected)
                {
                    // Arrange
                    var key = GetRandomString();
                    var actual = CreateSubstituteObjectCache();
                    var sut = Create(actual);
                    var method = typeof(MemoryCache)
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(mi =>
                            mi.Name == nameof(NoCache.Get) &&
                            mi.IsGenericMethod
                        );
                    var specific = method.MakeGenericMethod(type);
                    // Act
                    var result = specific.Invoke(sut, new object[] { key });
                    // Assert
                    Expect(result)
                        .To.Equal(expected);
                    Expect(actual)
                        .To.Have.Received(1)
                        .Get(key);
                }
            }
        }

        [TestFixture]
        public class GetOrDefault : TestMemoryCache
        {
            [Test]
            public void ShouldAlwaysReturnDefaultValue()
            {
                // Arrange
                var key = GetRandomString();
                var expected = GetRandomInt(1);
                var actual = CreateSubstituteObjectCache();
                var sut = Create(actual);
                // Act
                var result = sut.GetOrDefault(key, expected);
                // Assert
                Expect(result)
                    .To.Equal(expected);
                Expect(actual)
                    .To.Have.Received(1)
                    .Get(key);
            }
        }

        [TestFixture]
        public class GetOrSet : TestMemoryCache
        {
            public interface IFetcher
            {
                T Fetch<T>();
            }

            [TestFixture]
            public class WhenNoCachedValue : GetOrSet
            {
                [TestFixture]
                public class GivenKey : WhenNoCachedValue
                {
                    [TestFixture]
                    public class AndGenerator : GivenKey
                    {
                        [Test]
                        public void ShouldAlwaysReturnResultOfGenerator()
                        {
                            // Arrange
                            var key = GetRandomString();
                            var expected = GetRandomInt();
                            var actual = CreateSubstituteObjectCache();
                            var sut = Create(actual);
                            var fetcher = Substitute.For<IFetcher>();
                            fetcher.Fetch<int>().Returns(expected);
                            // Act
                            var result = sut.GetOrSet(
                                key,
                                fetcher.Fetch<int>
                            );
                            // Assert
                            Expect(result)
                                .To.Equal(expected);
                            Expect(actual)
                                // first get is optimistic
                                // second is locked
                                .To.Have.Received(2)
                                .Get(key);
                            Expect(fetcher)
                                .To.Have.Received(1)
                                .Fetch<int>();
                            Expect(actual)
                                .To.Have.Received(1)
                                .Set(key, expected, MemoryCache.DefaultCachePolicy);
                        }

                        [TestFixture]
                        public class AndSlidingExpiration : GivenKey
                        {
                            [Test]
                            public void ShouldAlwaysReturnResultOfGenerator()
                            {
                                // Arrange
                                var key = GetRandomString();
                                var expected = GetRandomInt();
                                var slidingExpiration = GetRandom<TimeSpan>();
                                var actual = CreateSubstituteObjectCache();
                                var sut = Create(actual);
                                var fetcher = Substitute.For<IFetcher>();
                                fetcher.Fetch<int>().Returns(expected);
                                // Act
                                var result = sut.GetOrSet(
                                    key,
                                    fetcher.Fetch<int>,
                                    slidingExpiration
                                );
                                // Assert
                                Expect(result)
                                    .To.Equal(expected);
                                Expect(actual)
                                    .To.Have.Received(2)
                                    .Get(key);
                                Expect(fetcher)
                                    .To.Have.Received(1)
                                    .Fetch<int>();
                                Expect(actual)
                                    .To.Have.Received(1)
                                    .Set(key, expected, Arg.Is<CacheItemPolicy>(
                                            o => o.AbsoluteExpiration == ObjectCache.InfiniteAbsoluteExpiration &&
                                                o.SlidingExpiration == slidingExpiration
                                        )
                                    );
                            }
                        }

                        [TestFixture]
                        public class AndAbsoluteExpiration : GivenKey
                        {
                            [Test]
                            public void ShouldAlwaysReturnResultOfGenerator()
                            {
                                // Arrange
                                var key = GetRandomString();
                                var expected = GetRandomInt();
                                var absoluteExpiration = GetRandom<DateTime>();
                                var actual = CreateSubstituteObjectCache();
                                var sut = Create(actual);
                                var fetcher = Substitute.For<IFetcher>();
                                fetcher.Fetch<int>().Returns(expected);
                                // Act
                                var result = sut.GetOrSet(
                                    key,
                                    fetcher.Fetch<int>,
                                    absoluteExpiration
                                );
                                // Assert
                                Expect(result)
                                    .To.Equal(expected);
                                Expect(actual)
                                    .To.Have.Received(2)
                                    .Get(key);
                                Expect(fetcher)
                                    .To.Have.Received(1)
                                    .Fetch<int>();
                                Expect(actual)
                                    .To.Have.Received(1)
                                    .Set(key, expected, Arg.Is<CacheItemPolicy>(
                                            o => o.AbsoluteExpiration == absoluteExpiration &&
                                                o.SlidingExpiration == ObjectCache.NoSlidingExpiration
                                        )
                                    );
                            }
                        }
                    }
                }
            }

            [TestFixture]
            public class AndHaveCachedValue : GetOrSet
            {
                [TestFixture]
                public class GivenKey : AndHaveCachedValue
                {
                    [TestFixture]
                    public class AndGenerator : GivenKey
                    {
                        [Test]
                        public void ShouldAlwaysReturnResultOfGenerator()
                        {
                            // Arrange
                            var key = GetRandomString();
                            var expected = GetRandomInt();
                            var actual = CreateSubstituteObjectCache();
                            var fetcher = Substitute.For<IFetcher>();
                            fetcher.Fetch<int>().Returns(GetAnother(expected));
                            var sut = Create(actual);
                            sut.Set(key, expected);
                            // Act
                            var result = sut.GetOrSet(
                                key,
                                fetcher.Fetch<int>
                            );
                            // Assert
                            Expect(result)
                                .To.Equal(expected);
                            Expect(actual)
                                .To.Have.Received(1)
                                .Get(key);
                        }

                        [TestFixture]
                        public class AndSlidingExpiration : GivenKey
                        {
                            [Test]
                            public void ShouldAlwaysReturnResultOfGenerator()
                            {
                                // Arrange
                                var key = GetRandomString();
                                var expected = GetRandomInt();
                                var slidingExpiration = GetRandom<TimeSpan>();
                                var actual = CreateSubstituteObjectCache();
                                var sut = Create(actual);
                                var fetcher = Substitute.For<IFetcher>();
                                fetcher.Fetch<int>().Returns(GetAnother(expected));
                                sut.Set(key, expected);
                                // Act
                                var result = sut.GetOrSet(
                                    key,
                                    fetcher.Fetch<int>,
                                    slidingExpiration
                                );
                                // Assert
                                Expect(result)
                                    .To.Equal(expected);
                                Expect(actual)
                                    .To.Have.Received(1)
                                    .Get(key);
                            }
                        }

                        [TestFixture]
                        public class AndAbsoluteExpiration : GivenKey
                        {
                            [Test]
                            public void ShouldAlwaysReturnResultOfGenerator()
                            {
                                // Arrange
                                var key = GetRandomString();
                                var expected = GetRandomInt();
                                var absoluteExpiration = GetRandom<DateTime>();
                                var actual = CreateSubstituteObjectCache();
                                var sut = Create(actual);
                                var fetcher = Substitute.For<IFetcher>();
                                fetcher.Fetch<int>().Returns(GetAnother(expected));
                                sut.Set(key, expected);
                                // Act
                                var result = sut.GetOrSet(
                                    key,
                                    fetcher.Fetch<int>,
                                    absoluteExpiration
                                );
                                // Assert
                                Expect(result)
                                    .To.Equal(expected);
                                Expect(actual)
                                    .To.Have.Received(1)
                                    .Get(key);
                            }
                        }
                    }
                }
            }
        }

        [TestFixture]
        public class Remove : TestMemoryCache
        {
            [TestFixture]
            public class WhenNoMatchingKey : Remove
            {
                [Test]
                public void ShouldNotThrow()
                {
                    // Arrange
                    var key = GetRandomString();
                    var actual = CreateSubstituteObjectCache();
                    var sut = Create(actual);
                    // Act
                    Expect(() => sut.Remove(key))
                        .Not.To.Throw();
                    // Assert
                }
            }

            [TestFixture]
            public class WhenHaveMatchingKey : TestMemoryCache
            {
                [Test]
                public void ShouldRemove()
                {
                    // Arrange
                    var key = GetRandomString();
                    var actual = CreateSubstituteObjectCache();
                    var sut = Create(actual);
                    sut.Set(key, GetRandomInt(1));
                    Expect(sut.ContainsKey(key))
                        .To.Be.True();
                    // Act
                    Expect(() => sut.Remove(key))
                        .Not.To.Throw();
                    var result = sut.ContainsKey(key);
                    // Assert
                    Expect(result).To.Be.False();
                }
            }
        }

        [TestFixture]
        public class RemoveAll : TestMemoryCache
        {
            [TestFixture]
            public class WhenEmpty : RemoveAll
            {
                [Test]
                public void ShouldNotThrow()
                {
                    // Arrange
                    var actual = CreateSubstituteObjectCache();
                    var sut = Create(actual);
                    // Act
                    Expect(() => sut.RemoveAll())
                        .Not.To.Throw();
                    // Assert
                }
            }

            [TestFixture]
            public class WhenHaveCache : RemoveAll
            {
                [Test]
                public void ShouldClearAll()
                {
                    // Arrange
                    var actual = CreateSubstituteObjectCache();
                    var items = GetRandomArray<KeyValuePair<string, int>>();
                    var sut = Create(actual);
                    items.ForEach(kvp => sut.Set(kvp.Key, kvp.Value));
                    // Act
                    sut.RemoveAll();
                    // Assert
                    items.ForEach(kvp =>
                        Expect(actual)
                            .To.Have.Received(1)
                            .Remove(kvp.Key)
                    );
                }
            }
        }

        [TestFixture]
        public class IntegrationTests
        {
            [TestFixture]
            public class AttemptingToGetNonExistentItem : IntegrationTests
            {
                [Test]
                public void ShouldReturn()
                {
                    // Arrange
                    var sut = Create();
                    // Act
                    var result = sut.Get(GetRandomString(10));
                    // Assert
                    Expect(result).To.Be.Null();
                }
            }

            [TestFixture]
            public class AttemptingGetForMismatchedType : IntegrationTests
            {
                [Test]
                public void ShouldAttemptConversion()
                {
                    // Arrange
                    var sut = Create();
                    var key = GetRandomString();
                    var value = GetRandomNumericString(2, 4);
                    var expected = int.Parse(value);
                    // Act
                    sut.Set(key, value);
                    var result = sut.Get<int>(key);
                    // Assert
                    Expect(result).To.Equal(expected);
                }

                [Test]
                public void ShouldReturnDefaultWhenConversionNotPossible()
                {
                    // Arrange
                    var sut = Create();
                    var key = GetRandomString();
                    var value = GetRandomAlphaString();
                    var expected = default(int);
                    // Act
                    sut.Set(key, value);
                    var result = sut.Get<int>(key);
                    // Assert
                    Expect(result).To.Equal(expected);
                }
            }

            [TestFixture]
            public class EssentiallyTestingMemoryCache : IntegrationTests
            {
                [TestFixture]
                public class Expiration : EssentiallyTestingMemoryCache
                {
                    [Test]
                    public void ShouldReFetchAfterItemHasExpired()
                    {
                        // Arrange
                        var sut = Create();
                        var key = GetRandomString();
                        var original = GetRandomInt(1);
                        var updated = GetRandomInt(2);
                        var fetcher = Substitute.For<GetOrSet.IFetcher>();
                        var count = 0;
                        fetcher.Fetch<int>()
                            .Returns(ci =>
                            {
                                switch (count++)
                                {
                                    case 0:
                                        return original;
                                    case 1:
                                        return updated;
                                    default:
                                        throw new InvalidOperationException();
                                }
                            });
                        var cacheTime = TimeSpan.FromSeconds(1);
                        // Act
                        var result1 = sut.GetOrSet(
                            key,
                            fetcher.Fetch<int>,
                            cacheTime
                        );
                        var result2 = sut.GetOrSet(key,
                            fetcher.Fetch<int>,
                            cacheTime
                        );
                        Thread.Sleep(1000);
                        var result3 = sut.GetOrSet(
                            key,
                            fetcher.Fetch<int>,
                            cacheTime
                        );
                        
                        // Assert
                        Expect(fetcher)
                            .To.Have.Received(2)
                            .Fetch<int>();
                        Expect(result1)
                            .To.Equal(original);
                        Expect(result2)
                            .To.Equal(original);
                        Expect(result3)
                            .To.Equal(updated);
                    }
                }
            }

            private ICache Create()
            {
                return new MemoryCache();
            }
        }

        private ObjectCache CreateSubstituteObjectCache()
        {
            var backingStore = new Dictionary<string, object>();
            var result = Substitute.For<ObjectCache>();
            result.When(o => o.Set(
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CacheItemPolicy>())
            ).Do(ci =>
            {
                var args = ci.Args();
                backingStore[args[0] as string] = args[1];
            });
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
            result.Contains(Arg.Any<string>())
                .Returns(ci => backingStore.ContainsKey(ci.Args()[0] as string));
            result.Contains(Arg.Any<string>(), Arg.Any<string>())
                .Returns(ci => backingStore.ContainsKey(ci.Args()[0] as string));
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
            var foo = result as IEnumerable<KeyValuePair<string, object>>;
            foo.GetEnumerator().Returns(ci => backingStore.GetEnumerator());
            return result;
        }

        private ICache Create(
            ObjectCache actual)
        {
            return new MemoryCache(actual);
        }
    }
}