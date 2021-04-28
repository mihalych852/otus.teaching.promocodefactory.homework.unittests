using Microsoft.Extensions.DependencyInjection;
using System;
using Otus.Teaching.PromoCodeFactory.WebHost.Extensions;
using Otus.Teaching.PromoCodeFactory.DataAccess.Data;

namespace Otus.Teaching.PromoCodeFactory.UnitTests.WebHost
{
    public class TestFixture_InMemory : IDisposable
    {
        public IServiceProvider ServiceProvider { get; set; }
        public IServiceCollection ServiceCollection { get; set; }

        /// <summary>
        /// Выполняется перед запуском тестов
        /// </summary>
        public TestFixture_InMemory()
        {
            
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMVCOptions();
            serviceCollection.AddDatabaseInMemoryOptions();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            ServiceProvider = serviceProvider;
            ServiceCollection = serviceCollection;

            var efDbInitializer = serviceProvider.GetService<IDbInitializer>();
            efDbInitializer.InitializeDb();
        }

        public void Dispose()
        {
            
        }
    }
}
