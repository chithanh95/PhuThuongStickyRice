using PhuThuongStickyRice.Application;
using NetArchTest.Rules;
using Xunit;

namespace PhuThuongStickyRice.ArchTests
{
    public class ApplicationTests
    {
        [Fact]
        public void ApplicationShouldNotHaveDependencyOnInfrastructure()
        {
            var result = Types.InAssembly(typeof(Dispatcher).Assembly)
                .ShouldNot()
                .HaveDependencyOn("PhuThuongStickyRice.Infrastructure")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public void ApplicationShouldNotHaveDependencyOnPersistence()
        {
            var result = Types.InAssembly(typeof(Dispatcher).Assembly)
                .ShouldNot()
                .HaveDependencyOn("PhuThuongStickyRice.Persistence")
                .GetResult();

            Assert.True(result.IsSuccessful);
        }
    }
}
