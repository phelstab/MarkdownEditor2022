using Community.VisualStudio.Toolkit;
using MarkdownEditor2022;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

[ComVisible(true)]
[Guid(PackageGuids.EditorFactoryString)]
internal sealed class AsciidocEditorV2 : LanguageBase
{
    private DropdownBars _dropdownBars;

    public AsciidocEditorV2(object site) : base(site)
    { }

    public override string Name => Constants.LanguageName;

    public override string[] FileExtensions { get; } = new[] { Constants.FileExtensionAdoc };

    public override void SetDefaultPreferences(LanguagePreferences preferences)
    {
        preferences.EnableCodeSense = false;
        preferences.EnableMatchBraces = true;
        preferences.EnableMatchBracesAtCaret = true;
        preferences.EnableShowMatchingBrace = true;
        preferences.EnableCommenting = true;
        preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
        preferences.LineNumbers = true;
        preferences.MaxErrorMessages = 100;
        preferences.AutoOutlining = false;
        preferences.MaxRegionTime = 2000;
        preferences.InsertTabs = false;
        preferences.IndentSize = 2;
        preferences.IndentStyle = IndentingStyle.Smart;
        preferences.ShowNavigationBar = true;
        preferences.WordWrap = true;
        preferences.WordWrapGlyphs = true;
        preferences.AutoListMembers = true;
        preferences.EnableQuickInfo = true;
        preferences.ParameterInformation = true;
    }

    public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView textView)
    {
        _dropdownBars?.Dispose();
        _dropdownBars = new DropdownBars(textView, this);
        return _dropdownBars;
    }

    public override void Dispose()
    {
        _dropdownBars?.Dispose();
        _dropdownBars = null;
        base.Dispose();
    }
}
