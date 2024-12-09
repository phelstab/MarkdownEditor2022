﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor2022
{
    [Command(PackageIds.ToggleSync)]
    internal sealed class EnablePreviewSyncCommand : BaseCommand<EnablePreviewSyncCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return base.InitializeCompletedAsync();
        }
                
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Checked = AdvancedOptions.Instance.EnableScrollSync;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            AdvancedOptions options = await AdvancedOptions.GetLiveInstanceAsync();

            options.EnableScrollSync = !options.EnableScrollSync;
            await options.SaveAsync();

            Command.Checked = !options.EnableScrollSync;
        }
    }
}
