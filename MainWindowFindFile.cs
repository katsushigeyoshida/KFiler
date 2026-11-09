using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KFiler
{
    public partial class MainWindow : Window
    {
        private int mLastFindFileNo = 0;            //  検索ファイル表示数

        /// <summary>
        /// ファイル検索ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btFileSearch_Click(object sender, RoutedEventArgs e)
        {
            if (0 < cbFindFileName.Text.Length && 0 < cbSelectFindFileFolder.Text.Length) {
                mFindFile.setData(cbSelectFindFileFolder.Text, cbFindFileName.Text);
                setFindFileNameFolder();
                findExecute();
            }
        }

        /// <summary>
        /// Grepボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btGrep_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// ファイル検索リストのダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFindFileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int index = lvFindFileList.SelectedIndex;
            if (0 <= index) {
                FindFileItem fi = (FindFileItem)lvFindFileList.Items[index];
                Directory.SetCurrentDirectory(fi.mFolder);
                ylib.openUrl(fi.mPath);
            }
        }

        /// <summary>
        /// 検索リストのキーダウン処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFindFileList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"lvFindFileList_PreviewKeyDown: {e.Key} {e.KeyboardDevice.Modifiers}");
            List<string> fileList = new List<string>();
            if (0 < lvFindFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FindFileItem item in lvFindFileList.SelectedItems) {
                    fileList.Add(item.mPath);
                }
            }
            if (0 <= lvFindFileList.SelectedIndex) {
                //  ファイル選択
                int index = lvFindFileList.SelectedIndex;
                FindFileItem fi = (FindFileItem)lvFindFileList.Items[index];
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                    //  Ctrl+ ?
                    switch (e.Key) {
                        case Key.B: binView(fi.mPath); break;               //  BinaryViewer
                        case Key.C: fileDropCopy(fileList); break;
                        case Key.E: textFileOpen(fi.mPath); break;          //  Text Editor
                        case Key.G: imageView(fi.mPath); break;             //  ImageViewer
                        case Key.M: treeExpand(fi.mFolder); break;          //  フォルダを開く
                        case Key.O:                                         //  ファイルを開く
                            Directory.SetCurrentDirectory(fi.mFolder);
                            ylib.openUrl(fi.mPath);
                            break;
                        case Key.P: zipCompress(fileList); break;           //  Zip圧縮
                        case Key.S: addFavoriteDocument(fileList); break;   //  お気に入りに登録
                        case Key.U: UnPack(fileList); break;                //  解凍
                        case Key.V: break;
                        case Key.X: fileDropCopy(fileList, true); break;    //  ファイルの切取り
                    }
                } else {
                    switch (e.Key) {
                        case Key.C: fileCopy(fileList, ""); break;          //  ファイルコピー
                        case Key.D: fileRemove(fileList); break;            //  ファイル削除
                        case Key.M: fileCopy(fileList, "", true); break;    //  ファイル移動
                        case Key.T:                                         //  ターミナルで開く
                            if (mCurFolder != null && mCurFolder != "")
                                Directory.SetCurrentDirectory(mCurFolder);
                            ylib.processStart(mTerminal, "");
                            break;
                        case Key.Enter:                                     //  開く
                            if (Directory.Exists(fi.mPath)) {
                                treeExpand(fi.mPath);
                            } else
                                openFile(fi.mPath); break;
                    }
                }
            }
        }
        /// <summary>
        /// 検索リストのキー操作
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifierKeys"></param>
        /// <returns></returns>
        private bool findFileListKeyOperation(Key key, ModifierKeys modifierKeys)
        {
            System.Diagnostics.Debug.WriteLine($"findFileListKeyOperation: {key} {modifierKeys}");
            if (cbFindFileName.IsFocused)
                return true;
            
            return true;
        }

        /// <summary>
        /// ファイル検索リストコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFindFileList_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            List<string> fileList = new List<string>();
            if (0 < lvFindFileList.SelectedItems.Count) {
                //  複数選択
                foreach (FindFileItem item in lvFindFileList.SelectedItems) {
                    fileList.Add(item.mPath);
                }
            }

            string curDir = Directory.GetCurrentDirectory();
            if (0 <= lvFindFileList.SelectedIndex) {
                //  ファイル選択
                int index = lvFindFileList.SelectedIndex;
                FindFileItem fi = (FindFileItem)lvFindFileList.Items[index];
                if (menuItem.Name.CompareTo("lvFindFileOpenMenu") == 0) {
                    //開く
                    Directory.SetCurrentDirectory(fi.mFolder);
                    ylib.openUrl(fi.mPath);
                } else if (menuItem.Name.CompareTo("lvFindFileFavoriteMenu") == 0) {
                    //  お気に入りに追加
                    addFavoriteDocument(fileList);
                } else if (menuItem.Name.CompareTo("lvFindFileEditorMenu") == 0) {
                    //  内部エディタ
                    textFileOpen(fi.mPath);
                } else if (menuItem.Name.CompareTo("lvFindFileImageViewMenu") == 0) {
                    //  イメージビューワ
                    imageView(fi.mPath);
                } else if (menuItem.Name.CompareTo("lvFindFileBinViewMenu") == 0) {
                    //  内部バイナリビューワ
                    binView(fi.mPath);
                } else if (menuItem.Name.CompareTo("lvFindFileFolderOpenMenu") == 0) {
                    // フォルダを開く
                    treeExpand(fi.mFolder);
                } else if (menuItem.Name.CompareTo("lvFindFileCopyMenu") == 0) {
                    //  ファイルコピーダイヤログ
                    fileCopy(fileList, "");
                } else if (menuItem.Name.CompareTo("lvFindFileMoveMenu") == 0) {
                    //  ファイル移動ダイヤログ
                    fileCopy(fileList, "", true);
                } else if (menuItem.Name.CompareTo("lvFindFileRemoveMenu") == 0) {
                    //  ファイル削除ダイヤログ
                    fileRemove(fileList);
                }
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
        private void lvFindFileListColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding)
                return;
            string header = headerClicked.Column.Header.ToString();

            setFindFileListSortDisp(header);
        }

        /// <summary>
        /// 検索の開始
        /// </summary>
        private async void findExecute()
        {
            mLastFindFileNo = 0;
            lvFindFileList.Items.Clear();
            await Task.Run(() => {
                mFindFile.getFindFile();
            });
        }

        /// <summary>
        /// 検索結果の表示
        /// </summary>
        public async void findFileDisp()
        {
            for (int i = mLastFindFileNo; i < mFindFile.mFindFileList.Count; i++) {
                await Dispatcher.BeginInvoke(new Action(() => {
                    lvFindFileList.Items.Add(mFindFile.mFindFileList[i]);
                    tbStatus.Text = $"{i} / {mFindFile.mFindFileList.Count}";
                }));
                ylib.DoEvents();
            }
            mLastFindFileNo = mFindFile.mFindFileList.Count;
        }

        /// <summary>
        /// 検索フォルダの設定
        /// </summary>
        /// <param name="folder">検索フォルダパス</param>
        public void setFindFileFolder(string folder)
        {
            mFindFile.addFindFileFolder(folder);
            cbSelectFindFileFolder.Items.Clear();
            foreach (var fd in mFindFile.mFindFileFolderList)
                cbSelectFindFileFolder.Items.Add(fd);
            cbSelectFindFileFolder.SelectedIndex = 0; ;
        }

        /// <summary>
        /// 検索ファイル名をコンボボックスに設定
        /// </summary>
        public void setFindFileNameFolder()
        {
            cbFindFileName.Items.Clear();
            foreach (var name in mFindFile.mFindFileNameList)
                cbFindFileName.Items.Add(name);
            cbFindFileName.SelectedIndex = 0;

            cbSelectFindFileFolder.Items.Clear();
            foreach (var folder in mFindFile.mFindFileFolderList)
                cbSelectFindFileFolder.Items.Add(folder);
            cbSelectFindFileFolder.SelectedIndex = 0;
        }

        /// <summary>
        /// ソートして再表示
        /// </summary>
        /// <param name="header">ソートカラム名</param>
        private void setFindFileListSortDisp(string header = "")
        {
            FileFindSortType type = FileFindSortType.Name;
            if (header == "ファイル名") {
                type = FileFindSortType.Name;
            } else if (header == "日時") {
                type = FileFindSortType.DateTime;
            } else if (header == "サイズ") {
                type = FileFindSortType.Size;
            } else if (header == "フォルダ") {
                type = FileFindSortType.Folder;
            }
            if (header != "") {
                FileFindSortDirection direct =
                    mFindFile.mSort[type] == FileFindSortDirection.Ascending ?
                    FileFindSortDirection.Descending : FileFindSortDirection.Ascending;
                mFindFile.sort(type, direct);
            }
            //  再表示
            mLastFindFileNo = 0;
            lvFindFileList.Items.Clear();
            findFileDisp();
        }
    }
}
