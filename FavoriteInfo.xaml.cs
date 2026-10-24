using CoreLib;
using System.IO;
using System.Windows;

namespace KFiler
{
    /// <summary>
    /// FavoriteInfo.xaml の相互作用ロジック
    /// お気に入りデータ編集ダイヤログ
    /// </summary>
    public partial class FavoriteInfo : Window
    {
        public FavoriteItem mItem;
        public List<string> mCategoryList;
        public List<string> mApplicationList = new List<string>();
        private string mApplicationListPath = "ApplicationList.csv";

        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FavoriteInfo()
        {
            InitializeComponent();

            mApplicationListPath = Path.Combine(ylib.getAppFolderPath(), mApplicationListPath);
            loadApplicationList();
        }

        /// <summary>
        /// 前処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbTitle.Text = mItem.mTitle;
            tbCategory.Text = mItem.mCategory;
            tbExtension.Text = mItem.mExtension;
            tbDateTime.Text = mItem.mDateTime;
            tbSize.Text = mItem.mSize;
            tbPath.Text = mItem.mPath;
            cbApplication.Text = mItem.mApplication;
            FileInfo fi = new FileInfo(mItem.mPath);
            if (fi.Exists) {
                tbDateTime.Text = fi.LastWriteTime.ToString();
                tbSize.Text = fi.Length.ToString("N0");
            }
            
            cbApplication.Items.Clear();
            for (int i = 0; i < mApplicationList.Count && i < 100; i++)
                cbApplication.Items.Add(mApplicationList[i]);
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveApplicationList();
        }

        /// <summary>
        /// 分類の選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btAddCategory_Click(object sender, RoutedEventArgs e)
        {
            Char[] trimChar = { ',', ' ' };
            SelectMenu dlg = new SelectMenu();
            dlg.Title = "分類の選択";
            dlg.mMenuList = mCategoryList.ToArray();
            if (dlg.ShowDialog() == true) {
                tbCategory.Text = (tbCategory.Text + "," + dlg.mSelectItem).Trim(trimChar);
            }
        }

        /// <summary>
        /// OKボタン 登録処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btOK_Click(object sender, RoutedEventArgs e)
        {
            Char[] trimChar = { ',', ' ' };
            mItem.mTitle = tbTitle.Text;
            mItem.mCategory = tbCategory.Text;
            mItem.mDateTime = tbDateTime.Text;
            mItem.mSize = tbSize.Text;
            mItem.mName = Path.GetFileName(mItem.mPath);
            mItem.mExtension = Path.GetExtension(mItem.mPath).TrimStart('.');
            mItem.mFolder = Path.GetDirectoryName(mItem.mPath);
            mItem.mApplication = cbApplication.Text.Trim(trimChar);
            addApplicationPath(cbApplication.Text);
            Close();
        }

        /// <summary>
        /// Cancelボタン キャンセル処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// アプリケーションの選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbApplication_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            List<string[]> filters = new List<string[]> { new string[] { "EXEファイル", "*.exe" } };
            string appPath = yfile.fileOpenSelectDlg("起動アプリケーションの選択", ".", filters);
            addApplicationPath(appPath);
        }

        /// <summary>
        /// アプリケーションの登録
        /// </summary>
        /// <param name="appPath">アプリのパス</param>
        private void addApplicationPath(string appPath)
        {
            if (0 < appPath.Length) {
                if (mApplicationList.Contains(appPath))
                    mApplicationList.Remove(appPath);
                mApplicationList.Insert(0, appPath);
                cbApplication.Items.Clear();
                for (int i = 0; i < mApplicationList.Count && i < 100; i++)
                    cbApplication.Items.Add(mApplicationList[i]);
                cbApplication.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// アプリケーションリストの読み込み
        /// </summary>
        private void loadApplicationList()
        {
            mApplicationList = yfile.loadListData(mApplicationListPath);
        }
        
        /// <summary>
        /// アプリケーションリストの保存
        /// </summary>
        private void saveApplicationList()
        {
            yfile.saveListData(mApplicationListPath, mApplicationList);
        }
    }
}
