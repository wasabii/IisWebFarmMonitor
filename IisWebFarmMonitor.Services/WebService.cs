using System;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Cogito.Autofac;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Serialization;

namespace IisWebFarmMonitor.Services
{

    [RegisterAs(typeof(WebService))]
    public partial class WebService
    {

        readonly ILifetimeScope parent;
        ILifetimeScope scope;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="config"></param>
        public WebService(ILifetimeScope parent)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        /// <summary>
        /// Registers framework dependencies.
        /// </summary>
        /// <param name="services"></param>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            ConfigureMvcServices(parent, services);

            // return nested scope with new services
            return new AutofacServiceProvider(scope = parent.BeginLifetimeScope(builder => builder.Populate(services)));
        }

        /// <summary>
        /// Configures the MVC services.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="services"></param>
        void ConfigureMvcServices(IComponentContext context, IServiceCollection services)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // configure MVC
            var mvc = services.AddMvcCore();
            mvc.AddControllersAsServices();
            mvc.AddJsonFormatters();
            mvc.AddJsonOptions(i => i.SerializerSettings.ContractResolver = new DefaultContractResolver());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
        }

    }

}
