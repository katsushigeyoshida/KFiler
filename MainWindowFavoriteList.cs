using CoreLib;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KFiler
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// お気に入りリストダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFavoriteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int index = lvFavoriteList.SelectedIndex;
            if (0 <= index) {
                openFavorite((FavoriteItem)lvFavoriteList.Items[index]);
            }
        }

        /// <summary>
        /// お気に入りリストのコンテキストメニュー選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvFavoriteList_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            List<string> fileList = new List<string>();
            List<FavoriteItem> itemList = new List<FavoriteItem>();
            if (0 < lvFavoriteList.SelectedItems.Count) {
                //  複数選択
                foreach (FavoriteItem item in lvFavoriteList.SelectedItems) {
                    fileList.Add(item.mPath);
                    itemList.Add(item);
                }
            }
            int index = lvFavoriteList.SelectedIndex;
            if (0 <= index) {
                bool ret = false;
                FavoriteItem item = (FavoriteItem)lvFavoriteList.Items[index];
                if (menuItem.Name == "lvFavoriteOpenMenu") {
                    //  開く
                    openFavorite(item);
                } else if (menuItem.Name == "lvFavoriteEditorMenu") {
                    //  テキストエディタ
                    textFileOpen(item.mPath);
                } else if (menuItem.Name == "lvFavoriteImageViewMenu") {
                    //  イメージビューワ
                    imageView(item.mPath);
                } else if (menuItem.Name == "lvFavoriteBinViewMenu") {
                    //  内部バイナリビューワ
                    binView(item.mPath);
                } else if (menuItem.Name == "lvFavoriteFolderOpenMenu") {
                    //  フォルダを開く
                    treeExpand(item.mFolder);
                } else if (menuItem.Name == "lvFavoriteAddCategoryMenu") {
                    //  分類追加
                    ret = addCategory(fileList);
                } else if (menuItem.Name == "lvFavoriteEditMenu") {
                    //  データの編集
                    ret = editFevoriteInfo(item);
                } else if (menuItem.Name == "lvFavoriteGenreCopyMenu") {
                    //  大分類のコピー
                    ret = moveFavoriteGenre(itemList);
                } else if (menuItem.Name == "lvFavoriteGenreMoveMenu") {
                    //  大分類の移動
                    ret = moveFavoriteGenre(itemList, true);
                } else if (menuItem.Name == "lvFavoriteFileCopyMenu") {
                    //  ファイルコピー
                    fileCopy(fileList, mCurFolder);
                } else if (menuItem.Name == "lvFavoriteRemoveMenu") {
                    //  削除
                    ret = mFavoriteList.removeItem(fileList);
                }
                if (ret)
                    favoriteListRedisp();
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
        private void lvFavoriteListColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding)
                return;
            string header = headerClicked.Column.Header.ToString();

            setFavoriteListSortDisp(header);
        }

        /// <summary>
        /// 大分類の選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSelectGenre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = cbSelectGenre.SelectedIndex;
            if (0 <= index) {
                mFavoriteList.chengeGenre(cbSelectGenre.Items[index].ToString());
                mFavoriteList.makeCategory();
                favoriteListRedisp();
            }
        }

        /// <summary>
        /// 大分類のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbGenreMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("cbCreateMenu") == 0) {
                //  作成
                createFavoriteGenre();
            } else if (menuItem.Name.CompareTo("cbRenameMenu") == 0) {
                //  名称変更
                renameFavoriteGenre(cbSelectGenre.Text);
            } else if (menuItem.Name.CompareTo("cbDeleteMenu") == 0) {
                //  削除
                removeFavoriteGenre(cbSelectGenre.Text);
            }
        }

        /// <summary>
        /// 分類の選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSelectCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = cbSelectCategory.SelectedIndex;
            if (0 <= index) {
                favoriteListRedisp();
            }
        }

        /// <summary>
        /// お気に入りツールバーのボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btFavoriteList_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            List<string> fileList = new List<string>();
            if (0 < lvFavoriteList.SelectedItems.Count) {
                //  複数選択
                foreach (FavoriteItem item in lvFavoriteList.SelectedItems)
                    fileList.Add(item.mPath);
            }
            int index = lvFavoriteList.SelectedIndex;
            if (0 <= index) {
                bool ret = false;
                FavoriteItem item = (FavoriteItem)lvFavoriteList.Items[index];
                string path = mFavoriteList.mFavoriteList[index].mPath;
                if (button.Name.CompareTo("btSelectCategpry") == 0) {
                    //  分類の登録
                    ret = addCategory(fileList);
                } else if (button.Name.CompareTo("btEditFavoriteItem") == 0) {
                    //  データの編集
                    ret = editFevoriteInfo((FavoriteItem)lvFavoriteList.Items[index]);
                } else if (button.Name.CompareTo("btRemoveFavoriteItem") == 0) {
                    //  データの削除
                    ret = mFavoriteList.removeItem(fileList);
                }
                if (ret)
                    favoriteListRedisp();
            }
        }

        /// <summary>
        /// お気に入りリストのキー入力処理
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifierKeys"></param>
        /// <returns></returns>
        private bool favoriteListKeyOperation(Key key, ModifierKeys modifierKeys)
        {
            List<string> fileList = new List<string>();
            List<FavoriteItem> itemList = new List<FavoriteItem>();
            TabItem tabItem = (TabItem)tabListView.SelectedItem;
            FavoriteItem favItem = null;
            //  お気に入りリスト
            if (0 < lvFavoriteList.SelectedItems.Count) {
                //  複数選択
                foreach (FavoriteItem item in lvFavoriteList.SelectedItems)
                    if (File.Exists(item.mPath) || Directory.Exists(item.mPath)) {
                        fileList.Add(item.mPath);
                        itemList.Add(item);
                    }
                favItem = (FavoriteItem)lvFavoriteList.SelectedItems[0];
            }

            if (fileList.Count == 0)
                return false;
            if (Directory.Exists(mCurFolder))
                Directory.SetCurrentDirectory(mCurFolder);
            string path = fileList[0];
            bool ret = false;
            if (modifierKeys == ModifierKeys.Control) {
                //  Ctrl+ ?
                switch (key) {
                    case Key.C: fileDropCopy(fileList); break;                  //  クリップボードにファイルコピー
                    case Key.E: textFileOpen(path); break;                      //  Text Editor
                    case Key.G: imageView(path); break;                         //  内部イメージビューワ
                    case Key.M: treeExpand(favItem.mFolder); break;             //  フォルダに移動
                    case Key.O: openFavorite(favItem); break;                   //  開く
                    case Key.P: zipCompress(fileList); break;                   //  ファイル圧縮
                    case Key.X: fileDropCopy(fileList, true); break;            //  クリップボードのファイルカット
                    default: return false;
                }
            } else {
                switch (key) {
                    case Key.C: fileCopy(fileList, mCurFolder); break;          //  ファイル転送ダイヤログ(コピー)
                    case Key.D: ret = mFavoriteList.removeItem(fileList); break;//  お気に入り削除
                    case Key.H: ret = editFevoriteInfo(favItem); break;         //  お気に入り編集
                    case Key.M: ret = moveFavoriteGenre(itemList, true); break; //  大分類への移動
                    case Key.R: moveFavoriteGenre(itemList, false); break;      //  大分類へのコピー
                    case Key.S: ret = addCategory(fileList); break;             //  分類追加
                    case Key.T:                                                 //  ターミナルで開く
                        Directory.SetCurrentDirectory(favItem.mFolder);
                        ylib.processStart(mTerminal, "");
                        break;
                    case Key.Enter: openFavorite(favItem); break;               //  開く
                    default: return false;
                }
            }
            if (ret)
                favoriteListRedisp();

            return true;
        }

        /// <summary>
        /// 大分類の作成
        /// </summary>
        private void createFavoriteGenre()
        {
            InputBox dlg = new InputBox();
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Title = "大分類";
            if (dlg.ShowDialog() == true)
                changeFavoriteGenre(dlg.mEditText);
        }

        /// <summary>
        /// 大分類の名称変更
        /// </summary>
        /// <param name="genre"></param>
        private void renameFavoriteGenre(string genre)
        {
            InputBox dlg = new InputBox();
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Title = "大分類";
            dlg.mEditText = genre;
            if (dlg.ShowDialog() == true) {
                renameFavoriteGenre(genre, dlg.mEditText);
            }
        }

        /// <summary>
        /// 大分類の削除
        /// </summary>
        /// <param name="genre">大分類</param>
        private void removeFavoriteGenre(string genre)
        {
            if (MessageBox.Show($"{genre} を削除しても良いですか?", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                mFavoriteList.removeGenre(genre);
                mFavoriteList.mFavoriteListPath = "";
                changeFavoriteGenre();
            }
        }

        /// <summary>
        /// 大分類の変更
        /// </summary>
        /// <param name="genre">大分類名</param>
        private void changeFavoriteGenre(string genre = "")
        {
            mFavoriteList.chengeGenre(genre);
            List<string> genres = mFavoriteList.getGenreList();
            cbSelectGenre.Items.Clear();
            foreach (string item in genres) {
                cbSelectGenre.Items.Add(item);
            }
            cbSelectGenre.SelectedIndex = cbSelectGenre.Items.IndexOf(genre);
        }

        /// <summary>
        /// 大分類の名称変更
        /// </summary>
        /// <param name="srcGenre"></param>
        /// <param name="destGenre"></param>
        private void renameFavoriteGenre(string srcGenre, string destGenre)
        {
            if (mFavoriteList.isGenreExists(destGenre)) {
                MessageBox.Show($"{destGenre}が存在するので変更できません");
                return;
            }
            if (mFavoriteList.renameGenre(srcGenre, destGenre)) {
                changeFavoriteGenre(destGenre);
            }
        }

        /// <summary>
        /// お気に入りデータの実行
        /// </summary>
        /// <param name="favItem"></param>
        private void openFavorite(FavoriteItem favItem)
        {
            if (Directory.Exists(favItem.mPath)) {
                treeExpand(favItem.mPath);
            } else if (File.Exists(favItem.mPath)) {
                mFavoriteList.openFile(favItem);
            } else {
                MessageBox.Show("ファイルまたはフォルダが存在しません");
            }
        }

        /// <summary>
        /// ソートして再表示
        /// </summary>
        /// <param name="header">ソートカラム名</param>
        private void setFavoriteListSortDisp(string header = "")
        {
            FavoriteDataSortType type = FavoriteDataSortType.Name;
            if (header == "タイトル") {
                type = FavoriteDataSortType.Title;
            } else if (header == "分類") {
                type = FavoriteDataSortType.Category;
            } else if (header == "日時") {
                type = FavoriteDataSortType.DateTime;
            } else if (header == "サイズ") {
                type = FavoriteDataSortType.Size;
            } else if (header == "ファイル名") {
                type = FavoriteDataSortType.Name;
            } else if (header == "フォルダ") {
                type = FavoriteDataSortType.Folder;
            }
            if (header != "") {
                FavoriteDataSortDirection direct =
                    mFavoriteList.mSort[type] == FavoriteDataSortDirection.Ascending ? 
                    FavoriteDataSortDirection.Descending : FavoriteDataSortDirection.Ascending;
                mFavoriteList.sort(type, direct);
            }
            //  再表示
            favoriteListRedisp();
        }

        /// <summary>
        /// 分類名の追加
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        private bool addCategory(List<string> fileList)
        {
            Char[] trimChar = { ',', ' ' };
            TabItem tabItem = (TabItem)tabListView.SelectedItem;
            if (tabItem.Name == "tabFavoriteList") {
                InputSelect dlg = new InputSelect();
                dlg.Title = "分類名の入力";
                dlg.mInputType = InputSelect.InputType.TEXT;
                dlg.Owner = this;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dlg.mTextList = mFavoriteList.mCategoryList.ToList().Skip(1).ToList();
                if (dlg.ShowDialog() == true) {
                    mFavoriteList.addCategory(fileList, dlg.mText.Trim(trimChar));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ファイル情報の編集
        /// </summary>
        /// <param name="item"></param>
        private bool editFevoriteInfo(FavoriteItem item)
        {
            FavoriteInfo dlg = new FavoriteInfo();
            dlg.Owner = this;
            dlg.WindowStartupLocation  = WindowStartupLocation.CenterOwner;
            dlg.mItem = item;
            dlg.mCategoryList = mFavoriteList.getCategoryList();
            return dlg.ShowDialog() == true;
        }

        /// <summary>
        /// 大分類のコピー/移動
        /// </summary>
        /// <param name="items"></param>
        /// <param name="move"></param>
        /// <returns></returns>
        private bool moveFavoriteGenre(List<FavoriteItem> items, bool move = false)
        {
            string? genre = selectGenre("大分類の" + (move ? "移動" : "コピー"));
            if (genre != null) {
                FavoriteList favoriteList = new FavoriteList(genre);
                foreach (FavoriteItem item in items) {
                    favoriteList.mFavoriteList.Add(item);
                    if (move)
                        mFavoriteList.mFavoriteList.Remove(item);
                }
                favoriteList.saveList();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 大分類の選択
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private string? selectGenre(string title ="")
        {
            SelectMenu dlg = new SelectMenu();
            dlg.Title = title == "" ? "大分類の選択" : title;
            dlg.mReadOnly = false;
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.mMenuList = mFavoriteList.getGenreList().ToArray();
            if (dlg.ShowDialog() == true)
                return dlg.mSelectItem;
            return null;
        }

        /// <summary>
        /// 大分類の項目をコンボボックスに設定
        /// </summary>
        private void setFavoriteGenre()
        {
            List<string> genres = mFavoriteList.getGenreList();
            cbSelectGenre.Items.Clear();
            foreach (string item in genres) {
                cbSelectGenre.Items.Add(item);
            }
        }

        /// <summary>
        /// お気に入りにファイルを登録
        /// </summary>
        private void addFavoriteDocument(List<string> fileList)
        {
            string? genre = selectGenre();
            if (genre != null) {
                changeFavoriteGenre(genre);
                foreach (string path in fileList) {
                    mFavoriteList.setPath(path);
                }
                favoriteListRedisp();
            }
        }

        /// <summary>
        /// お気に入りファイルリストの表示
        /// </summary>
        private void favoriteListRedisp()
        {
            string category = "";
            int index = cbSelectCategory.SelectedIndex;
            if (0 <= index)
                category = cbSelectCategory.Items[index].ToString();
            lvFavoriteList.Items.Clear();
            List<FavoriteItem> favoriteList = mFavoriteList.getFilteringList(category);
            for (int i = 0; i < favoriteList.Count; i++)
                lvFavoriteList.Items.Add(favoriteList[i]);
            mFavoriteList.makeCategory();
            cbSelectCategory.ItemsSource = mFavoriteList.getCategoryList();
        }
    }
}
