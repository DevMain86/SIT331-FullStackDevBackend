using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace robot_controller_api.Persistence.EF
{
    /// <summary>
    /// Design-time services for EF reverse engineering.
    /// EF will discover and call ConfigureDesignTimeServices when scaffolding.
    /// </summary>
    public class ScaffoldingDesignTimeServices : IDesignTimeServices
    {
        /// <summary>
        /// Register design-time services here.
        /// Register a post-scaffold processor that can be invoked by a custom
        /// scaffolding step or manually after scaffolding to transform generated metadata
        /// into code using Handlebars templates.
        /// </summary>
        /// <param name="services">Service collection provided by EF tooling.</param>
        public void ConfigureDesignTimeServices(IServiceCollection services)
        {
            // Register a post-scaffold processor (runs after scaffolding to render templates).
            services.AddSingleton<IPostScaffoldProcessor, HandlebarsPostScaffoldProcessor>();
        }
    }

    // Interface for a post-scaffold processor that runs template rendering.
    public interface IPostScaffoldProcessor
    {
        // Execute the post-scaffold processing step.
        void Run(string? scaffoldOutputDirectory = null);
    }

    // Simple example implementation that delegates to TemplateRunner.
    public class HandlebarsPostScaffoldProcessor : IPostScaffoldProcessor
    {
        public void Run(string? scaffoldOutputDirectory = null)
        {
            // Example: call TemplateRunner to render sample templates.
            // Extend this to read real scaffolding metadata and render per-entity files.
            try
            {
                var runner = new TemplateRunner();
                runner.RenderSampleTemplates(); // render sample output for verification
            }
            catch (Exception ex)
            {
                Console.WriteLine("Post-scaffold processor failed: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}