using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Practices.NetCore.Hangfire.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Practices.NetCore.Hangfire
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Practices.NetCore.Hangfire", Version = "v1" });
            });

            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("AppDbConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true // Migration to Schema 7 is required
                })
                .WithJobExpirationTimeout(TimeSpan.FromHours(6))//expired automatically after 24 hours by default.
                );

            // Add the processing server as IHostedService
            services.AddHangfireServer(opt => {//https://docs.hangfire.io/en/latest/background-methods/using-cancellation-tokens.html
                opt.CancellationCheckInterval = TimeSpan.FromSeconds(5); // Default value
            });

            services.AddTransient<IJobService, JobService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Practices.NetCore.Hangfire v1"));
            }

            app.UseHangfireDashboard("/manage-schedulers", new DashboardOptions {
                AppPath = "/swagger",
                DashboardTitle = "Manage schedulers!",
                Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                    {
                        RequireSsl = Convert.ToBoolean(Configuration.GetSection("HangfireCredentials:RequireSsl").Value),
                        SslRedirect = false,
                        LoginCaseSensitive = true,
                        Users = new []
                        {
                            new BasicAuthAuthorizationUser
                            {
                                Login = Configuration.GetSection("HangfireCredentials:UserName").Value,
                                PasswordClear =  Configuration.GetSection("HangfireCredentials:Password").Value
                            }
                        }

                    }) }
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            //Fire and forget job 
            var jobId = backgroundJobClient.Enqueue<IJobService>((x) => x.SendAsync("phong.le@gmail.com"));
            var jobId1 = backgroundJobClient.Enqueue<IJobService>((x) => x.LongRunningProcess("phong.le@gmail.com", CancellationToken.None));
            //Schedule Job / Delayed Job

            var jobId2 = backgroundJobClient.Schedule<IJobService>((x) => x.SendAsync("phong.le@gmail.com"), TimeSpan.FromSeconds(60));

            // Recurring Job for every 1 min            
            recurringJobManager.AddOrUpdate<IJobService>("creating-when-starting", (x) => x.SendAsync("phong.le@gmail.com"), Cron.Minutely());
        }
    }
}
