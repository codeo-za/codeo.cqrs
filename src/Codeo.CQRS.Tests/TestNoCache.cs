using System;
using System.Linq;
using System.Reflection;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Tests.Models;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestNoCache
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
        public class ContainsKey
        {
            [Test]
            public void ShouldAlwaysReturnFalse()
            {
                // Arrange
                var sut = Create();
                // Act
                var result = sut.ContainsKey(GetRandomString());
                // Assert
                Expect(result).To.Be.False();
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
                    public void ShouldNotThrow()
                    {
                        // Arrange
                        var sut = Create();
                        var key = GetRandomString();
                        var value = GetRandom<Person>();
                        // Act
                        Expect(() => sut.Set(
                            key,
                            value
                        )).Not.To.Throw();
                        // Assert
                    }

                    [TestFixture]
                    public class AndAbsoluteExpiration
                    {
                        [Test]
                        public void ShouldNotThrow()
                        {
                            // Arrange
                            var sut = Create();
                            var key = GetRandomString();
                            var value = GetRandom<Person>();
                            var expiration = GetRandom<DateTime>();
                            // Act
                            Expect(() => sut.Set(
                                key,
                                value,
                                expiration
                            )).Not.To.Throw();
                            // Assert
                        }
                    }

                    [TestFixture]
                    public class AndSlidingExpiration
                    {
                        [Test]
                        public void ShouldNotThrow()
                        {
                            // Arrange
                            var sut = Create();
                            var key = GetRandomString();
                            var value = GetRandom<Person>();
                            var slidingExpiration = GetRandom<TimeSpan>();
                            // Act
                            Expect(() => sut.Set(
                                key,
                                value,
                                slidingExpiration
                            )).Not.To.Throw();
                            // Assert
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
                    var sut = Create();
                    var key = GetRandomString();
                    // Act
                    var result = sut.Get(key);
                    // Assert
                    Expect(result)
                        .To.Be.Null();
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
                    var sut = Create();
                    var method = typeof(NoCache)
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
                    var expected = GetRandomInt();
                    var sut = Create();
                    // Act
                    var result = sut.GetOrDefault(key, expected);
                    // Assert
                    Expect(result).To.Equal(expected);
                }
            }

            [TestFixture]
            public class GetOrSet
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
                            var sut = Create();
                            // Act
                            var result = sut.GetOrSet(
                                key,
                                () => expected
                            );
                            // Assert
                            Expect(result)
                                .To.Equal(expected);
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
                                var sut = Create();
                                // Act
                                var result = sut.GetOrSet(
                                    key,
                                    () => expected,
                                    slidingExpiration
                                );
                                // Assert
                                Expect(result)
                                    .To.Equal(expected);
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
                                var sut = Create();
                                // Act
                                var result = sut.GetOrSet(
                                    key,
                                    () => expected,
                                    absoluteExpiration
                                );
                                // Assert
                                Expect(result)
                                    .To.Equal(expected);
                            }
                        }
                    }
                }
            }

            [TestFixture]
            public class Remove
            {
                [Test]
                public void ShouldNotThrow()
                {
                    // Arrange
                    var key = GetRandomString();
                    var sut = Create();
                    // Act
                    Expect(() => sut.Remove(key))
                        .Not.To.Throw();
                    // Assert
                }
            }

            [TestFixture]
            public class Clear
            {
                [Test]
                public void ShouldNotThrow()
                {
                    // Arrange
                    var sut = Create();
                    // Act
                    Expect(() => sut.Clear())
                        .Not.To.Throw();
                    // Assert
                }
            }
        }

        private static ICache Create()
        {
            return new NoCache();
        }
    }
}