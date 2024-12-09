﻿using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using System;
using Microsoft.VisualStudio.Shell;
namespace MarkdownEditor2022
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(PasteImageCommand))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class PasteImageCommand : ICommandHandler<PasteCommandArgs>
    {
        private string _lastPath;
        private ITextView _view;
        private string _fileName;
        private const string _format = "![{1}]({0})";

        public string DisplayName => GetType().Name;

        public bool ExecuteCommand(PasteCommandArgs args, CommandExecutionContext executionContext)
        {
            _view = args.TextView;
            _fileName = args.TextView.TextBuffer.GetFileName();

            return HandlePaste();
        }

        public CommandState GetCommandState(PasteCommandArgs args)
        {
            return CommandState.Available;
        }

        protected bool HandlePaste()
        {
            IDataObject data = Clipboard.GetDataObject();

            if (data == null)
            {
                return false;
            }

            string[] formats = data.GetFormats();

            if (formats == null)
            {
                return false;
            }

            // This is to check if the image is text copied from PowerPoint etc.
            bool trueBitmap = formats.Any(x => new[] { "DeviceIndependentBitmap", "PNG", "JPG", "System.Drawing.Bitmap" }.Contains(x));
            bool textFormat = formats.Any(x => new[] { "Text", "Rich Text Format" }.Contains(x));
            bool hasBitmap = data.GetDataPresent("System.Drawing.Bitmap") || data.GetDataPresent(DataFormats.FileDrop);

            if (!hasBitmap && !trueBitmap || textFormat)
            {
                return false;
            }

            string existingFile = null;

            try
            {
                if (!GetPastedFileName(data, out existingFile))
                {
                    return true;
                }

                _lastPath = Path.GetDirectoryName(existingFile);

                SaveClipboardImageToFile(data, existingFile);
                UpdateTextBuffer(existingFile, _fileName);
            }
            catch (Exception ex)
            {
                ex.Log();
            }

            return true;
        }

        private bool GetPastedFileName(IDataObject data, out string fileName)
        {
            string extension;
            fileName = "file";

            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string fullpath = ((string[])data.GetData(DataFormats.FileDrop))[0];
                fileName = Path.GetFileName(fullpath);
                extension = Path.GetExtension(fileName).TrimStart('.');
            }
            else
            {
                extension = GetMimeType((Bitmap)data.GetData("System.Drawing.Bitmap"));
            }

            SaveFileDialog dialog = new()
            {
                FileName = fileName,
                DefaultExt = "." + extension,
                Filter = extension.ToUpperInvariant() + " Files|*." + extension,
                InitialDirectory = _lastPath ?? Path.GetDirectoryName(_fileName)
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            fileName = dialog.FileName;

            return true;
        }

        private static string GetMimeType(Bitmap bitmap)
        {
            if (bitmap.RawFormat.Guid == ImageFormat.Bmp.Guid)
            {
                return "bmp";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Emf.Guid)
            {
                return "emf";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Exif.Guid)
            {
                return "exif";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Gif.Guid)
            {
                return "gif";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Icon.Guid)
            {
                return "icon";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Jpeg.Guid)
            {
                return "jpg";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Tiff.Guid)
            {
                return "tiff";
            }

            if (bitmap.RawFormat.Guid == ImageFormat.Wmf.Guid)
            {
                return "wmf";
            }
            return "png";
        }

        private void UpdateTextBuffer(string fileName, string relativeTo)
        {
            int position = _view.Caret.Position.BufferPosition.Position;
            string relative = PackageUtilities.MakeRelative(relativeTo, fileName)
.Replace("\\", "/");

            string altText = MarkdownDropHandler.ToFriendlyName(fileName);
            string image = string.Format(CultureInfo.InvariantCulture, _format, relative, altText);

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                edit.Insert(position, image);
                edit.Apply();
            }
        }

        public void SaveClipboardImageToFile(IDataObject data, string existingFile)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string original = ((string[])data.GetData(DataFormats.FileDrop))[0];

                if (File.Exists(original))
                {
                    File.Copy(original, existingFile, true);
                }
            }
            else
            {
                using (Bitmap image = (Bitmap)data.GetData("System.Drawing.Bitmap"))
                using (MemoryStream ms = new())
                {
                    image.Save(existingFile, GetImageFormat(Path.GetExtension(existingFile)));
                }
            }
        }

        public static ImageFormat GetImageFormat(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".gif" => ImageFormat.Gif,
                ".bmp" => ImageFormat.Bmp,
                ".ico" => ImageFormat.Icon,
                _ => ImageFormat.Png,
            };
        }
    }
}