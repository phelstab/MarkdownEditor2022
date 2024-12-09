﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor2022
{
    [Command(PackageIds.OpenSettings)]
    internal sealed class OpenSettingsCommand : BaseCommand<OpenSettingsCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }
        
        protected override void Execute(object sender, EventArgs e)
        {
            Package.ShowOptionPage(typeof(OptionsProvider.AdvancedOptions));
        }
    }
}
