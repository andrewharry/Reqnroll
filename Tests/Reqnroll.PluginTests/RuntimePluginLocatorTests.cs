using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using Reqnroll.Infrastructure;
using Reqnroll.Plugins;
using Xunit;

namespace Reqnroll.PluginTests
{
    public class RuntimePluginLocatorTests
    {
        [Fact]
        public void LoadPlugins_Find_TestAssembly()
        {
            var testAssemblyProvider = new TestAssemblyProvider();
            var testAssembly = Assembly.GetExecutingAssembly();
            testAssemblyProvider.RegisterTestAssembly(testAssembly);

            //ARRANGE
            var runtimePluginLocator = new RuntimePluginLocator(new RuntimePluginLocationMerger(), new ReqnrollPath(), testAssemblyProvider);

            //ACT
            var plugins = runtimePluginLocator.GetAllRuntimePlugins();

            //ASSERT
            plugins.Should().Contain(testAssembly.Location);
        }

        [Fact]
        public void LoadPlugins_Find_All_Referenced_Plugins()
        {
            var testAssemblyProvider = new TestAssemblyProvider();
            var testAssembly = Assembly.GetExecutingAssembly();
            testAssemblyProvider.RegisterTestAssembly(testAssembly);

            //ARRANGE
            var runtimePluginLocator = new RuntimePluginLocator(new RuntimePluginLocationMerger(), new ReqnrollPath(), testAssemblyProvider);

            //ACT
            var plugins = runtimePluginLocator.GetAllRuntimePlugins();

            //ASSERT
            var projectReferences = GetProjectReferencesDlls();
            var allProjectReferenceFoundByPluginLocator = projectReferences.All(pr => plugins.Any(plugin => plugin.Contains(pr)));
            allProjectReferenceFoundByPluginLocator.Should().BeTrue();

            var numberOfGeneratorPlugins = NumberOfGeneratorPluginsReferenced(projectReferences);
            var generatorPluginsFound = plugins.Where(p => p.Contains("Generator")).ToList();
            generatorPluginsFound.Count.Should().Be(numberOfGeneratorPlugins);

            var numberOfRuntimePlugins = NumberOfRuntimePluginsReferenced(projectReferences);
            var runtimePluginsFound = plugins.Where(p => !p.Equals(testAssembly.Location)).Except(generatorPluginsFound).ToList();
            runtimePluginsFound.Should().HaveCount(numberOfRuntimePlugins, $"{string.Join(",", runtimePluginsFound)} were found");
        }

        private int NumberOfGeneratorPluginsReferenced(List<string> projectReferences)
        {
            return projectReferences.Count(p => p.Contains("Generator"));
        }

        private int NumberOfRuntimePluginsReferenced(List<string> projectReferences)
        {
            return projectReferences.Count(p => !p.Contains("Generator"));
        }

        private List<string> GetProjectReferencesDlls()
        {
            var projectReferences = GetProjectReferences();

            var projectDlls = new List<string>();
            foreach (string projectReference in projectReferences)
            {
                //real ref, not test dependent
                if (!projectReference.Contains("ReqnrollPlugin"))
                    continue;

                var index = projectReference.LastIndexOf("\\", StringComparison.InvariantCulture);
                var projectName = projectReference.Substring(index + 1);
                projectDlls.Add(projectName.Replace("csproj", "dll"));
            }

            return projectDlls;
        }

        private List<string> GetProjectReferences()
        {
            var dir = Environment.CurrentDirectory;
            var csprojPath = Path.Combine(dir, "..", "..", "..", "Reqnroll.PluginTests.csproj");
            var project = XDocument.Load(csprojPath);

            var projectReferences = project.Element("Project")
                                           .Elements("ItemGroup")
                                           .Elements("ProjectReference")
                                           .Attributes("Include")
                                           .Select(i => i.Value);

            // For running this test with NCrunch we need to load the assembly references as well
            // because NCrunch replaces project references with assembly references in the project file.
            var assemblyReferences = project.Element("Project")
                                            .Elements("ItemGroup")
                                            .Elements("Reference")
                                            .Attributes("Include")
                                            .Select(i => Path.GetFileNameWithoutExtension(i.Value) + ".csproj");

            return projectReferences.Concat(assemblyReferences).ToList();
        }

    }
}
