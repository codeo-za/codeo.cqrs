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
        public class DefaultCachePolicy
        {
            [Test]
            public void ShouldHaveDefaultPropertyValues()
            {
                // Arrange
                var expected = new CacheItemPolicy();
                // Act
                var result = MemoryCache.DefaultCachePolicy;
                // Assert
                Expect(result.Priority)
                    .To.Equal(expected.Priority);
                Expect(result.AbsoluteExpiration)
                    .To.Equal(expected.AbsoluteExpiration);
                Expect(result.SlidingExpiration)
                    .To.Equal(expected.SlidingExpiration);
            }
        }

        [TestFixture]
        public class ConstructedWithNoParameters
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
        public class ConstructedWithExplicitSettings
        {
            [Test]
            public void ShouldSetUpNamedCacheWithSettings()
            {
                // Arrange
                var name = GetRandomString();
                var cacheSizeInMb = GetRandomInt(5, 50);
                var memoryLimit = GetRandomInt(50, 80);
                var pollingInterval = TimeSpan.FromSeconds(GetRandomInt(10, 20));
                var sut = new MemoryCache(
                    name,
                    cacheSizeInMb,
                    memoryLimit,
                    pollingInterval);
                // Act
                var actual = sut.Cache as System.Runtime.Caching.MemoryCache;
                // Assert
                Expect(actual).Not.To.Be.Null();
                Expect(actual.Name).To.Equal(name);
                Expect(actual.PollingInterval).To.Equal(pollingInterval);
                Expect(actual.CacheMemoryLimit).To.Equal(cacheSizeInMb * 1024 * 1024);
                Expect(actual.PhysicalMemoryLimit).To.Equal(memoryLimit);
            }
        }

        [TestFixture]
        public class Dispose
        {
            [Test]
            public void ShouldDisposeUnderlyingCache()
            {
                // Arrange
                var actual = new DisposableCache();
                var sut = Create(actual);
                Expect(actual.Disposed)
                    .To.Be.False();
                // Act
                sut.Dispose();
                // Assert
                Expect(actual.Disposed)
                    .To.Be.True();
            }

            public class DisposableCache : ObjectCache, IDisposable
            {
                public void Dispose()
                {
                    Disposed = true;
                }

                public bool Disposed { get; private set; }

                public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys,
                    string regionName = null)
                {
                    throw new NotImplementedException();
                }

                protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
                {
                    throw new NotImplementedException();
                }

                public override bool Contains(string key, string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override object AddOrGetExisting(string key,
                    object value,
                    DateTimeOffset absoluteExpiration,
                    string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
                {
                    throw new NotImplementedException();
                }

                public override object AddOrGetExisting(string key,
                    object value,
                    CacheItemPolicy policy,
                    string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override object Get(string key, string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override CacheItem GetCacheItem(string key, string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override void Set(string key,
                    object value,
                    DateTimeOffset absoluteExpiration,
                    string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override void Set(CacheItem item, CacheItemPolicy policy)
                {
                    throw new NotImplementedException();
                }

                public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override IDictionary<string, object> GetValues(IEnumerable<string> keys,
                    string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override object Remove(string key, string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override long GetCount(string regionName = null)
                {
                    throw new NotImplementedException();
                }

                public override DefaultCacheCapabilities DefaultCacheCapabilities { get; }
                public override string Name { get; }

                public override object this[string key]
                {
                    get => throw new NotImplementedException();
                    set => throw new NotImplementedException();
                }
            }
        }

        [TestFixture]
        public class ContainsKey
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
        public class Set
        {
            [TestFixture]
            public class GivenKey
            {
                [TestFixture]
                public class AndValue
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
                    public class AndAbsoluteExpiration
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
                    public class AndSlidingExpiration
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
        public class Get
        {
            [TestFixture]
            public class Untyped
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
            public class Typed
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
        public class GetOrDefault
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
        public class GetOrSet
        {
            public interface IFetcher
            {
                T Fetch<T>();
            }

            [TestFixture]
            public class WhenNoCachedValue
            {
                [TestFixture]
                public class GivenKey
                {
                    [TestFixture]
                    public class AndGenerator
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
                        public class AndSlidingExpiration
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
                        public class AndAbsoluteExpiration
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
            public class AndHaveCachedValue
            {
                [TestFixture]
                public class GivenKey
                {
                    [TestFixture]
                    public class AndGenerator
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
                        public class AndSlidingExpiration
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
                        public class AndAbsoluteExpiration
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
        public class Remove
        {
            [TestFixture]
            public class WhenNoMatchingKey
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
            public class WhenHaveMatchingKey
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
        public class RemoveAll
        {
            [TestFixture]
            public class WhenEmpty
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
            public class WhenHaveCache
            {
                [Test]
                public void ShouldClearAll()
                {
                    // Arrange
                    var actual = CreateSubstituteObjectCache();
                    // FIXME: PeanutButter.RandomGenerators is not generating random
                    // KeyValuePairs correctly -- update to GetRandomCollection when it does
                    var items = new[]
                    {
                        new KeyValuePair<string, int>(GetRandomString(), GetRandomInt()), 
                        new KeyValuePair<string, int>(GetRandomString(), GetRandomInt()), 
                        new KeyValuePair<string, int>(GetRandomString(), GetRandomInt()), 
                    };
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
            public class AttemptingToGetNonExistentItem
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
            public class AttemptingGetForMismatchedType
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
            public class EssentiallyTestingMemoryCache
            {
                [TestFixture]
                public class Expiration
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
                        var expires = DateTime.Now + TimeSpan.FromSeconds(1);
                        // Act
                        var result1 = sut.GetOrSet(
                            key,
                            fetcher.Fetch<int>,
                            expires
                        );
                        var result2 = sut.GetOrSet(key,
                            fetcher.Fetch<int>,
                            expires
                        );
                        Thread.Sleep(1100);
                        var result3 = sut.GetOrSet(
                            key,
                            fetcher.Fetch<int>,
                            expires
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

            private static ICache Create()
            {
                return new MemoryCache();
            }

            [SetUp]
            public void Setup()
            {
                ClearSystemRuntimeCachingMemoryCache();
            }

            [TearDown]
            public void Teardown()
            {
                ClearSystemRuntimeCachingMemoryCache();
            }

            private static void ClearSystemRuntimeCachingMemoryCache()
            {
                var cache = System.Runtime.Caching.MemoryCache.Default;
                var keys = cache
                    .Select(kvp => kvp.Key)
                    .ToArray();
                foreach (var key in keys)
                {
                    cache.Remove(key);
                }
            }
        }

        private static ObjectCache CreateSubstituteObjectCache()
        {
            return SubstituteObjectCacheBuilder
                .Create()
                .Build();
        }

        private static ICache Create(
            ObjectCache actual)
        {
            return new MemoryCache(actual);
        }
    }
}