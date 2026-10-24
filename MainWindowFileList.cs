using CoreLib;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KFiler
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher mWatcher = null;     //  フォルダ監視
        private string mFileFilter = "*.*";              //  ファイル検索のフィルタ

        /// <summary>
        /// FileListのListViewの更新
        /// </summary>
        /// <param name="curFolder"></param>
        private void fileListUpdate(string curFolder)
        {
            ClearFolderWatch();                 //  フォルダ監視停止
            Title = $"[{curFolder}]";
            mFileEntityList.getFileEntityList(curFolder, mFileFilter);
            tabChange("tabFileList");
            setFileListSortDisp();
            statusDisp();
            SetFolderWatch(curFolder);          //  フォルダ監視開始
        }


        /// <summary>
        /// ファイルリストの更新
        /// </summary>
        private void fileListUpdate()
        {
            mFileEntityList.getFileEntityList(mCurFolder, mFileFilter);
            fileListRedisp();
        }

        /// <summary>
        /// ファイルリストの再表示
        /// </summary>
        private void fileListRedisp()
        {
            //  ファイル監視でスレッドの競合が起きるので非同期処理を追加
            this.Dispatcher.Invoke((Action)(() => {
                lvFileList.Items.Clear();
                foreach (var fileEntity in mFileEntityList.mFiles)
                    lvFileList.Items.Add(fileEntity);
            }));
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
                //  ファイルリストのメニュー表示切替
                MenuItem itemOpen = getContextMenuHeaderIItem(lvFileList.ContextMenu, "lvOpenMenu");
                MenuItem itemFavorite = getContextMenuHeaderIItem(lvFileList.ContextMenu, "lvAddFavoriteMenu");
                if (itemOpen != null) {
                    if (Directory.Exists(path)) {
                        itemOpen.Visibility = Visibility.Collapsed;
                        itemFavorite.Visibility = Visibility.Collapsed;
                    } else {
                        itemOpen.Visibility = Visibility.Visible;
                        itemFavorite.Visibility = Visibility.Visible;
                    }
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
        private void lvFileListColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding) return;
            string header = headerClicked.Column.Header.ToString();

            setFileListSortDisp(header);
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
                ClearFolderWatch();
                //  サブディレクトリに移動
                string path = mFileEntityList.mFiles[index].mPath;
                if (Directory.Exists(path)) {
                    treeExpand(path);
                } else if (File.Exists(path)) {
                    //  選択ファイルを開
                    openFile(path);
                    fileListUpdate();
                }
                SetFolderWatch(mCurFolder);
            }
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
            ClearFolderWatch();
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
                    fileCopy(fileList, mCurFolder);
                } else if (menuItem.Name.CompareTo("lvFileMoveMenu") == 0) {
                    //  ファイル移動ダイヤログ
                    fileCopy(fileList, mCurFolder, true);
                    fileListUpdate();
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
                    fileListUpdate();
                } else if (menuItem.Name.CompareTo("lvFileDeCompressMenu") == 0) {
                    //  ファイル解凍ダイヤログ
                    UnPack(fileList);
                    fileListUpdate();
                } else if (menuItem.Name.CompareTo("lvOpenMenu") == 0) {
                    //  ファイルを開く
                    Directory.SetCurrentDirectory(mCurFolder);
                    ylib.openUrl(path);
                } else if (menuItem.Name.CompareTo("lvPropertyMenu") == 0) {
                    //  プロパティ

                } else if (menuItem.Name.CompareTo("lvAddFavoriteMenu") == 0) {
                    //  お気に入りに追加
                    addFavoriteDocument();
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
            SetFolderWatch(mCurFolder);
            Directory.SetCurrentDirectory(curDir);
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

            ClearFolderWatch();
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
                //  ファイル検索

            } else if (button.Name.CompareTo("btGrep") == 0) {
                //  GREP

            } else if (button.Name.CompareTo("btAddDocument") == 0) {
                //  お気に入りに追加
                addFavoriteDocument();
            } else if (button.Name.CompareTo("btUpFolder") == 0) {
                //  上位フォルダに移動
                mCurFolder = Path.GetDirectoryName(mCurFolder)!;
                treeExpand(mCurFolder);
            }
            SetFolderWatch(mCurFolder);
        }

        /// <summary>
        /// ファイルリストキー入力処理
        /// </summary>
        /// <param name="key">キーコード</param>
        /// <param name="modifierKeys">制御キー</param>
        /// <returns></returns>
        private bool fileListKeyOperation(Key key, ModifierKeys modifierKeys)
        {
            List<string> fileList = new List<string>();
            //  ファイルリスト
            if (0 < lvFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FileEntity item in lvFileList.SelectedItems)
                    fileList.Add(item.mPath);
            }
            if (fileList.Count == 0)
                return false;
            if (mCurFolder != null && mCurFolder != "")
                Directory.SetCurrentDirectory(mCurFolder);
            string path = fileList[0];
            bool ret = true;
            ClearFolderWatch();
            if (modifierKeys == ModifierKeys.Control) {
                //  Ctrl+ ?
                switch (key) {
                    case Key.C: fileDropCopy(fileList); break;                  //  クリップボードにファイルコピー
                    case Key.E: textFileOpen(path); break;                      //  内部Text Editor
                    case Key.G: imageView(path); break;                         //  内部イメージビューワ
                    case Key.O: openFile(path); break;                          //  開く(extractZip)
                    case Key.P: zipCompress(fileList); break;                   //  ファイル圧縮
                    case Key.R: fileListUpdate(); break;                        //  ファイルリストの更新
                    case Key.S: addFavoriteDocument(); break;                   //  お気に入りに登録
                    case Key.U: UnPack(fileList);                               //  ファイルの解凍
                                fileListUpdate();
                                break;
                    case Key.X: fileDropCopy(fileList, true); break;            //  クリップボードのファイルカット
                    default: ret = false; break;
                }
            } else {
                switch (key) {
                    case Key.C: fileCopy(fileList, mCurFolder); break;           //  ファイル転送ダイヤログ(コピー)
                    case Key.D: fileRemove(fileList);
                                fileListUpdate();
                                break;                                          //  ファイル削除ダイヤログ
                    case Key.H: break;                                          //  (お気に入り編集)
                    case Key.M: fileCopy(fileList, mCurFolder, true); break;     //  ファイル移動ダイヤログ
                    case Key.R: fileRename(fileList);
                                fileListUpdate();
                                break;                                          //  ファイル名変更ダイヤログ
                    case Key.Enter:                                             //  開く
                        if (Directory.Exists(path)) {
                            treeExpand(path);
                        } else 
                            openFile(path); break;
                    default: ret = false; break;
                }
            }
            SetFolderWatch(mCurFolder);
            return ret;
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
            fileCopy(fileList, "", mCurFolder, copyType == 2);
        }


        /// <summary>
        /// ファイルのコピー処理()
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        /// <param name="srcFolder">コピー元フォルダ</param>
        /// <param name="move">移動処理</param>
        private void fileCopy(List<string> fileList, string srcFolder, bool move = false)
        {
            string targetFolder = selectTargetFolder("コピー先フォルダの指定", mCopyFolderListPath);
            fileCopy(fileList, srcFolder, targetFolder, move);
        }

        /// <summary>
        /// ファイルコピー処理
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        /// <param name="srcFolder">コピー元フォルダ</param>
        /// <param name="targetFolder">コピー先フォルダ</param>
        /// <param name="move">移動/コピー</param>
        private void fileCopy(List<string> fileList, string srcFolder, string targetFolder, bool move = false)
        {
            if (targetFolder == null || targetFolder == "" ||
                fileList == null || fileList.Count == 0) return;

            if (mFileCopyDialog != null)
                mFileCopyDialog.Close();
            mFileCopyDialog = new FileCopyDialog();
            mFileCopyDialog.Owner = this;
            mFileCopyDialog.mSrcFiles = fileList;
            mFileCopyDialog.mSrcFolder = srcFolder;
            mFileCopyDialog.mDestFolder = targetFolder;
            mFileCopyDialog.mSrcRemove = move;
            mFileCopyDialog.mCallBackOn = true;
            mFileCopyDialog.callback = fileListUpdate;
            mFileCopyDialog.Show();
        }

        /// <summary>
        /// ファイルの削除処理
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        private void fileRemove(List<string> fileList)
        {
            if (0 < fileList.Count) {
                FileDeleteDialog dlg = new FileDeleteDialog();
                dlg.Owner = this;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
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
                dlg.Title = "ファイル名の変更";
                dlg.Topmost = true;
                dlg.Owner = this;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dlg.mCalcButtonVisible = false;
                dlg.mEditText = Path.GetFileName(path);
                if (dlg.ShowDialog() != true)
                    break;
                string newPath = Path.Combine(Path.GetDirectoryName(path), dlg.mEditText);
                if (Directory.Exists(path)) {
                    if (Directory.Exists(newPath)) {
                        ylib.messageBox(this, "すでにフォルダが存在しています。");
                    } else {
                        Directory.Move(path, newPath);
                    }
                } else if (File.Exists(path)) {
                    if (File.Exists(newPath)) {
                        ylib.messageBox(this, "すでにファイルが存在しています。");
                    } else {
                        File.Move(path, newPath);
                    }
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
            dlg.Topmost = true;
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
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
            //dlg.Topmost = true;
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mInputType = InputSelect.InputType.FOLDER;
            dlg.mTextList = yfile.loadListData(folderListPath);
            if (dlg.ShowDialog() != true)
                return "";
            yfile.saveListData(folderListPath, dlg.mTextList);
            return dlg.mText;
        }

        /// <summary>
        /// 圧縮ファイルの解凍
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void UnPack(List<string> fileList)
        {
            string extractFolder = yfile.folderSelect("解凍先フォルダの選択", ".");
            foreach (string path in fileList) {
                if (Path.GetExtension(path).ToLower() == ".zip") {
                    //  Zipファイルの解凍
                    extractZip(path, extractFolder);
                } else if (Path.GetExtension(path).ToLower() == ".gz") {
                    //  gzファイルの解凍
                    extractGz(path, extractFolder);
                } else if (Path.GetExtension(path).ToLower() == ".tar") {
                    //  tarファイルの解凍

                }
            }
        }

        /// <summary>
        /// ファイルを開く
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void openFile(string path)
        {
            //  開く
            Directory.SetCurrentDirectory(mCurFolder);
            ylib.openUrl(path);
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
            mTextEditorDlg.Title = $"エディタ [{Path.GetFileName(path)}]";
            //mTextEditorDlg.Topmost = true;
            mTextEditorDlg.mFontFamily  = "MS Gothic";
            mTextEditorDlg.mFileEdit    = true;
            mTextEditorDlg.mReadOnly    = false;
            mTextEditorDlg.mFilePath    = path;
            mTextEditorDlg.mMultiLine   = true;
            mTextEditorDlg.mTextRapping = false;
            mTextEditorDlg.mCalcMenu       = true;
            mTextEditorDlg.mHexCalcMenu    = true;
            mTextEditorDlg.mFileSelectMenu = true;
            mTextEditorDlg.Show();
        }

        /// <summary>
        /// イメージビューワを開く
        /// </summary>
        /// <param name="path">ファイルパス</param>
        private void imageView(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".jpg" ||
                Path.GetExtension(path).ToLower() == ".png") {
                //  写真表示
                string[] fileList = yfile.getFiles(Path.GetDirectoryName(path));
                if (mImageView != null)
                    mImageView.Close();
                mImageView = new ImageView();
                mImageView.Owner = this;
                mImageView.mImageList = fileList.ToList();
                mImageView.mImagePath = path;
                mImageView.Show();
            }
        }

        /// <summary>
        /// Zipファイルを解凍する
        /// </summary>
        /// <param name="zipPath">Zipファイルパス</param>
        private void extractZip(string zipPath,string extractFolder)
        {
            if (extractFolder.Length > 0) {
                if (!yfile.zipExtract(zipPath, extractFolder, true)) {
                    if (yfile.mError)
                        ylib.messageBox(this, yfile.mErrorMessage, "extractZip Error");
                }
            }
        }

        /// <summary>
        /// gzファイルを解凍する
        /// </summary>
        /// <param name="gzPath"></param>
        private void extractGz(string gzPath, string extractFolder)
        {
            if (extractFolder.Length > 0) {
                string outPath = Path.Combine(extractFolder, Path.GetFileName(gzPath));
                outPath = outPath.Replace(".gz", "");
                if (!yfile.gzipDecompress(gzPath, outPath)) {
                    if (yfile.mError)
                        ylib.messageBox(this, yfile.mErrorMessage, "extract GZ Error");
                }
            }
        }

        /// <summary>
        /// フォルダリストの読込
        /// </summary>
        /// <param name="listPath">フォルダリストパス</param>
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
        /// フォルダ監視開始
        /// </summary>
        /// <param name="path">監視フォルダ</param>
        private void SetFolderWatch(string path)
        {
            if (mWatcher != null || path == "")
                return;

            Debug.WriteLine("SetFolderWatch : " + path);
            mWatcher = new System.IO.FileSystemWatcher();
            mWatcher.Path = path;            //監視するディレクトリを指定
            //最終アクセス日時、最終更新日時、ファイル、フォルダ名の変更を監視する
            mWatcher.NotifyFilter =
                (
                System.IO.NotifyFilters.LastAccess |
                System.IO.NotifyFilters.LastWrite |
                System.IO.NotifyFilters.FileName |
                System.IO.NotifyFilters.DirectoryName
                );
            mWatcher.Filter = "";            //すべてのファイルを監視
            mWatcher.IncludeSubdirectories = false;
            //m_Watcher.SynchronizingObject = this;   //UIのスレッドにマーシャリングするコンソールアプリケーションでの使用では必要ない

            //イベントハンドラの追加
            mWatcher.Changed += new System.IO.FileSystemEventHandler(watcher_Changed);
            mWatcher.Created += new System.IO.FileSystemEventHandler(watcher_Changed);
            mWatcher.Deleted += new System.IO.FileSystemEventHandler(watcher_Changed);
            mWatcher.Renamed += new System.IO.RenamedEventHandler(watcher_Changed);

            //監視を開始する
            mWatcher.EnableRaisingEvents = true;
            //  Console.WriteLine("監視を開始しました。");
        }

        /// <summary>
        /// フォルダ変更があったとき
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void watcher_Changed(System.Object source, System.IO.FileSystemEventArgs e)
        {
            Debug.WriteLine("watcher_Changed : " + e.ChangeType.ToString());
            fileListUpdate();
            Debug.WriteLine("watcher_Changed : End");
            //switch (e.ChangeType) {
            //    case System.IO.WatcherChangeTypes.Changed:
            //        Console.WriteLine("ファイル 「" + e.FullPath + "」が変更されました。");
            //        break;
            //    case System.IO.WatcherChangeTypes.Created:
            //        Console.WriteLine("ファイル 「" + e.FullPath + "」が作成されました。");
            //        break;
            //    case System.IO.WatcherChangeTypes.Deleted:
            //        Console.WriteLine("ファイル 「" + e.FullPath + "」が削除されました。");
            //        break;
            //}
        }

        /// <summary>
        /// フォルダ監視停止
        /// </summary>
        private void ClearFolderWatch()
        {
            Debug.WriteLine("ClearFolderWatch");
            //監視を終了
            if (mWatcher != null) {
                mWatcher.EnableRaisingEvents = false;
                mWatcher.Dispose();
                mWatcher = null;
            }
        }
    }
}
