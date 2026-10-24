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
        private FavoriteList mFavoriteList = new FavoriteList();

        private string mCurFolder = "";                     //  カレントフォルダ
        private DiffFolder mDiffFolder;                     //  フォルダ比較ダイヤログ
        private FileCopyDialog mFileCopyDialog;             //  ファイルコピーダイヤログ
        private InputBox mTextEditorDlg;                    //  テキスト表示ダイヤログ
        private ImageView mImageView;                       //  イメージビューワーダイヤログ
        private string mFolderListPath = "FileExplorerFolderList.csv";  //  お気に入りフォルダリストの保存ファイルパス
        private string mCopyFolderListPath = "CopyFolderList.csv";  //  コピー先フォルダリストの保存ファイルパス
        private string mCompressFolderListPath = "CompressFolderList.csv";  //  圧縮先フォルダリストの保存ファイルパス
        private string mHelpFilePath = "CadAppManual.pdf";  //  PDFのヘルプファイル
        private string mTerminal = "powershell.exe";    //  "cmd.exe", "powershell.exe"(powershell5.1)ls, "pwsh.exe"(powershell7)


        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            mFolderListPath = Path.Combine(ylib.getAppFolderPath(), mFolderListPath);
            mCopyFolderListPath = Path.Combine(ylib.getAppFolderPath(), mCopyFolderListPath);
            //mCompressFolderListPath = Path.Combine(ylib.getAppFolderPath(), mCompressFolderListPath);
            mHelpFilePath = Path.Combine(ylib.getAppFolderPath(), mHelpFilePath);

            WindowFormLoad();

            loadFolderList(mFolderListPath);

            rootTree();
            mFileEntityList.getFileEntityList(null, mFileFilter);
            setFileListSortDisp();
            statusDisp();

            favoriteListRedisp();
            cbSelectCategory.SelectedIndex = 0;

            stbFileList.Visibility     = Visibility.Visible;
            stbFavoriteList.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 前処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mImageView != null)
                mImageView.Close();
            if (mFileCopyDialog != null)
                mFileCopyDialog.Close();
            if (mDiffFolder != null)
                mDiffFolder.Close();
            ClearFolderWatch();
            saveFolderList(mFolderListPath);
            mFavoriteList.saveList();
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
                    mCurFolder = item.mDirectory.FullName;
                } else {
                    //  ListViewでRoot情報(Disk情報)表示
                    mCurFolder = "";
                }
                //  ListViewにファイル情報を設定
                fileListUpdate(mCurFolder);
            }
        }

        /// <summary>
        /// キー入力処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            List<string> fileList = new List<string>();
            TabItem tabItem = (TabItem)tabListView.SelectedItem;
            bool ret = false;
            if (tabItem.Name == "tabFileList") {
                //  ファイルリストキー入力処理
                ret = fileListKeyOperation(e.Key, e.KeyboardDevice.Modifiers);
            } else if (tabItem.Name == "tabFavoriteList") {
                //  お気に入りリストキー入力処理
                ret = favoriteListKeyOperation(e.Key, e.KeyboardDevice.Modifiers);
            }
            if (ret)
                return;

            //  ファイル選択無関係のキー処理
            ClearFolderWatch();
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                //  Ctrl+ ?
                switch (e.Key) {
                    case Key.E: textFileOpen(""); break;           //  Text Editor
                    case Key.V: fileDropPaste();                   //  クリップボードのファイル貼付け
                                fileListUpdate();
                                break;
                }
            } else {
                switch (e.Key) {
                    case Key.K: mkDirectory(); 
                                fileListUpdate(); 
                                break;         //  フォルダ作成ダイヤログ
                    case Key.T: if (mCurFolder != null && mCurFolder != "")
                                    Directory.SetCurrentDirectory(mCurFolder);  //  ターミナルで開く
                                ylib.processStart(mTerminal, "");
                                break;
                }
            }
            SetFolderWatch(mCurFolder);
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
            ylib.openUrl(mHelpFilePath);
        }

        /// <summary>
        /// DirectoryTreeをパスに合わせて開く
        /// </summary>
        /// <param name="path">対象のフォルダ</param>
        private void treeExpand(string path)
        {
            Title = $"[{path}]";
            if (path == null || path == "") {
                rootTree();
            } else {
                string[] dirs = path.Split(Path.DirectorySeparatorChar);
                DirectoryTree item = mDirectoryTree;
                for (int i = 0; i < dirs.Length; i++) {
                    item.IsExpanded = true;
                    bool abort = false;
                    foreach (DirectoryTree dirTree in item.Items) {
                        if (dirTree.mDirectory.Name.Trim('\\') == dirs[i]) {
                            item = dirTree;
                            item.IsSelected = true;
                            item.BringIntoView();           //  指定Itemまでスクロール
                            abort = true;
                            break;
                        }
                    }
                    if (!abort) break;
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

        /// <summary>
        /// ファイルリストとお気に入りリストとの表示タブ切換え(ツールバーの切換え)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem item = (TabItem)tabListView.SelectedItem;
            if (item.Name == "tabFileList") {
                stbFileList.Visibility = Visibility.Visible;
                stbFavoriteList.Visibility = Visibility.Collapsed;
            } else if (item.Name == "tabFavoriteList") {
                ClearFolderWatch();                                 //  フォルダ監視停止
                stbFileList.Visibility = Visibility.Collapsed;
                stbFavoriteList.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 選択されているタブ名の取得
        /// </summary>
        /// <returns>タブ名</returns>
        private string getSelctTabName()
        {
            TabItem item = (TabItem)tabListView.SelectedItem;
            return item.Name;
        }

        /// <summary>
        /// タブの切替
        /// </summary>
        /// <param name="tabName">タブ名</param>
        private void tabChange(string tabName)
        {
            if (tabName == "tabFileList")
                tabFileList.IsSelected = true;
            if (tabName == "tabFavoriteList")
                tabFavoriteList.IsSelected = true;
        }

        /// <summary>
        /// お気に入りにファイルを登録
        /// </summary>
        private void addFavoriteDocument()
        {
            List<string> fileList = new List<string>();
            if (0 < lvFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FileEntity item in lvFileList.SelectedItems) {
                    mFavoriteList.setPath(item.mPath);
                }
                favoriteListRedisp();
            }
        }
    }
}