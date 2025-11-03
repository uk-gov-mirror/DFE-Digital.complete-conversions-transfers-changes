using Dfe.Complete.Application.Common.Interfaces;
using Dfe.Complete.Application.Contacts.Interfaces;
using Dfe.Complete.Application.DaoRevoked.Interfaces;
using Dfe.Complete.Application.KeyContacts.Interfaces;
using Dfe.Complete.Application.Notes.Interfaces;
using Dfe.Complete.Application.ProjectGroups.Interfaces;
using Dfe.Complete.Application.Projects.Interfaces;
using Dfe.Complete.Application.Projects.Interfaces.CsvExport;
using Dfe.Complete.Application.Users.Interfaces;
using Dfe.Complete.Domain.Interfaces.Repositories;
using Dfe.Complete.Infrastructure.CommandServices;
using Dfe.Complete.Infrastructure.Database;
using Dfe.Complete.Infrastructure.QueryServices;
using Dfe.Complete.Infrastructure.QueryServices.CsvExport;
using Dfe.Complete.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Dfe.Complete.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureDependencyGroup(this IServiceCollection services, IConfiguration config)
        {
            //Repos
            services.AddScoped(typeof(ICompleteRepository<>), typeof(CompleteRepository<>));

            //Cache service
            services.AddServiceCaching(config);

            //Db
            var connectionString = config.GetConnectionString("DefaultConnection");

            services.AddDbContext<CompleteContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Queries
            services.AddScoped<IListAllProjectsQueryService, ListAllProjectsQueryService>();
            services.AddScoped<IProjectsQueryBuilder, ProjectsQueryBuilder>();
            services.AddScoped<IConversionCsvQueryService, ConversionCsvQueryService>();
            services.AddScoped<INoteWriteRepository, NoteWriteRepository>();
            services.AddScoped<IProjectReadRepository, ProjectReadRepository>();
            services.AddScoped<IProjectGroupReadRepository, ProjectGroupReadRepository>();
            services.AddScoped<IProjectGroupWriteRepository, ProjectGroupWriteRepository>();
            services.AddScoped<INoteReadRepository, NoteReadRepository>();
            services.AddScoped<IUserReadRepository, UserReadRepository>();
            services.AddScoped<IUserWriteRepository, UserWriteRepository>();
            services.AddScoped<ILocalAuthoritiesQueryService, LocalAuthoritiesQueryService>();
            services.AddScoped<ITaskDataReadRepository, TaskDataReadRepository>();
            services.AddScoped<ITaskDataWriteRepository, TaskDataWriteRepository>();
            services.AddScoped<IKeyContactReadRepository, KeyContactReadRepository>();
            services.AddScoped<IKeyContactWriteRepository, KeyContactWriteRepository>();
            services.AddScoped<IContactReadRepository, ContactReadRepository>();
            services.AddScoped<IContactWriteRepository, ContactWriteRepository>();
            services.AddScoped<IDaoRevocationWriteRepository, DaoRevocationWriteRepository>();
            services.AddScoped<IProjectWriteRepository, ProjectWriteRepository>();
            services.AddScoped<IDaoRevocationReadRepository, DaoRevocationReadRepository>();

            // Authentication

            AddInfrastructureHealthChecks(services);

            var redisAppSettings = config.GetSection("Redis");
            if (redisAppSettings.GetValue<bool>("Enable"))
            {
                // Configure Redis Based Distributed Session
                var redisConfigurationOptions = new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    ResolveDns = true,
                    EndPoints = { $"{redisAppSettings.GetValue<string>("Host")}:{redisAppSettings.GetValue<int>("Port")}" },
                    Password = redisAppSettings.GetValue<string>("Password"),
                    User = "default",
                    DefaultDatabase = 0,
                    AsyncTimeout = 15000,
                    SyncTimeout = 15000,
                };

                services.AddStackExchangeRedisCache(redisCacheConfig =>
                {
                    redisCacheConfig.ConfigurationOptions = redisConfigurationOptions;
                    redisCacheConfig.InstanceName = "redis-master";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            return services;
        }

        public static void AddInfrastructureHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<CompleteContext>("Complete Database");
        }
    }
}
