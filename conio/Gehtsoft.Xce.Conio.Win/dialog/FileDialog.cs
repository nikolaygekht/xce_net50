using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Gehtsoft.Xce.Conio;
using Gehtsoft.Xce.Conio.Win;

namespace Gehtsoft.Xce.Conio.Win
{
    //+-- [title] -----------------------------------+0
    //|Path                                          |1 0
    //|+--------------------------------------------+|2 1 = height - 5 (3)
    //||                                             |3 2
    //|+--------------------------------------------+|4 3
    //|File: ________________________________________|5 4 = height - 4
    //|              < Ok > < Cancel >               |6 5 = height - 3
    //+----------------------------------------------+7
    //                                                8
    public class FileDialog : Dialog
    {
        private string mCurrentDirectory;
        private readonly string mCurrentFile;
        private readonly DialogItemListBox mDirectoryList;
        private readonly DialogItemLabel mPathLabel;
        private readonly DialogItemLabel mFileLabel;
        private readonly DialogItemSingleLineTextBox mFileEdit;
        private readonly DialogItemButton mOk;
        private readonly DialogItemButton mCancel;
        private readonly List<DialogItemListBoxString> mLocations = new List<DialogItemListBoxString>();
        private readonly DialogItemButton mLocationsButton;
        private readonly List<DialogItemButton> mBottomButtons = new List<DialogItemButton>();

        public FileDialog(string title, string defaultDirectory, string defaultFile, IColorScheme colors, int height, int width) : base(title, colors, true, height, width)
        {
            AddItem(mPathLabel = new DialogItemLabel("", 0x1000, 0, 0));
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if ((drive.DriveType == DriveType.Fixed ||
                     drive.DriveType == DriveType.Network ||
                     drive.DriveType == DriveType.Removable) && drive.IsReady)
                {
                    DialogItemListBoxString d = new DialogItemListBoxString(drive.RootDirectory.FullName, drive.RootDirectory.FullName);
                    mLocations.Add(d);
                }
            }

            AddItem(mDirectoryList = new DialogItemListBox(0x1001, 1, 0, height - 5, width - 3));
            AddItem(mFileLabel = new DialogItemLabel("File:", 0x1003, height - 4, 0));
            AddItem(mFileEdit = new DialogItemSingleLineTextBox("", 0x1004, height - 4, 5, width - 2 - 5));
            AddItem(mOk = new DialogItemButton("< &Ok >", DialogResultOK, height - 3, 0));
            AddItem(mCancel = new DialogItemButton("< &Cancel >", DialogResultCancel, height - 3, 0));
            mBottomButtons.Add(mOk);
            mBottomButtons.Add(mCancel);
            CenterButtons(mBottomButtons.ToArray());
            AddItem(mLocationsButton = new DialogItemButton("\u2193", 0x1002, 1, width - 3));

            try
            {
                if (defaultDirectory?.Length == 0 && defaultFile?.Length == 0)
                {
                    mCurrentDirectory = Path.GetFullPath(".");
                    defaultFile = "";
                    mCurrentFile = "";
                }

                if (defaultDirectory?.Length == 0 && defaultFile != "")
                {
                    FileInfo fi = new FileInfo(defaultFile);
                    mCurrentDirectory = Path.GetFullPath(fi.Directory.FullName);
                    mCurrentFile = fi.Name;
                }

                if (defaultDirectory != "")
                {
                    mCurrentDirectory = Path.GetFullPath(defaultDirectory);
                    FileInfo fi = new FileInfo(defaultFile);
                    mCurrentFile = fi.Name;
                }
            }
            catch (Exception)
            {
                mCurrentFile = "";
                mCurrentDirectory = Path.GetFullPath(".");
            }
            ChangeDirectory("*.*");
            mFileEdit.Text = mCurrentFile;
        }

        public void AddLocation(string location)
        {
            mLocations.Add(new DialogItemListBoxString(null, location));
        }

        public override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (Height < 9 || Width < 20)
                Move(Row, Column, Math.Max(Height, 9), Math.Max(Width, 20));
            else
            {
                int height, width;
                height = Height;
                width = Width;

                mPathLabel.Move(0, 0, 1, Math.Min(width - 3, mPathLabel.Title.Length));
                mLocationsButton.Move(0, width - 3, 1, 1);
                UpdatePathLabel();
                mDirectoryList.Move(1, 0, height - 5, width - 2);
                mFileLabel.Move(height - 4, 0, 1, 5);
                mFileEdit.Move(height - 4, 5, 1, width - 5 - 2);
                foreach (DialogItemButton b in mBottomButtons)
                    b.Move(height - 3, 0, 1, b.Title.Length);
                CenterButtons(mBottomButtons.ToArray());
                Invalidate();
            }
        }

        private void UpdatePathLabel()
        {
            int max = Width - 3;
            if (max <= 0)
                return;
            if (mCurrentDirectory.Length < max)
                mPathLabel.Title = mCurrentDirectory;
            else
            {
                mPathLabel.Title = "..." + mCurrentDirectory[^(max - 3)..];
            }
            Invalidate();
        }

        private sealed class FileNameComparer : IComparer<string>
        {
            public int Compare(string s1, string s2)
            {
                return string.Compare(s1, s2, true);
            }
        }

        private readonly static FileNameComparer mComparer = new FileNameComparer();

        private void ChangeDirectory(string pattern)
        {
            UpdatePathLabel();
            mDirectoryList.RemoveAllItems();
            if (Directory.Exists(mCurrentDirectory))
            {
                string[] directories = Directory.GetDirectories(mCurrentDirectory);
                Array.Sort(directories, mComparer);
                DirectoryInfo parent = Directory.GetParent(mCurrentDirectory);
                if (parent != null)
                    mDirectoryList.AddItem(new DialogItemListBoxString("[..]", ".."));

                foreach (string s in directories)
                {
                    string s1 = (new DirectoryInfo(s)).Name;
                    mDirectoryList.AddItem(new DialogItemListBoxString("[" + s1 + "]", s1));
                }

                string[] files = Directory.GetFiles(mCurrentDirectory, pattern, SearchOption.TopDirectoryOnly);
                Array.Sort(directories, mComparer);
                foreach (string s in files)
                {
                    string s1 = (new FileInfo(s)).Name;
                    mDirectoryList.AddItem(new DialogItemListBoxString(s1, s1));
                }
            }
            if (mDirectoryList.Count > 0)
                mDirectoryList.CurSel = 1;
            mFileEdit.Text = "";
        }

        private void OnOkClick()
        {
            if (mFileEdit.Text?.Length == 0)
            {
                OnItemClick(mDirectoryList);
            }
            else if (mFileEdit.Text.Contains("*") || mFileEdit.Text.Contains("?"))
            {
                ChangeDirectory(mFileEdit.Text);
                mFileEdit.Text = "";
            }
            else if (mFileEdit.Text == "..")
            {
                DirectoryInfo d = new DirectoryInfo(mCurrentDirectory).Parent;
                if (d != null)
                {
                    mCurrentDirectory = new DirectoryInfo(mCurrentDirectory).Parent.FullName;
                    ChangeDirectory("*.*");
                    mFileEdit.Text = "";
                }
            }
            else
            {
                string f = Path.Combine(mCurrentDirectory, mFileEdit.Text);
                if (Directory.Exists(f))
                {
                    mCurrentDirectory = Path.GetFullPath(f);
                    ChangeDirectory("*.*");
                    mFileEdit.Text = "";
                }
                else
                {
                    OnFileChosen();
                }
            }
        }

        private void OnDirectoryListClick()
        {
            int sel = mDirectoryList.CurSel;
            if (sel < 0 && sel >= mDirectoryList.Count)
                return;

            DialogItemListBoxString selitem = mDirectoryList[sel];
            string selName = (string)selitem.UserData;

            if (selName == "..")
            {
                mCurrentDirectory = new DirectoryInfo(mCurrentDirectory).Parent.FullName;
                ChangeDirectory("*.*");
                return;
            }

            string selPath = Path.GetFullPath(Path.Combine(mCurrentDirectory, selName));
            if (Directory.Exists(selPath))
            {
                mCurrentDirectory = selPath;
                ChangeDirectory("*.*");
            }
            else if (mFileEdit.Text?.Length == 0)
            {
                mFileEdit.Text = (string)selitem.UserData;
                OnFileChosen();
            }
            else
            {
                mFileEdit.Text = (string)selitem.UserData;
            }
        }

        private void OnLocationsButtonClick()
        {
            mPathLabel.WindowToScreen(0, 0, out int row, out int column);
            row++;
            ModalListBox list = new ModalListBox(row, column, 10, Width - 2, Colors);
            int max = Width - 2;
            if (max <= 0)
                return;
            foreach (DialogItemListBoxString s in mLocations)
            {
                string name = (string)s.UserData;
                if (name.Length >= max)
                    name = "..." + name[^(max - 3)..];
                s.Label = name;
                list.AddItem(s);
            }

            if (list.DoModal(Manager))
            {
                mCurrentDirectory = (string)mLocations[list.CurSel].UserData;
                ChangeDirectory("*.*");
            }
        }

        public override void OnItemClick(DialogItem item)
        {
            if (ReferenceEquals(item, mOk))
                OnOkClick();
            if (ReferenceEquals(item, mCancel))
                EndDialog(DialogResultCancel);
            if (ReferenceEquals(item, mDirectoryList))
                OnDirectoryListClick();
            if (ReferenceEquals(item, mLocationsButton))
                OnLocationsButtonClick();
        }

        private string mFile = "";

        public string File => mFile;

        virtual public void OnFileChosen()
        {
            string file;

            try
            {
                file = Path.GetFullPath(Path.Combine(mCurrentDirectory, mFileEdit.Text));
            }
            catch (Exception)
            {
                MessageBox.Show(this.Manager, this.Colors, "The file name is invalid", "Error", MessageBoxButtonSet.Ok);
                return;
            }

            if (CheckFileName(file))
            {
                mFile = file;
                EndDialog(DialogResultOK);
            }
        }

        virtual protected bool CheckFileName(string name)
        {
            try
            {
                _ = new FileInfo(name);
            }
            catch (Exception)
            {
                MessageBox.Show(this.Manager, this.Colors, "The file name is invalid", "Error", MessageBoxButtonSet.Ok);
            }
            return true;
        }

        protected void AddCustomButton(int id, string title)
        {
            DialogItemButton b = new DialogItemButton(title, id, Height - 3, 0);
            mBottomButtons.Add(b);
            AddItemBefore(b, mLocationsButton);
            CenterButtons(b);
        }
    }

    public class FileOpenDialog : FileDialog
    {
        [Flags]
        public enum OpenModes
        {
            None = 0,
            ExistingFilesOnly = 0x1,
            NewFilePromprt = 0x2,
        }

        private readonly OpenModes mMode;

        public FileOpenDialog(string directory, string defaultFile, OpenModes mode, IColorScheme colors) : base("Open", directory, defaultFile, colors, 20, 60)
        {
            mMode = mode;
        }

        protected override bool CheckFileName(string name)
        {
            bool rc = base.CheckFileName(name);
            if (!rc)
                return false;

            FileInfo fi = new FileInfo(name);
            if (!fi.Directory.Exists)
            {
                MessageBox.Show(this.Manager, this.Colors, "The folder chosen does not exist", "Error", MessageBoxButtonSet.Ok);
                return false;
            }

            if (!fi.Exists && ((mMode & OpenModes.ExistingFilesOnly) == OpenModes.ExistingFilesOnly))
            {
                MessageBox.Show(this.Manager, this.Colors, "The file chosen does not exist", "Error", MessageBoxButtonSet.Ok);
                return false;
            }

            if (!fi.Exists && ((mMode & OpenModes.NewFilePromprt) == OpenModes.NewFilePromprt))
            {
                MessageBoxButton answer = MessageBox.Show(this.Manager, this.Colors, "The file chosen does not exist. Create a new one?", "Error", MessageBoxButtonSet.YesNo);
                if (answer != MessageBoxButton.Yes)
                    return false;
            }
            return true;
        }
    }

    public class FileSaveDialog : FileDialog
    {
        [Flags]
        public enum SaveModes
        {
            None = 0,
            OverwritePrompt = 0x1,
        }

        private readonly SaveModes mMode;

        public FileSaveDialog(string directory, string defaultFile, SaveModes mode, IColorScheme colors)
            : base("Save", directory, defaultFile, colors, 20, 60)
        {
            mMode = mode;
        }

        protected override bool CheckFileName(string name)
        {
            bool rc = base.CheckFileName(name);
            if (!rc)
                return false;

            FileInfo fi = new FileInfo(name);
            if (!fi.Directory.Exists)
            {
                MessageBox.Show(this.Manager, this.Colors, "The folder chosen does not exist", "Error", MessageBoxButtonSet.Ok);
                return false;
            }

            if (fi.Exists && ((mMode & SaveModes.OverwritePrompt) == SaveModes.OverwritePrompt))
            {
                MessageBoxButton answer = MessageBox.Show(this.Manager, this.Colors, "The file chosen exists. Do you want overwrite it?", "Error", MessageBoxButtonSet.YesNo);
                if (answer != MessageBoxButton.Yes)
                    return false;
            }
            return true;
        }
    }
}
