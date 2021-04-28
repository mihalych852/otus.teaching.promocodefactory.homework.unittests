using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Otus.Teaching.PromoCodeFactory.Core.Abstractions.Repositories;
using Otus.Teaching.PromoCodeFactory.DataAccess;
using Otus.Teaching.PromoCodeFactory.DataAccess.Data;
using Otus.Teaching.PromoCodeFactory.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Otus.Teaching.PromoCodeFactory.WebHost.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMVCOptions(this IServiceCollection services)
        {
            services.AddControllers().AddMvcOptions(x =>
                 x.SuppressAsyncSuffixInActionNames = false);
        }

        public static void AddDatabaseOptions(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IDbInitializer, EfDbInitializer>();
            services.AddDbContext<DataContext>(x =>
            {
                x.UseSqlite("Filename=PromoCodeFactoryDb.sqlite");
                //x.UseNpgsql(Configuration.GetConnectionString("PromoCodeFactoryDb"));
                x.UseSnakeCaseNamingConvention();
                x.UseLazyLoadingProxies();
            });
        }

        public static void AddOpenAPI(this IServiceCollection services)
        {
            services.AddOpenApiDocument(options =>
            {
                options.Title = "PromoCode Factory API Doc";
                options.Version = "1.0";
            });
        }

        public static void AddDatabaseInMemoryOptions(this IServiceCollection services)
        {
            services.AddEntityFrameworkInMemoryDatabase();

            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IDbInitializer, EfDbInitializer>();

            services.AddDbContext<DataContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDb", builder => { });
                        options.UseInternalServiceProvider(services.BuildServiceProvider());

                    }
                );
        }
    }
}
