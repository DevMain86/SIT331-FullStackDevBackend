using System;
using System.Collections.Generic;
using System.IO;
using HandlebarsDotNet;
using robot_controller_api.Persistence.EF.Helpers;

namespace robot_controller_api.Persistence.EF
{
    public class TemplateRunner
    {
        // Relative path to the templates folder (adjust if your project layout differs)
        private readonly string templateFolderPath = Path.Combine("Persistence", "EF", "Templates");

        public TemplateRunner()
        {
            // Register helpers (toPascal, toSnake) so templates can call them.
            NamingHelpers.RegisterHelpers();
        }

        // Render a sample set of templates to the console. Useful for quick verification.
        public void RenderSampleTemplates()
        {
            // Build a minimal metadata object that matches the placeholders used in the .hbs files.
            var sampleMetadata = new
            {
                Namespace = "robot_controller_api",
                ContextName = "RobotContext",
                EntityName = "RobotCommand",
                TableName = "robotcommand",
                Entities = new[] { new { EntityName = "RobotCommand" } },
                Properties = new[]
                {
                    new { Name = "id", Type = "int", IsKey = true, IsIdentity = true },
                    new { Name = "name", Type = "string", IsKey = false, IsIdentity = false },
                    new { Name = "description", Type = "string", IsKey = false, IsIdentity = false },
                    new { Name = "ismovecommand", Type = "bool", IsKey = false, IsIdentity = false },
                    new { Name = "createddate", Type = "DateTime", IsKey = false, IsIdentity = false },
                    new { Name = "modifieddate", Type = "DateTime", IsKey = false, IsIdentity = false }
                }
            };

            // Render Entity.hbs
            var entityOutput = RenderTemplate("Entity.hbs", sampleMetadata);
            Console.WriteLine("----- Entity.cs (rendered) -----");
            Console.WriteLine(entityOutput);

            // Render Mapping.hbs
            var mappingOutput = RenderTemplate("Mapping.hbs", sampleMetadata);
            Console.WriteLine("----- Mapping.cs (rendered) -----");
            Console.WriteLine(mappingOutput);

            // Render DbContext.hbs
            var dbContextOutput = RenderTemplate("DbContext.hbs", sampleMetadata);
            Console.WriteLine("----- DbContext.cs (rendered) -----");
            Console.WriteLine(dbContextOutput);
        }

        /// <summary>
        /// Render a template by name using the provided model.
        /// </summary>
        /// <param name="templateFileName">File name of the template in the Templates folder (e.g. "Entity.hbs").</param>
        /// <param name="model">The model object passed to Handlebars.</param>
        /// <returns>Rendered string.</returns>
        public string RenderTemplate(string templateFileName, object model)
        {
            var path = Path.Combine(templateFolderPath, templateFileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Template not found: {path}");

            var templateText = File.ReadAllText(path);
            var template = Handlebars.Compile(templateText);
            var result = template(model);
            return result;
        }

        /// <summary>
        /// Helper to write rendered output to disk.
        /// </summary>
        /// <param name="outputFolder">Relative output folder.</param>
        /// <param name="fileName">File name to write.</param>
        /// <param name="content">Rendered content.</param>
        public void RenderToFile(string outputFolder, string fileName, string content)
        {
            Directory.CreateDirectory(outputFolder);
            var outPath = Path.Combine(outputFolder, fileName);
            File.WriteAllText(outPath, content);
            Console.WriteLine($"Wrote {outPath}");
        }
    }
}
