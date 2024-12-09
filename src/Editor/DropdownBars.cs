﻿using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor2022
{
    internal class DropdownBars : TypeAndMemberDropdownBars, IVsDropdownBarClient4, IDisposable
    {
        private readonly LanguageService _languageService;
        private readonly IWpfTextView _textView;
        private readonly Document _document;
        private bool _disposed;
        private bool _hasBufferChanged;
        private static readonly Regex _stripHtml = new(@"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", RegexOptions.Compiled);

        public DropdownBars(IVsTextView textView, LanguageService languageService) : base(languageService)
        {
            _languageService = languageService;
            _textView = textView.ToIWpfTextView();
            _document = _textView.TextBuffer.GetDocument();
            _document.Parsed += OnDocumentParsed;

            InitializeAsync(textView).FireAndForget();
        }

        // This moves the caret to trigger initial drop down load
        private Task InitializeAsync(IVsTextView textView)
        {
            return ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                textView.SendExplicitFocus();
                _textView.Caret.MoveToNextCaretPosition();
                _textView.Caret.PositionChanged += CaretPositionChanged;
                _textView.Caret.MoveToPreviousCaretPosition();
            }).Task;
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => SynchronizeDropdowns();
        private void OnDocumentParsed(Document document)
        {
            _hasBufferChanged = true;
            SynchronizeDropdowns();
        }

        private void SynchronizeDropdowns()
        {
            if (_document.IsParsing)
            {
                return;
            }

            _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                _languageService.SynchronizeDropdowns();
            }, VsTaskRunContext.UIThreadIdlePriority);
        }

        public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView oldView, int line, int col, ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
        {
            if (_hasBufferChanged || dropDownMembers.Count == 0)
            {
                dropDownMembers.Clear();
                IWpfTextView view = oldView.ToIWpfTextView();

                _document.Markdown.Descendants<HeadingBlock>()
                    .Select(headingBlock => CreateDropDownMember(headingBlock, oldView, view))
                    .ToList()
                    .ForEach(ddm => dropDownMembers.Add(ddm));
            }

            if (dropDownTypes.Count == 0)
            {
                string thisExt = $" {Vsix.Name} ({Vsix.Version})";
                string markdig = Path.GetFileName($"   Powered by Markdig ({Markdig.Markdown.Version})");
                dropDownTypes.Add(new DropDownMember(thisExt, new TextSpan(), 0, DROPDOWNFONTATTR.FONTATTR_GRAY));
                dropDownTypes.Add(new DropDownMember(markdig, new TextSpan(), 0, DROPDOWNFONTATTR.FONTATTR_GRAY));
            }

            DropDownMember currentDropDown = dropDownMembers
                .OfType<DropDownMember>()
                .Where(d => d.Span.iStartLine <= line)
                .LastOrDefault();

            selectedMember = dropDownMembers.IndexOf(currentDropDown);
            selectedType = 0;
            _hasBufferChanged = false;

            return true;
        }

        private static DropDownMember CreateDropDownMember(HeadingBlock headingBlock, IVsTextView oldView, IWpfTextView textView)
        {
            string headingText = textView.TextBuffer.CurrentSnapshot.GetText(headingBlock.ToSpan());

            if (headingText.Contains('\n'))
            {
                headingText = headingText.Split('\n').First();
            }

            headingText = ProcessHeadingText(headingText ?? string.Empty, headingBlock.Level, headingBlock.HeaderChar);
            headingText = _stripHtml.Replace(headingText, "");

            DROPDOWNFONTATTR fontAttr = headingBlock.Level == 1 ? DROPDOWNFONTATTR.FONTATTR_BOLD : DROPDOWNFONTATTR.FONTATTR_PLAIN;
            TextSpan textSpan = GetTextSpan(headingBlock, oldView);
            
            return new DropDownMember(headingText, textSpan, 0, fontAttr);
        }

        private static TextSpan GetTextSpan(HeadingBlock headingBlock, IVsTextView textView)
        {
            TextSpan textSpan = new();

            textView.GetLineAndColumn(headingBlock.Span.Start, out textSpan.iStartLine, out textSpan.iStartIndex);
            textView.GetLineAndColumn(headingBlock.Span.End + 1, out textSpan.iEndLine, out textSpan.iEndIndex);

            return textSpan;
        }

        /// <summary>
        /// Formats heading for dropdown presentation.
        /// Removes Markdown heading characters, and indents based on heading level.
        /// 
        /// "## Hello World" -> "     Hello World"
        /// </summary>
        private static string ProcessHeadingText(string text, int level, char headingChar)
        {
            string headingDeclaration = new(headingChar, level);

            if (text.StartsWith(headingDeclaration))
            {
                text = text.Substring(headingDeclaration.Length);
            }

            return new string(' ', (3 * level) + 2).Substring(4) + text.Trim();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _textView.Caret.PositionChanged -= CaretPositionChanged;
            _document.Parsed -= OnDocumentParsed;
        }

        public ImageMoniker GetEntryImage(int iCombo, int iIndex)
        {
            if (iCombo == 0)
            {
                return KnownMonikers.HotSpot;
            }

            return KnownMonikers.Commit;
        }
    }
}
