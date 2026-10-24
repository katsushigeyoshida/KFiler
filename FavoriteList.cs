using CoreLib;
using System.IO;

namespace KFiler
{
    public enum FavoriteDataSortType { Title, Category, Name, Extension, Folder, DateTime, Size, Path, Application, Non }
    public enum FavoriteDataSortDirection { Ascending, Descending }

    /// <summary>
    /// お気に入りファイルデータ
    /// </summary>
    public class FavoriteItem
    {
        public string mTitle { get; set; } = "";
        public string mCategory { get; set; } = "";
        public string mName { get; set; } = "";
        public string mExtension { get; set; } = "";
        public string mFolder { get; set; } = "";
        public string mDateTime { get; set; } = "";
        public string mSize { get; set; } = "";
        public string mPath { get; set; } = "";
        public string mApplication { get; set; } = "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param name="title">タイトル</param>
        /// <param name="ctegory">分類目</param>
        public FavoriteItem(string path, string title = "", string ctegory = "")
        {
            FileInfo fileInfo = new FileInfo(path);
            mTitle = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            mCategory = "";
            mName = fileInfo.Name;
            mExtension = fileInfo.Extension.TrimStart('.');
            mFolder = fileInfo.DirectoryName;
            if (fileInfo.Exists) {
                mDateTime = fileInfo.LastWriteTime.ToString();
                mSize = fileInfo.Length.ToString("N0");
                mPath = fileInfo.FullName;
            }
        }

        /// <summary>
        /// コンストラクタ(リストファイルデータの登録)
        /// </summary>
        /// <param name="data">文字列データ</param>
        public FavoriteItem(string[] data)
        {
            mTitle     = data[0];
            mCategory  = data[1];
            mName      = data[2];
            mExtension = data[3];
            mFolder    = data[4];
            mDateTime  = data[5];
            mSize      = data[6];
            mPath      = data[7];
            mApplication = data[8];
        }

        /// <summary>
        /// 文字配列データに変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public string[] cnvStringArray()
        {
            string[] data = new string[] {
                mTitle, mCategory, mName, mExtension, mFolder, mDateTime, mSize, mPath, mApplication
            };
            return data;
        }
    }

    /// <summary>
    /// お気に入りファイルリストクラス
    /// </summary>
    public class FavoriteList
    {
        public List<FavoriteItem> mFavoriteList = new List<FavoriteItem>();
        public string mFavoriteListPath = "FavoriteList.csv";       //  リストデータ保存パス
        public string[] mDataTitle = new string[] {                 //  リストデータタイトル
            "Title", "Category", "Name", "Extension", "Folder", "DateTime", "Size", "Path", "Apprication",
        };
        public Dictionary<FavoriteDataSortType, FavoriteDataSortDirection> mSort = new Dictionary<FavoriteDataSortType, FavoriteDataSortDirection>();
        public HashSet<string> mCategoryList  = new HashSet<string>();

        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FavoriteList()
        {
            mFavoriteListPath = Path.Combine(ylib.getAppFolderPath(), mFavoriteListPath);
            loadList();
            //  ソートカラムの昇順/降順データの登録
            if (!mSort.ContainsKey(FavoriteDataSortType.Title))
                mSort.Add(FavoriteDataSortType.Title, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Category))
                mSort.Add(FavoriteDataSortType.Category, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Name))
                mSort.Add(FavoriteDataSortType.Name, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Extension))
                mSort.Add(FavoriteDataSortType.Extension, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Folder))
                mSort.Add(FavoriteDataSortType.Folder, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.DateTime))
                mSort.Add(FavoriteDataSortType.DateTime, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Size))
                mSort.Add(FavoriteDataSortType.Size, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Path))
                mSort.Add(FavoriteDataSortType.Path, FavoriteDataSortDirection.Ascending);
            if (!mSort.ContainsKey(FavoriteDataSortType.Application))
                mSort.Add(FavoriteDataSortType.Application, FavoriteDataSortDirection.Ascending);
        }

        /// <summary>
        /// 分類でフィルタリングしたリストの取得
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<FavoriteItem> getFilteringList(string category)
        {
            if (category == null || category == "全リスト")
                return mFavoriteList;
            List<FavoriteItem> filteringList = new List<FavoriteItem>();
            foreach (FavoriteItem item in  mFavoriteList) {
                List<string> categories = item.mCategory.Split(',').ToList();
                if (0 <= categories.IndexOf(category))
                    filteringList.Add(item);
            }
            return filteringList;
        }

        /// <summary>
        /// 分類リストの作成
        /// </summary>
        public void makeCategory()
        {
            mCategoryList.Clear();
            mCategoryList.Add("全リスト");
            foreach (FavoriteItem item in mFavoriteList) {
                string[] category = item.mCategory.Split(',');
                foreach (string categoryItem in category)mCategoryList.Add(categoryItem.Trim(' '));
            }
        }

        /// <summary>
        /// 分類リストの取得
        /// </summary>
        /// <returns></returns>
        public List<string> getCategoryList()
        {
            return mCategoryList.ToList();
        }

        /// <summary>
        /// ファイルを開く(アプリケーションの指定があればそれを優先)
        /// </summary>
        /// <param name="item"></param>
        public void openFile(FavoriteItem item)
        {
            if (item.mApplication != "")
                ylib.processStart(item.mApplication, item.mPath);
            else
                ylib.openUrl(item.mPath);
        }

        /// <summary>
        /// ファイルパスからデータの登録
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void setPath(string path)
        {
            if (mFavoriteList.FindIndex(x => x.mPath == path) < 0)
                mFavoriteList.Add(new FavoriteItem(path));
            makeCategory();
        }

        /// <summary>
        /// 分類の追加
        /// </summary>
        /// <param name="fileList">お気に入りパスリスト</param>
        /// <param name="category">分類名</param>
        public void addCategory(List<string> fileList, string category)
        {
            char[] trimChars = { ',', ' ' };
            foreach (string path in fileList) {
                FavoriteItem item = mFavoriteList.Find(x => x.mPath == path);
                if (item != null) {
                    List<string> categories = item.mCategory.Split(",").ToList();
                    if (0 == categories.Count) {
                        item.mCategory = category;
                    } else if (0 > categories.IndexOf(category)) {
                        item.mCategory += "," + category;
                    }
                    item.mCategory = item.mCategory.Trim(trimChars);
                }
            }
            makeCategory();
        }

        /// <summary>
        /// リストから削除
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void removeItem(string path)
        {
            int index = mFavoriteList.FindIndex(x => x.mPath == path);
            if (0 <= index)
                mFavoriteList.RemoveAt(index);
            makeCategory();
        }

        /// <summary>
        /// リストから削除
        /// </summary>
        /// <param name="fileList">ファイルリスト</param>
        public void removeItem(List<string> fileList)
        {
            foreach (string path in fileList) {
                int index = mFavoriteList.FindIndex(x => x.mPath == path);
                if (0 <= index)
                    mFavoriteList.RemoveAt(index);
            }
            makeCategory();
        }


        /// <summary>
        /// ファイルからデータの読み込み
        /// </summary>
        public void loadList()
        {
            List<string[]> dataList = yfile.loadCsvData(mFavoriteListPath, mDataTitle);
            mFavoriteList.Clear();
            for (int i = 0; i < dataList.Count; i++)
                mFavoriteList.Add(new FavoriteItem(dataList[i]));
            makeCategory();
        }

        /// <summary>
        /// リストデータをファイルに保存
        /// </summary>
        public void saveList()
        {
            List<string[]> dataList = new List<string[]>();
            for (int i = 0; i < mFavoriteList.Count; i++)
                dataList.Add(mFavoriteList[i].cnvStringArray());
            yfile.saveCsvData(mFavoriteListPath, mDataTitle, dataList);
        }


        /// <summary>
        /// ソート処理
        /// </summary>
        /// <param name="sortType">ソート対象(Title/Category/Name/Folder/DateTime/Size/Path/Application)</param>
        /// <param name="sortDirection">ソートの向き(昇順/降順)</param>
        public void sort(FavoriteDataSortType sortType, FavoriteDataSortDirection sortDirection)
        {
            if (mSort.ContainsKey(sortType))
                mSort[sortType] = sortDirection;
            else
                mSort.Add(sortType, sortDirection);
            mFavoriteList.Sort((a, b) => compare(a, b, sortType, sortDirection));
        }

        /// <summary>
        /// ソートの比較処理
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="sortType">ソート対象(Title/Category/Name/Folder/DateTime/Size/Path/Application)</param>
        /// <param name="sortDirection">ソートの向き(昇順/降順)</param>
        /// <returns>比較結果</returns>
        private int compare(FavoriteItem a, FavoriteItem b, FavoriteDataSortType sortType, FavoriteDataSortDirection sortDirection)
        {
            int ret = 0;
            if (sortType == FavoriteDataSortType.Title) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mTitle.CompareTo(b.mTitle) : b.mTitle.CompareTo(a.mTitle);
            } else if (sortType == FavoriteDataSortType.Category) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mCategory.CompareTo(b.mCategory) : b.mCategory.CompareTo(a.mCategory);
            } else if (sortType == FavoriteDataSortType.Name) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mName.CompareTo(b.mName) : b.mName.CompareTo(a.mName);
            } else if (sortType == FavoriteDataSortType.Extension) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mExtension.CompareTo(b.mExtension) : b.mExtension.CompareTo(a.mExtension);
            } else if (sortType == FavoriteDataSortType.Folder) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mFolder.CompareTo(b.mFolder) : b.mFolder.CompareTo(a.mFolder);
            } else if (sortType == FavoriteDataSortType.DateTime) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mDateTime.CompareTo(b.mDateTime) : b.mDateTime.CompareTo(a.mDateTime);
            } else if (sortType == FavoriteDataSortType.Size) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mSize.CompareTo(b.mSize) : b.mSize.CompareTo(a.mSize);
            } else if (sortType == FavoriteDataSortType.Path) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mPath.CompareTo(b.mPath) : b.mPath.CompareTo(a.mPath);
            } else if (sortType == FavoriteDataSortType.Application) {
                ret = sortDirection == FavoriteDataSortDirection.Ascending ?
                        a.mApplication.CompareTo(b.mApplication) : b.mApplication.CompareTo(a.mApplication);
            }
            return ret;
        }
    }
}
