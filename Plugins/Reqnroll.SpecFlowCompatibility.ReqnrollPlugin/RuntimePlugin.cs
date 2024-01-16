﻿using Reqnroll.Configuration;
using Reqnroll.Plugins;
using Reqnroll.SpecFlowCompatibility.ReqnrollPlugin;
using Reqnroll.UnitTestProvider;
using System.Configuration;

[assembly: RuntimePlugin(typeof(RuntimePlugin))]

namespace Reqnroll.SpecFlowCompatibility.ReqnrollPlugin;
public class RuntimePlugin : IRuntimePlugin
{
    public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
    {
        runtimePluginEvents.ConfigurationDefaults += (_, args) =>
        {
            // if it was loaded already from JSON, we skip
            if (args.ReqnrollConfiguration.ConfigSource != ConfigSource.Default)
                return;

            var loader = new AppConfigConfigurationLoader();
            if (loader.HasAppConfig)
            {
                var configSection = ConfigurationManager.GetSection("specFlow") as ConfigurationSectionHandler;
                loader.LoadAppConfig(args.ReqnrollConfiguration, configSection);
            }
        };
    }
}
