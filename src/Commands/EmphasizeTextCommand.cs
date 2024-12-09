﻿using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor2022
{
    [Command(PackageIds.MakeBold)]
    internal sealed class MakeBoldCommand : BaseCommand<MakeBoldCommand>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false; // Delegate to VisibilityConstraints defined in .vsct
            return base.InitializeCompletedAsync();
        }
        
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await Emphasizer.EmphasizeTextAsync("**");
        }
    }

    [Command(PackageIds.MakeItalic)]
    internal sealed class MakeItalicCommand : BaseCommand<MakeItalicCommand>
    {
        protected override async Task InitializeCompletedAsync()
        {
            Command.Supported = false; // Delegate to VisibilityConstraints defined in .vsct

            // Intercept the IncrementalSearch command (Ctrl+i) to hijack the keyboard shortcut
            await VS.Commands.InterceptAsync(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID.ISEARCH, () =>
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    DocumentView doc = await VS.Documents.GetActiveDocumentViewAsync();
                    if (doc?.TextBuffer != null && doc.TextBuffer.ContentType.IsOfType(Constants.LanguageName))
                    {
                        await Command.CommandID.ExecuteAsync();
                        return CommandProgression.Stop;
                    }

                    return CommandProgression.Continue;
                });
            });
        }
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await Emphasizer.EmphasizeTextAsync("*");
        }
    }

    public class Emphasizer
    {
        public static async Task EmphasizeTextAsync(string chars)
        {
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            ITextStructureNavigatorSelectorService svc = await VS.GetMefServiceAsync<ITextStructureNavigatorSelectorService>();
            ITextStructureNavigator navigator = svc.GetTextStructureNavigator(docView.TextBuffer);

            ITextUndoHistoryRegistry history = await VS.GetMefServiceAsync<ITextUndoHistoryRegistry>();
            ITextUndoHistory undo = history.RegisterHistory(docView.TextBuffer);

            using (ITextUndoTransaction transaction = undo.CreateTransaction("Emphasize text"))
            {
                foreach (SnapshotSpan span in docView.TextView.Selection.SelectedSpans.Reverse())
                {
                    int end = span.End;
                    int start = span.Start;

                    if (span.IsEmpty)
                    {
                        TextExtent word = navigator.GetExtentOfWord(span.Start);

                        if (word.IsSignificant)
                        {
                            end = word.Span.End;
                            start = word.Span.Start;
                        }
                    }

                    SnapshotSpan ss = new(span.Snapshot, Span.FromBounds(start, end));

                    docView.TextBuffer.Replace(ss, chars + ss.GetText() + chars);
                }

                transaction.Complete();
            }
        }
    }
}
