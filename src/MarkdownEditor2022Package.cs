using Community.VisualStudio.Toolkit;
using MarkdownEditor2022;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(PackageGuids.MarkdownEditor2022String)]
[ProvideLanguageService(typeof(AsciidocEditorV2), Constants.LanguageName, 0)]
[ProvideLanguageEditorOptionPage(typeof(DialogPage), Constants.LanguageName, "", "Advanced", null, "adoc")]
[ProvideLanguageExtension(typeof(AsciidocEditorV2), Constants.FileExtensionAdoc)]
[ProvideEditorFactory(typeof(AsciidocEditorV2), 0, false)]
[ProvideEditorLogicalView(typeof(AsciidocEditorV2), VSConstants.LOGVIEWID.TextView_string)]
[ProvideEditorExtension(typeof(AsciidocEditorV2), Constants.FileExtensionAdoc, 1000)]
[ProvideFileIcon(Constants.FileExtensionAdoc, "KnownMonikers.Document")]
public sealed class MarkdownEditor2022Package : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        AsciidocEditorV2 language = new AsciidocEditorV2(this);
        RegisterEditorFactory(language);
        ((IServiceContainer)this).AddService(typeof(AsciidocEditorV2), language, true);

        await this.RegisterCommandsAsync();
    }
}
