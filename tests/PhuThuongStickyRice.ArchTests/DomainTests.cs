using PhuThuongStickyRice.Domain.Entities;
using NetArchTest.Rules;
using Xunit;

namespace PhuThuongStickyRice.ArchTests
{
    public class DomainTests
    {
        [Fact]
        public void DomainShouldNotHaveDependencyOnApplication()
        {
            var result = Types.InAssembly(typeof(Entity<>).Assembly)
                .ShouldNot()
                .HaveDependencyOn("PhuThuongStickyRice.Application")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void DomainShouldNotHaveDependencyOnInfrastructure()
        {
            var result = Types.InAssembly(typeof(Entity<>).Assembly)
                .ShouldNot()
                .HaveDependencyOn("PhuThuongStickyRice.Infrastructure")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void DomainShouldNotHaveDependencyOnPersistence()
        {
            var result = Types.InAssembly(typeof(Entity<>).Assembly)
                .ShouldNot()
                .HaveDependencyOn("PhuThuongStickyRice.Persistence")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }
    }
}
