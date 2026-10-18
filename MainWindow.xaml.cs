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
        private DiffFolder mDiffFolder;             //  フォルダ比較ダイヤログ
        private string mFolderListPath = "FileExplorerFolderList.csv";  //  お気に入りフォルダリストの保存ファイルパス
        private string mTerminal = "pwsh.exe";      //  "cmd.exe", "powershell.exe"(powershell5.1)ls, "pwsh.exe"(powershell7)
        private string mCopyFolderListPath = "CopyFolderList.csv";  //  コピー先フォルダリストの保存ファイルパス
        private string mCompressFolderListPath = "CompressFolderList.csv";  //  圧縮先フォルダリストの保存ファイルパス

        private InputBox mTextEditorDlg;            //  テキスト表示ダイヤログ


        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        public MainWindow()
        {
            InitializeComponent();

            WindowFormLoad();

            loadFolderList(mFolderListPath);

            rootTree();
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
            if (mFileCopyDialog != null)
                mFileCopyDialog.Close();
            if (mDiffFolder != null)
                mDiffFolder.Close();
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

        /// <summary>
        /// ファイルリストの項目選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = lvFileList.SelectedIndex;
            if (0 <= index) {
                string path = mFileEntityList.mFiles[index].mPath;
                System.Diagnostics.Debug.WriteLine($"FileListSelect: {path}");
                //  ファイルリストのメニュー表示切替
                MenuItem item = getContextMenuHeaderIItem(lvFileList.ContextMenu, "lvOpenMenu");
                if (item != null) {
                    if (Directory.Exists(path))
                        item.Visibility = Visibility.Collapsed;
                    else
                        item.Visibility = Visibility.Visible;
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
                    //  コピー
                    fileDropCopy(fileList);
                } else if (menuItem.Name.CompareTo("lvCutMenu") == 0) {
                    //  カット
                    fileDropCopy(fileList, true);
                } else if (menuItem.Name.CompareTo("lvFileCopyMenu") == 0) {
                    //  ファイルコピーダイヤログ
                    fileCopy(fileList);
                } else if (menuItem.Name.CompareTo("lvFileMoveMenu") == 0) {
                    //  ファイル移動ダイヤログ
                    fileCopy(fileList, true);
                } else if (menuItem.Name.CompareTo("lvFileRenameMenu") == 0) {
                    //  ファイル名変更ダイヤログ
                    fileRename(fileList);
                    fileListUpdate();
                } else if (menuItem.Name.CompareTo("lvFileRemoveMenu") == 0) {
                    //  ファイル削除ダイヤログ
                    fileRemove(fileList);
                    fileListUpdate();
                } else if (menuItem.Name.CompareTo("lvFileCompressMenu") == 0) {
                    //  ファイル圧縮ダイヤログ
                    zipCompress(fileList);
                } else if (menuItem.Name.CompareTo("lvCopyPathMenu") == 0) {

                } else if (menuItem.Name.CompareTo("lvCopyNameMenu") == 0) {

                } else if (menuItem.Name.CompareTo("lvOpenMenu") == 0) {
                    //  ファイルを開く
                    Directory.SetCurrentDirectory(mCurFolder);
                    ylib.openUrl(path);
                } else if (menuItem.Name.CompareTo("lvPropertyMenu") == 0) {

                }
            }
            //  ファイル選択無関係
            if (menuItem.Name.CompareTo("lvMkdirMenu") == 0) {
                //  フォルダ作成
                mkDirectory();
                fileListUpdate();
            } else if (menuItem.Name.CompareTo("lvPasteMenu") == 0) {
                //  貼付け
                fileDropPaste();
                fileListUpdate();
            } else if (menuItem.Name.CompareTo("lvTerminalMenu") == 0) {
                //  ターミナルで開く
                Directory.SetCurrentDirectory(mCurFolder);
                ylib.processStart(mTerminal, "");
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
            int index = lvFileList.SelectedIndex;
            if (0 <= index) {
                //  ファイル選択
                string path = mFileEntityList.mFiles[index].mPath;
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                    //  Ctrl+ ?
                    switch (e.Key) {
                        case Key.B: break;                                          //  Binary Editor
                        case Key.C: fileDropCopy(fileList); break;                  //  クリップボードにファイルコピー
                        case Key.D: break;
                        case Key.E: textFileOpen(path); break;                      //  Text Editor
                        case Key.G: break;
                        case Key.I: break;
                        case Key.M: break;
                        case Key.O: openFile(path); break;                          //  ファイルを開く
                        case Key.P: zipCompress(fileList); break;                   //  ファイル圧縮
                        case Key.S: break;
                        case Key.U: fileListUpdate();  break;                       //  ファイルリストの更新
                        case Key.X: fileDropCopy(fileList, true); break;            //  クリップボードのファイルカット
                    }
                } else {
                    switch (e.Key) {
                        case Key.B: break;      //  Binary Editor
                        case Key.C: fileCopy(fileList); break;                      //  ファイル転送ダイヤログ(コピー)
                        case Key.D: fileRemove(fileList); fileListUpdate(); break;  //  ファイル削除ダイヤログ
                        case Key.E: break;                      //  Text Editor
                        case Key.M: fileCopy(fileList, true); break;                //  ファイル移動ダイヤログ
                        case Key.R: fileRename(fileList); fileListUpdate(); break;  //  ファイル名変更ダイヤログ
                        case Key.Enter: openFile(path); break;                      //  開く(ImageViewer/extractZip)
                    }
                }
            }
            //  ファイル選択無関係
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                //  Ctrl+ ?
                switch (e.Key) {
                    case Key.E: if (index < 0) textFileOpen(""); break;                      //  Text Editor
                    case Key.V: fileDropPaste(); fileListUpdate(); break;       //  クリップボードのファイル貼付け
                }
            } else {
                switch (e.Key) {
                    case Key.K: mkDirectory(); fileListUpdate(); break;         //  フォルダ作成ダイヤログ
                    case Key.T: Directory.SetCurrentDirectory(mCurFolder);      //  ターミナルで開く
                                ylib.processStart(mTerminal, "");
                                break;
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
        /// ファイル操作ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btFileOpration_Click(object sender, RoutedEventArgs e)
        {
            List<string> fileList = new List<string>();
            if (0 < lvFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FileEntity item in lvFileList.SelectedItems)
                    fileList.Add(item.mPath);
            }

            Button button = (Button)sender;
            if (button.Name.CompareTo("btCopyFile") == 0) {
                //  コピー操作(Ctrl+C)
                fileDropCopy(fileList);
            } else if (button.Name.CompareTo("btCutFile") == 0) {
                //  切取り操作(Ctrl+X)
                fileDropCopy(fileList, true);
            } else if (button.Name.CompareTo("btPasteFile") == 0) {
                //  貼付け操作(Ctrl+V)
                fileDropPaste();
                fileListUpdate();
            } else if (button.Name.CompareTo("btCompareFolder") == 0) {
                //  フォルダ比較
                diffFolder();
            } else if (button.Name.CompareTo("btFileSearch") == 0) {

            } else if (button.Name.CompareTo("btGrep") == 0) {

            } else if (button.Name.CompareTo("btUpFolder") == 0) {
                //  上位フォルダに移動
                mCurFolder = Path.GetDirectoryName(mCurFolder)!;
                treeExpand(mCurFolder);
            }
        }

        /// <summary>
        /// 設定ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btSetting_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// ヘルプボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btHelp_Click(object sender, RoutedEventArgs e)
        {

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
        /// ディレクトリツリーのルート表示
        /// </summary>
        private void rootTree()
        {
            string computerName = Environment.MachineName;
            mDirectoryTree = new DirectoryTree(null, "", computerName);
            tvComponent.Items.Clear();
            tvComponent.Items.Add(mDirectoryTree);
        }

        /// <summary>
        /// DirectoryTreeをパスに合わせて開く
        /// </summary>
        /// <param name="path"></param>
        private void treeExpand(string path)
        {
            if (path == null || path == "") {
                rootTree();
            } else {
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
        /// クリップボードにファイルリストを設定
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        /// <param name="move">移動/コピー</param>
        private void fileDropCopy(List<string> fileList, bool move = false)
        {
            ylib.setClipboard(fileList, move ? 2 : 1);
        }

        /// <summary>
        /// クリップボードのファイルの貼付け
        /// </summary>
        private void fileDropPaste()
        {
            (List<string> fileList, int copyType) = ylib.getClipboardFiles();
            fileCopy(fileList, mCurFolder, copyType == 2);
        }

        /// <summary>
        /// ファイルのコピー処理()
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        /// <param name="move">移動処理</param>
        private void fileCopy(List<string> fileList, bool move = false)
        {
            string targetFolder = selectTargetFolder("コピー先フォルダの指定", mCopyFolderListPath);
            fileCopy(fileList, targetFolder, move);
        }

        /// <summary>
        /// ファイルコピー処理
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        /// <param name="targetFolder">コピー先フォルダ</param>
        /// <param name="move">移動/コピー</param>
        private void fileCopy(List<string> fileList, string targetFolder,  bool move = false)
        {
            if (targetFolder == null || targetFolder == "" ||
                fileList == null || fileList.Count == 0) return;

            if (mFileCopyDialog != null) {
                mFileCopyDialog.Close();
            }
            mFileCopyDialog = new FileCopyDialog();
            mFileCopyDialog.mSrcFiles = fileList;
            mFileCopyDialog.mDestFolder = targetFolder;
            mFileCopyDialog.mSrcRemove = move;
            mFileCopyDialog.mCallBackOn = true;
            mFileCopyDialog.callback = fileListUpdate;
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
        /// フォルダ比較ダイヤログを開く
        /// </summary>
        private void diffFolder()
        {
            if (mDiffFolder != null)
                mDiffFolder.Close();
            mDiffFolder = new DiffFolder();
            mDiffFolder.Owner = this;
            mDiffFolder.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            mDiffFolder.Show();
        }

        /// <summary>
        /// フォルダの選択ダイヤログ
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="folderListPath">フォルダパスリスト</param>
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
        /// テキストファイル表示
        /// </summary>
        /// <param name="path"></param>
        private void textFileOpen(string path)
        {
            if (mTextEditorDlg != null)
                mTextEditorDlg.Close();
            mTextEditorDlg = new InputBox();
            //mTextEditorDlg.Topmost = true;
            mTextEditorDlg.mFontFamily = "MS Gothic";
            mTextEditorDlg.mFileEdit = true;
            mTextEditorDlg.mReadOnly = false;
            mTextEditorDlg.mFilePath = path;
            mTextEditorDlg.mMultiLine = true;
            mTextEditorDlg.mTextRapping = false;
            mTextEditorDlg.Title = $"エディタ [{Path.GetFileName(path)}]";
            mTextEditorDlg.mCalcMenu = true;
            mTextEditorDlg.mHexCalcMenu = true;
            mTextEditorDlg.mFileSelectMenu = true;
            mTextEditorDlg.Show();
        }

        private void textFileClose()
        {
            System.Diagnostics.Debug.WriteLine($"InputBox Close: {mTextEditorDlg.mFilePath}");
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
                tbStatus.Text = $"選択 {yfile.FileSize2Str(selectTotalSize)}B({lvFileList.SelectedItems.Count})";
            }
            if (0 < tbStatus.Text.Length)
                tbStatus.Text += " / ";
            tbStatus.Text += $"全 {yfile.FileSize2Str(mFileEntityList.getTotalSize())}B({mFileEntityList.mFiles.Count})";
        }

        /// <summary>
        /// コンテキストメニューからヘッダ名のアイテムを探す
        /// </summary>
        /// <param name="menu">コンテキストメニュー</param>
        /// <param name="headerName">ヘッダ名</param>
        /// <returns>メニューアイテム</returns>
        private MenuItem getContextMenuHeaderIItem(ContextMenu menu, string headerName)
        {
            for (int i = 0; i < menu.Items.Count; i++) {
                MenuItem item = (MenuItem)(menu.Items[i]);
                if (item.Name == headerName)
                    return item;
            }
            return null;
        }
    }
}