using JustSaying.Sample.Restaurant.Models;
using JustSaying.Sample.Restaurant.OrderingApi.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace JustSaying.Sample.Restaurant.OrderingApi
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddJustSaying(config =>
            {
                config.Client(x =>
                {
                    if (_configuration.HasAWSServiceUrl())
                    {
                        // The AWS client SDK allows specifying a custom HTTP endpoint.
                        // For testing purposes it is useful to specify a value that
                        // points to a docker image such as `p4tin/goaws` or `localstack/localstack`
                        x.WithServiceUri(_configuration.GetAWSServiceUri())
                         .WithAnonymousCredentials();
                    }
                    else
                    {
                        // The real AWS environment will require some means of authentication
                        //x.WithBasicCredentials("###", "###");
                        //x.WithSessionCredentials("###", "###", "###");
                    }
                });
                config.Messaging(x =>
                {
                    // Configures which AWS Region to operate in
                    x.WithRegion(_configuration.GetAWSRegion());
                });
                config.Subscriptions(x =>
                {
                    // Creates the following if they do not already exist
                    //  - a SQS queue of name `orderreadyevent`
                    //  - a SQS queue of name `orderreadyevent_error`
                    //  - a SNS topic of name `orderreadyevent`
                    //  - a SNS topic subscription on topic 'orderreadyevent' and queue 'orderreadyevent'
                    x.ForTopic<OrderReadyEvent>();
                });
                config.Publications(x =>
                {
                    // Creates the following if they do not already exist
                    //  - a SNS topic of name `orderplacedevent`
                    x.WithTopic<OrderPlacedEvent>();
                });
            });

            // Added a message handler for message type for 'OrderReadyEvent' on topic 'orderreadyevent' and queue 'orderreadyevent'
            services.AddJustSayingHandler<OrderReadyEvent, OrderReadyEventHandler>();

            // Add a background service that is listening for messages related to the above subscriptions
            services.AddHostedService<Subscriber>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Restaurant Ordering API", Version = "v1" });
            });
        }

        public static void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant Ordering API");
                c.RoutePrefix = string.Empty;
            });
            app.UseDeveloperExceptionPage();
            app.UseMvc();
        }
    }
}
