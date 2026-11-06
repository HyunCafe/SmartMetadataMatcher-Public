using System;
using System.Collections.Generic;
using System.Reflection;
using Playnite.SDK;
using Playnite.SDK.Plugins;

[assembly: AssemblyTitle("Smart Metadata Matcher")]
[assembly: AssemblyCompany("HyunCafe")]
[assembly: AssemblyProduct("Smart Metadata Matcher")]
[assembly: AssemblyDescription("Private preview: unified import review, progress and recovery inspection") ]
[assembly: AssemblyVersion("0.4.29.0")]
[assembly: AssemblyFileVersion("0.4.29.0")]

namespace SmartMetadataMatcher
{
    public sealed class SmartMetadataMatcherPlugin : GenericPlugin
    {
        private static readonly Guid PluginId = new Guid("11D77564-F56F-5BF3-9CBB-7BB0CB427537");
        public override Guid Id { get { return PluginId; } }

        public SmartMetadataMatcherPlugin(IPlayniteAPI api) : base(api) { }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            // Single entry point. The dashboard Tools menu retains access to the
            // diagnostic exporter and the standalone title-only review window.
            yield return new MainMenuItem
            {
                Description = "Open Smart Metadata Matcher...",
                MenuSection = "@Smart Metadata Matcher",
                Action = a =>
                {
                    try { new CatalogWindow(PlayniteApi, GetPluginUserDataPath()).Show(); }
                    catch (Exception ex)
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage("Smart Metadata Matcher could not open: " + ex.Message,
                            "Smart Metadata Matcher");
                    }
                }
            };
        }
    }
}
