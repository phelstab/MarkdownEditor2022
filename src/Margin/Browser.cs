using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MarkdownEditor2022
{
    public class Browser : IDisposable
    {
        private readonly string _file;
        private readonly Document _document;
        public readonly WebView2 _browser = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0),
            Visibility = Visibility.Hidden
        };

        private const string _mappedHostName = "asciidoc-editor-host";

        public Browser(string file, Document document)
        {
            _file = file;
            _document = document;
            _browser.Initialized += BrowserInitialized;
        }

        public void Dispose()
        {
            _browser.Initialized -= BrowserInitialized;
            _browser.Dispose();
        }

        private void BrowserInitialized(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await InitializeWebView2Async();
                SetVirtualFolderMapping();
                _browser.Visibility = Visibility.Visible;
                await UpdateBrowserAsync();
            }).FireAndForget();
        }

        private async Task InitializeWebView2Async()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name);
            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, tempDir);
            await _browser.EnsureCoreWebView2Async(env);
        }

        private void SetVirtualFolderMapping()
        {
            string baseFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                _mappedHostName,
                baseFolder,
                CoreWebView2HostResourceAccessKind.Allow);
        }


        public Task RefreshAsync() => UpdateBrowserAsync();

        public async Task UpdateBrowserAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                string html = GetHtmlTemplate();
                _browser.NavigateToString(html);
            }
            catch { }
        }


        public async Task UpdatePositionAsync(int line, bool isTyping)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _browser.CoreWebView2.ExecuteScriptAsync(
                    $"document.getElementById('pragma-line-{line}')?.scrollIntoView({{behavior: 'smooth', block: 'center'}});");
            }
            catch { }
        }


        private string GetHtmlTemplate()
        {
            string content = File.ReadAllText(_file).Replace("\\", "\\\\")
                                                  .Replace("\r", "\\r")
                                                  .Replace("\n", "\\n")
                                                  .Replace("\"", "\\\"");
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <script src='http://{_mappedHostName}/margin/asciidoctor.min.js'></script>
    <style>
        body {{ margin: 0; padding: 20px; }}
        #content {{ font-family: system-ui; }}
    </style>
</head>
<body>
    <div id='content'></div>
    <script>
        window.addEventListener('load', function() {{
            if (typeof Asciidoctor === 'undefined') {{
                console.error('Asciidoctor failed to load');
                return;
            }}
            const asciidoctor = Asciidoctor();
            const content = `{content}`;
            const html = asciidoctor.convert(content, {{
                safe: 'safe',
                backend: 'html5',
                attributes: {{
                    showtitle: true,
                    icons: 'font'
                }}
            }});
            document.getElementById('content').innerHTML = html;
        }});
    </script>
</body>
</html>";
        }

    }
}
