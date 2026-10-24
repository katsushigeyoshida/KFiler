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
                mFavoriteList.openFile(mFavoriteList.mFavoriteList[index]);
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
            if (0 < lvFavoriteList.SelectedItems.Count) {
                //  複数選択
                foreach (FavoriteItem item in lvFavoriteList.SelectedItems)
                    fileList.Add(item.mPath);
            }
            int index = lvFavoriteList.SelectedIndex;
            if (0 <= index) {
                FavoriteItem item = mFavoriteList.mFavoriteList[index];
                if (menuItem.Name == "lvFavoriteOpenMenu") {
                    //  開く
                    mFavoriteList.openFile(item);
                } else if (menuItem.Name == "lvAddCategoryMenu") {
                    //  分類追加
                    addCategory(fileList);
                } else if (menuItem.Name == "lvFavoriteEditMenu") {
                    //  データの編集
                    editFevoriteInfo(mFavoriteList.mFavoriteList[index]);
                } else if (menuItem.Name == "lvFavoriteRemoveMenu") {
                    //  削除
                    mFavoriteList.removeItem(item.mPath);
                    favoriteListRedisp();
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
        private void lvFavoriteListColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding) return;
            string header = headerClicked.Column.Header.ToString();

            setFavoriteListSortDisp(header);
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
        /// お気に入りリストのキー入力処理
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifierKeys"></param>
        /// <returns></returns>
        private bool favoriteListKeyOperation(Key key, ModifierKeys modifierKeys)
        {
            List<string> fileList = new List<string>();
            TabItem tabItem = (TabItem)tabListView.SelectedItem;
            FavoriteItem favItem = null;
            //  お気に入りリスト
            if (0 < lvFavoriteList.SelectedItems.Count) {
                //  複数選択
                foreach (FavoriteItem item in lvFavoriteList.SelectedItems)
                    if (File.Exists(item.mPath) == true)
                        fileList.Add(item.mPath);
                favItem = (FavoriteItem)lvFavoriteList.SelectedItems[0];
            }

            if (fileList.Count == 0)
                return false;
            if (Directory.Exists(mCurFolder))
                Directory.SetCurrentDirectory(mCurFolder);
            string path = fileList[0];
            if (modifierKeys == ModifierKeys.Control) {
                //  Ctrl+ ?
                switch (key) {
                    case Key.C: fileDropCopy(fileList); break;                  //  クリップボードにファイルコピー
                    case Key.E: textFileOpen(path); break;                      //  Text Editor
                    case Key.G: imageView(path); break;                         //  内部イメージビューワ
                    case Key.O: openFile(path); break;                          //  開く(ImageViewer/extractZip)
                    case Key.P: zipCompress(fileList); break;                   //  ファイル圧縮
                    case Key.X: fileDropCopy(fileList, true); break;            //  クリップボードのファイルカット
                    default: return false;
                }
            } else {
                switch (key) {
                    case Key.B: break;                                          //  Binary Editor
                    case Key.C: fileCopy(fileList, mCurFolder); break;          //  ファイル転送ダイヤログ(コピー)
                    case Key.D: mFavoriteList.removeItem(fileList);             //  お気に入り削除
                                favoriteListRedisp();
                                break;
                    case Key.H: editFevoriteInfo(favItem);                      //  お気に入り編集
                                favoriteListRedisp();
                                break;
                    case Key.M: fileCopy(fileList, mCurFolder, true); break;    //  ファイル移動ダイヤログ
                    case Key.S: addCategory(fileList); break;                   //  分類追加
                    case Key.T: Directory.SetCurrentDirectory(favItem.mFolder); //  ターミナルで開く
                                ylib.processStart(mTerminal, "");
                                break;
                    case Key.Enter: mFavoriteList.openFile(favItem); break;     //  開く
                    default: return false;
                }
            }

            return true;
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
                string path = mFavoriteList.mFavoriteList[index].mPath;
                if (button.Name.CompareTo("btSelectCategpry") == 0) {
                    //  分類の登録
                    addCategory(fileList);
                } else if (button.Name.CompareTo("btEditFavoriteItem") == 0) {
                    //  データの編集
                    editFevoriteInfo(mFavoriteList.mFavoriteList[index]);
                    favoriteListRedisp();
                } else if (button.Name.CompareTo("btRemoveFavoriteItem") == 0) {
                    //  データの削除
                    mFavoriteList.removeItem(fileList);
                    favoriteListRedisp();
                }
            }
        }

        /// <summary>
        /// 分類名の追加
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        private void addCategory(List<string> fileList)
        {
            TabItem tabItem = (TabItem)tabListView.SelectedItem;
            if (tabItem.Name == "tabFavoriteList") {
                InputBox dlg = new InputBox();
                dlg.Title = "分類名の入力";
                if (dlg.ShowDialog() == true) {
                    mFavoriteList.addCategory(fileList, dlg.mEditText);
                    favoriteListRedisp();
                }
            }
        }

        /// <summary>
        /// ファイル情報の編集
        /// </summary>
        /// <param name="item"></param>
        private void editFevoriteInfo(FavoriteItem item)
        {
            FavoriteInfo dlg = new FavoriteInfo();
            dlg.Owner = this;
            dlg.WindowStartupLocation  = WindowStartupLocation.CenterOwner;
            dlg.mItem = item;
            dlg.mCategoryList = mFavoriteList.getCategoryList();
            dlg.ShowDialog();
            favoriteListRedisp();
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
