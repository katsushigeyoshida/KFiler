using CoreLib;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KFiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double mWindowWidth;
        private double mWindowHeight;

        private DirectoryTree mDirectoryTree;
        private FileEntityList mFileEntityList = new FileEntityList();

        private string mCurFolder = "";
        private ImageView mImageView;               //  イメージビューワーダイヤログ
        private FileCopyDialog mFileCopyDialog;     //  ファイルコピーダイヤログ
        private string mFolderListPath = "FileExplorerFolderList.csv";  //  お気に入りフォルダリストの保存ファイルパス
        private string mTerminal = "pwsh.exe";      //  "cmd.exe", "powershell.exe"(powershell5.1)ls, "pwsh.exe"(powershell7)
        private string mCopyFolderListPath = "CopyFolderList.csv";  //  コピー先フォルダリストの保存ファイルパス
        private string mCompressFolderListPath = "CompressFolderList.csv";  //  圧縮先フォルダリストの保存ファイルパス


        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        public MainWindow()
        {
            InitializeComponent();

            WindowFormLoad();

            loadFolderList(mFolderListPath);

            string computerName = Environment.MachineName;
            mDirectoryTree = new DirectoryTree(null, "", computerName);
            tvComponent.Items.Add(mDirectoryTree);

            mFileEntityList.getFileEntityList(null, "*.*");
            setFileListSortDisp();
            statusDisp();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mImageView != null)
                mImageView.Close();
            saveFolderList(mFolderListPath);
            WindowFormSave();
        }

        /// <summary>
        /// Windowの状態を前回の状態にする
        /// </summary>
        private void WindowFormLoad()
        {
            mWindowWidth = Width;
            mWindowHeight = Height;

            //  前回のWindowの位置とサイズを復元する(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.Reload();
            if (Properties.Settings.Default.MainWindowWidth < 100 || Properties.Settings.Default.MainWindowHeight < 100 ||
                System.Windows.SystemParameters.WorkArea.Height < Properties.Settings.Default.MainWindowHeight) {
                Properties.Settings.Default.MainWindowWidth = mWindowWidth;
                Properties.Settings.Default.MainWindowHeight = mWindowHeight;
            } else {
                Top = Properties.Settings.Default.MainWindowTop;
                Left = Properties.Settings.Default.MainWindowLeft;
                Width = Properties.Settings.Default.MainWindowWidth;
                Height = Properties.Settings.Default.MainWindowHeight;
                //  GridのalbumListの幅は直接設定できないのでGridLength()に変換して設定
                GridLength ln = new GridLength(Properties.Settings.Default.MainWindowTreeWidth);
                directoryTree.Width = ln;
            }
        }

        /// <summary>
        /// Window状態を保存する
        /// </summary>
        private void WindowFormSave()
        {
            //  Windowの位置とサイズを保存(登録項目をPropeties.settingsに登録して使用する)
            Properties.Settings.Default.MainWindowTop = Top;
            Properties.Settings.Default.MainWindowLeft = Left;
            Properties.Settings.Default.MainWindowWidth = Width;
            Properties.Settings.Default.MainWindowHeight = Height;
            Properties.Settings.Default.MainWindowTreeWidth = directoryTree.ActualWidth;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// TreeViewのItem選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvComponent_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DirectoryTree) {
                DirectoryTree item = e.NewValue as DirectoryTree;
                if (item.mDirectory != null) {
                    //  ListViewのディレクトリ・ファイルリスト
                    System.Diagnostics.Debug.WriteLine(item.mDirectory.FullName);
                    mCurFolder = item.mDirectory.FullName;
                    Title = $"[{mCurFolder}]";
                    mFileEntityList.getFileEntityList(mCurFolder, "*.*");
                    setFileListSortDisp();
                } else {
                    //  ListViewでRoot情報(Disk情報)表示
                    mCurFolder = "";
                    mFileEntityList.getFileEntityList(null, "*.*");
                    setFileListSortDisp();
                }
                statusDisp();
            }
        }


        private void lvFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = lvFileList.SelectedIndex;
            if (0 <= index) {
                string path = mFileEntityList.mFiles[index].mPath;
                System.Diagnostics.Debug.WriteLine($"FileListSelect: {path}");
                ContextMenu menu = lvFileList.ContextMenu;
                //  メニュー表示選択
                if (Directory.Exists(path)) {
                    //  ディレクトリ選択時メニュー
                    ((MenuItem)menu.Items[8]).Visibility = Visibility.Collapsed;    //  開く
                } else {
                    //  ファイル選択時メニュー
                    ((MenuItem)menu.Items[8]).Visibility = Visibility.Visible;      //  開く
                }
                statusDisp();
            }
        }

        /// <summary>
        /// カラムヘッダーでソート
        /// https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/data/how-to-sort-data-in-a-view
        /// https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked
        /// https://maywork.net/computer/xaml-less-wpf-26-listview-sort/
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void lvFileListColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding) return;
            System.Diagnostics.Debug.WriteLine(headerClicked.Column.Header);
            string header = headerClicked.Column.Header.ToString();

            setFileListSortDisp(header);
        }

        /// <summary>
        /// ファイルリストのメニュー処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFileList_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            List<string> fileList = new List<string>();
            if (0 < lvFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FileEntity item in lvFileList.SelectedItems) {
                    fileList.Add(item.mPath);
                }
            }
            string curDir = Directory.GetCurrentDirectory();
            if (0 <= lvFileList.SelectedIndex) {
                //  ファイル選択
                int index = lvFileList.SelectedIndex;
                string path = mFileEntityList.mFiles[index].mPath;
                if (menuItem.Name.CompareTo("lvCopyMenu") == 0) {
                    //  ファイルコピーダイヤログ
                    fileCopy(fileList);
                } else if (menuItem.Name.CompareTo("lvMoveMenu") == 0) {
                    //  ファイル移動ダイヤログ
                    fileCopy(fileList, true);
                } else if (menuItem.Name.CompareTo("lvRenameMenu") == 0) {
                    //  ファイル名変更ダイヤログ
                    fileRename(fileList);
                    fileListUpdate();
                } else if (menuItem.Name.CompareTo("lvRemoveMenu") == 0) {
                    //  ファイル削除ダイヤログ
                    fileRemove(fileList);
                    fileListUpdate();
                } else if (menuItem.Name.CompareTo("lvCompressMenu") == 0) {
                    //  ファイル圧縮ダイヤログ
                    zipCompress(fileList);
                } else if (menuItem.Name.CompareTo("lvCopyPathMenu") == 0) {

                } else if (menuItem.Name.CompareTo("lvCopyNameMenu") == 0) {

                } else if (menuItem.Name.CompareTo("lvOpenMenu") == 0) {
                    //  開く
                    Directory.SetCurrentDirectory(mCurFolder);
                    ylib.openUrl(path);
                } else if (menuItem.Name.CompareTo("lvPropertyMenu") == 0) {

                }
            }
            if (menuItem.Name.CompareTo("lvMkdirMenu") == 0) {
                //  フォルダ作成
                mkDirectory();
                fileListUpdate();
            } else if (menuItem.Name.CompareTo("lvTerminalMenu") == 0) {
                //  ターミナルで開く
                Directory.SetCurrentDirectory(mCurFolder);
                ylib.processStart("pwsh.exe", "");
            }
            Directory.SetCurrentDirectory(curDir);
        }

        /// <summary>
        /// キー入力処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"WindowPrevKey: {e.Key.ToString()}");
            List<string> fileList = new List<string>();
            if (0 < lvFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FileEntity item in lvFileList.SelectedItems)
                    fileList.Add(item.mPath);
            }
            if (0 <= lvFileList.SelectedIndex) {
                //  ファイル選択
                int index = lvFileList.SelectedIndex;
                string path = mFileEntityList.mFiles[index].mPath;
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                    //  Ctrl+ ?
                    switch (e.Key) {
                        case Key.C: break;
                        case Key.D: break;
                        case Key.E: break;
                        case Key.G: break;
                        case Key.I: break;
                        case Key.M: break;
                        case Key.O: openFile(path); break;                          //  開く
                        case Key.S: break;
                        case Key.V: break;
                        case Key.X: break;
                    }
                } else {
                    switch (e.Key) {
                        case Key.C: fileCopy(fileList); break;                      //  ファイル転送ダイヤログ(コピー)
                        case Key.D: fileRemove(fileList); break;                    //  ファイル削除ダイヤログ
                        case Key.K: mkDirectory(); fileListUpdate(); break;         //  フォルダ作成ダイヤログ
                        case Key.M: fileCopy(fileList, true); break;                //  ファイル移動ダイヤログ
                        case Key.R: fileRename(fileList); fileListUpdate(); break;  //  ファイル名変更ダイヤログ
                        case Key.Enter: openFile(path); break;                     //  開く(ImageViewer/extractZip)
                    }
                }
            }
        }

        /// <summary>
        /// お気に入り選択コンボボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSelectFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = cbSelectFolder.SelectedIndex;
            if (0 <= index && cbSelectFolder.Items[index] != mCurFolder) {
                mCurFolder = cbSelectFolder.Items[index].ToString();
                treeExpand(mCurFolder);
                cbSelectFolder.Items.RemoveAt(index);
                cbSelectFolder.Items.Insert(0, mCurFolder);
                cbSelectFolder.Text = mCurFolder;
            }
        }

        /// <summary>
        /// フォルダ登録ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (mCurFolder.Length > 0) {
                int index = cbSelectFolder.Items.IndexOf(mCurFolder);
                if (0 <= index)
                    cbSelectFolder.Items.RemoveAt(index);
                cbSelectFolder.Text = mCurFolder;
                cbSelectFolder.Items.Insert(0, mCurFolder);
                cbSelectFolder.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// ListViewのItemをダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int index = lvFileList.SelectedIndex;
            if (0 <= index) {
                //  サブディレクトリに移動
                string path = mFileEntityList.mFiles[index].mPath;
                System.Diagnostics.Debug.WriteLine($"FileListSelect: {path}");
                if (Directory.Exists(path)) {
                    Title = $"[{path}]";
                    treeExpand(path);
                } else if (File.Exists(path)) {
                    //  選択ファイルを開く(ImageViewer/extractZip)
                    openFile(path);
                }
            }
        }


        /// <summary>
        /// DirectoryTreeをパスに合わせて開く
        /// </summary>
        /// <param name="path"></param>
        private void treeExpand(string path)
        {
            string[] dirs = path.Split(Path.DirectorySeparatorChar);
            DirectoryTree item = mDirectoryTree;
            for (int i = 0; i < dirs.Length; i++) {
                item.IsExpanded = true;
                bool abort = false;
                foreach (DirectoryTree dirTree in item.Items) {
                    System.Diagnostics.Debug.WriteLine($"treeExpand: {dirTree.mDirectory.Name}");
                    if (dirTree.mDirectory.Name.Trim('\\') == dirs[i]) {
                        item = dirTree;
                        item.IsSelected = true;
                        abort = true;
                        break;
                    }
                }
                if (!abort) break;
            }
        }

        /// <summary>
        /// ソートして再表示
        /// </summary>
        /// <param name="header">ソートカラム名</param>
        private void setFileListSortDisp(string header = "")
        {
            FileEntitySortType type = FileEntitySortType.Name;
            if (header == "名前") {
                type = FileEntitySortType.Name;
            } else if (header == "日時") {
                type = FileEntitySortType.DateTime;
            } else if (header == "サイズ") {
                type = FileEntitySortType.Size;
            } else if (header == "属性") {
                type = FileEntitySortType.Attribute;
            }
            if (header != "") {
                FileEntitySortDirection direct = mFileEntityList.mSort[type] == FileEntitySortDirection.Ascending ? FileEntitySortDirection.Descending : FileEntitySortDirection.Ascending;
                mFileEntityList.sort(type, direct);
            }
            //  再表示
            fileListRedisp();
        }

        /// <summary>
        /// ファイルリストの更新
        /// </summary>
        private void fileListUpdate()
        {
            mFileEntityList.getFileEntityList(mCurFolder, "*.*");
            fileListRedisp();
        }

        /// <summary>
        /// ファイルリストの再表示
        /// </summary>
        private void fileListRedisp()
        {
            lvFileList.Items.Clear();
            foreach (var fileEntity in mFileEntityList.mFiles)
                lvFileList.Items.Add(fileEntity);
        }

        /// <summary>
        /// ファイルのコピー処理()
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        /// <param name="move">移動処理</param>
        private void fileCopy(List<string> fileList, bool move = false)
        {
            string targetFolder = selectTargetFolder("コピー先フォルダの指定", mCopyFolderListPath);
            if (targetFolder == null || targetFolder == "") return;

            if (mFileCopyDialog != null) {
                mFileCopyDialog.Close();
            }
            mFileCopyDialog = new FileCopyDialog();
            mFileCopyDialog.mSrcFiles = fileList;
            mFileCopyDialog.mDestFolder = targetFolder;
            mFileCopyDialog.mSrcRemove = move;
            if (move) {
                mFileCopyDialog.mCallBackOn = true;
                mFileCopyDialog.callback = fileListUpdate;
            }
            mFileCopyDialog.Show();
        }

        /// <summary>
        /// ファイルの削除処理
        /// </summary>
        /// <param name="fileList"></param>
        private void fileRemove(List<string> fileList)
        {
            if (0 < fileList.Count) {
                FileDeleteDialog dlg = new FileDeleteDialog();
                dlg.mSrcFiles = fileList;
                dlg.ShowDialog();
            }
        }

        /// <summary>
        /// フィル名の変更
        /// </summary>
        /// <param name="fileList">ファイル名リスト</param>
        private void fileRename(List<string> fileList)
        {
            foreach (string path in fileList) {
                InputBox dlg = new InputBox();
                dlg.Title = "ディレクトリの作成";
                dlg.mCalcButtonVisible = false;
                dlg.mEditText = Path.GetFileName(path);
                if (dlg.ShowDialog() != true)
                    break;
                string newPath = Path.Combine(Path.GetDirectoryName(path), dlg.mEditText);
                if (File.Exists(newPath)) {
                    ylib.messageBox(this, "すでにファイルが存在しています。");
                } else {
                    File.Move(path, newPath);
                }
            }
        }

        /// <summary>
        /// ディレクトリの作成
        /// </summary>
        private void mkDirectory()
        {
            InputBox dlg = new InputBox();
            dlg.Title = "ディレクトリの作成";
            dlg.mCalcButtonVisible = false;
            if (dlg.ShowDialog() != true)
                return;
            string directory = Path.Combine(mCurFolder, dlg.mEditText);
            yfile.makeDir(directory);
        }

        /// <summary>
        /// ディレクトリ・7ファイルをZipファイルに圧縮
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        private void zipCompress(List<string> fileList)
        {
            string targetFolder = yfile.folderSelect("圧縮先フォルダの選択", ".");
            if (targetFolder == null || targetFolder == "") return;
            foreach (string name in fileList) {
                string srcPath = Path.Combine(mCurFolder, name);
                string zipFilePath = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(srcPath) + ".zip");
                if (File.Exists(zipFilePath)) {
                    if (ylib.messageBox(this, "出力先にファイルが存在します。\n上書きしてもいいですか?", "確認", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        yfile.zipCompress(srcPath, targetFolder, true);
                } else
                    yfile.zipCompress(srcPath, targetFolder);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="folderListPath"></param>
        /// <returns></returns>
        private string selectTargetFolder(string title, string folderListPath)
        {
            InputSelect dlg = new InputSelect();
            dlg.Title = title;
            dlg.mInputType = InputSelect.InputType.FOLDER;
            dlg.mTextList = yfile.loadListData(folderListPath);
            if (dlg.ShowDialog() != true)
                return "";
            yfile.saveListData(folderListPath, dlg.mTextList);
            return dlg.mText;
        }

        /// <summary>
        /// ファイルを開く(ImageViewer/extractZip)
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void openFile(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".jpg" ||
                Path.GetExtension(path).ToLower() == ".png") {
                //  写真表示
                string[] fileList = yfile.getFiles(Path.GetDirectoryName(path));
                if (mImageView != null)
                    mImageView.Close();
                mImageView = new ImageView();
                mImageView.mImageList = fileList.ToList();
                mImageView.mImagePath = path;
                mImageView.Show();
            } else if (Path.GetExtension(path).ToLower() == ".zip") {
                //  Zipファイルの解凍
                extractZip(path);
            } else {
                //  開く
                Directory.SetCurrentDirectory(mCurFolder);
                ylib.openUrl(path);
            }
        }

        /// <summary>
        /// フォルダリストの読込
        /// </summary>
        /// <param name="listPath"></param>
        private void loadFolderList(string listPath)
        {
            List<string> list = yfile.loadListData(listPath);
            if (list == null)
                return;
            int n = 0;
            foreach (var path in list) {
                if (n < 50 && 0 < path.Length && !cbSelectFolder.Items.Contains(path)) {
                    cbSelectFolder.Items.Add(path);
                    n++;
                }
            }
        }

        /// <summary>
        /// フォルダリストの保存
        /// </summary>
        /// <param name="listPath"></param>
        private void saveFolderList(string listPath)
        {
            List<string> folderList = new List<string>();
            foreach (string path in cbSelectFolder.Items)
                folderList.Add(path);
            yfile.saveListData(listPath, folderList);
        }

        /// <summary>
        /// Zipファイルを解凍する
        /// </summary>
        /// <param name="zipPath">Zipパス</param>
        private void extractZip(string zipPath)
        {
            string extractFolder = yfile.folderSelect("解凍先フォルダの選択", ".");
            if (extractFolder.Length > 0) {
                if (!yfile.zipExtract(zipPath, extractFolder, true)) {
                    if (yfile.mError)
                        ylib.messageBox(this, yfile.mErrorMessage, "extractZip Error");
                }
            }
        }

        /// <summary>
        /// 下部Statusバーへの情報表示
        /// </summary>
        private void statusDisp()
        {
            tbStatus.Text = "";
            if (0 < lvFileList.SelectedItems.Count) {
                long selectTotalSize = 0;
                foreach (FileEntity item in lvFileList.SelectedItems) {
                    selectTotalSize += item.mLength;
                }
                tbStatus.Text = $"選択 {yfile.FileSize2Str(selectTotalSize)}({lvFileList.SelectedItems.Count})";
            }
            if (0 < tbStatus.Text.Length)
                tbStatus.Text += " / ";
            tbStatus.Text += $"全 {yfile.FileSize2Str(mFileEntityList.getTotalSize())}({mFileEntityList.mFiles.Count})";
        }
    }
}