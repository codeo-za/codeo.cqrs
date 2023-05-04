using NUnit.Framework;
using NExpect;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    public class TestCachingQuery
    {
        [Test]
        public void ShouldHaveUseCacheBooleanProperty()
        {
            // Arrange
            var sut = typeof(CachingQuery<int>);
            // Act
            Expect(sut)
                .To.Have.Property("UseCache")
                .With.Type(typeof(bool));
            // Assert
        }

        [TestFixture]
        public class WhenUseCacheSetFalse
        {
            [TestCase(CacheUsage.WriteOnly)]
            public void ShouldSetCacheUsage_(
                CacheUsage expected
            )
            {
                // Arrange
                var sut = new SomeDerivedCachingQuery();

                // Act
                sut.UseCache = true;
                sut.UseCache = false;

                // Assert
                Expect(sut.CacheUsage)
                    .To.Equal(CacheUsage.WriteOnly);
            }
        }

        [TestFixture]
        public class WhenUseCacheSetTrue
        {
            [TestCase(CacheUsage.Full)]
            public void ShouldSetCacheUsage_(
                CacheUsage expected
            )
            {
                // Arrange
                var sut = new SomeDerivedCachingQuery();

                // Act
                sut.UseCache = false;
                sut.UseCache = true;

                // Assert
                Expect(sut.CacheUsage)
                    .To.Equal(CacheUsage.Full);
            }
        }

        public class SomeDerivedCachingQuery
            : CachingQuery<int>
        {
            public override void Execute()
            {
            }

            public override void Validate()
            {
            }

            public SomeDerivedCachingQuery() 
                : base(false)
            {
            }
        }
    }
}