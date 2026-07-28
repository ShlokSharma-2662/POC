using Ecommerce.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Ecommerce.Tests.Infrastructure
{
    public class CacheInvalidationServiceTests
    {
        [Fact]
        public async Task InvalidateCartCacheAsync_RemovesCorrectUserCartKey()
        {
            var cacheService = new Mock<ICacheService>();
            var configuration = new Mock<IConfiguration>();
            configuration
                .Setup(config => config["Redis:IsCacheEnabled"])
                .Returns("true");
            var service = new CacheInvalidationService(
                cacheService.Object,
                configuration.Object);

            await service.InvalidateCartCacheAsync(42L);

            cacheService.Verify(
                cache => cache.RemoveAsync(CacheKeyBuilder.UserCart(42L)),
                Times.Once);
        }
    }
}
