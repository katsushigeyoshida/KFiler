using CoreLib;
using System.IO;
using static System.Net.WebRequestMethods;

namespace KFiler
{
    /// <summary>
    /// 検索ファイルデータ
    /// </summary>
    public class FindFileItem
    {
        public string mName {  get; set; }          //  ファイル名
        public string mDateTime { get; set; }       //  ファイル日時
        public string mSize { get; set; }           //  ファイルサイズ
        public string mFolder { get; set; }         //  ファイルフォルダ
        public string mPath { get; set; }           //  ファイルパス

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public FindFileItem(string path)
        {
            FileInfo fi = new FileInfo(path);
            mName = fi.Name;
            mDateTime = fi.LastWriteTime.ToString();
            mSize = fi.Length.ToString("N0");
            mFolder = fi.DirectoryName!;
            mPath = fi.FullName;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fi">FileInfo</param>
        public FindFileItem(FileInfo fi)
        {
            mName = fi.Name;
            mDateTime = fi.LastWriteTime.ToString();
            mSize = fi.Length.ToString("N0");
            mFolder = fi.DirectoryName!;
            mPath = fi.FullName;
        }
    }

    public enum FileFindSortType { Name, DateTime, Size, Folder, Path, Non }
    public enum FileFindSortDirection { Ascending, Descending }

    /// <summary>
    /// ファイル検索クラス
    /// </summary>
    public class FindFile
    {
        public List<FindFileItem> mFindFileList = new List<FindFileItem>(); //  検索されたファイルリスト
        public List<string> mFindFileFolderList = new List<string>();   //  検索フォルダリスト
        public List<string> mFindFileNameList = new List<string>();     //  検索ファイル名リスト
        public bool mAbort = false;                 //  中断フラグ
        public Action callback;                     //  コールバック関数

        public Dictionary<FileFindSortType, FileFindSortDirection> mSort =
            new Dictionary<FileFindSortType, FileFindSortDirection>();  //  構成要素のソートの向き(昇順/降順)
        private string mFindFileFolderListPath = "FindFileFolderListPath.csv";
        private string mFindFileNameListPath = "FindFileNameListPath.csv";

        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FindFile()
        {
            //  ソートの向きの初期設定
            if (!mSort.ContainsKey(FileFindSortType.Name))
                mSort.Add(FileFindSortType.Name, FileFindSortDirection.Ascending);
            if (!mSort.ContainsKey(FileFindSortType.DateTime))
                mSort.Add(FileFindSortType.DateTime, FileFindSortDirection.Ascending);
            if (!mSort.ContainsKey(FileFindSortType.Size))
                mSort.Add(FileFindSortType.Size, FileFindSortDirection.Ascending);
            if (!mSort.ContainsKey(FileFindSortType.Folder))
                mSort.Add(FileFindSortType.Folder, FileFindSortDirection.Ascending);
            if (!mSort.ContainsKey(FileFindSortType.Path))
                mSort.Add(FileFindSortType.Path, FileFindSortDirection.Ascending);
            //  検索フォルダ/ファイル名リストの読込
            mFindFileFolderListPath = Path.Combine(ylib.getAppFolderPath(), mFindFileFolderListPath);
            mFindFileNameListPath = Path.Combine(ylib.getAppFolderPath(), mFindFileNameListPath);
            loadFindFileFolderList();
            loadFindFileNameList();
        }

        /// <summary>
        /// ファイルの検索
        /// </summary>
        public void getFindFile()
        {
            mFindFileList.Clear();
            if (0 < mFindFileFolderList.Count && 0 < mFindFileNameList.Count) {
                getFindFile(mFindFileFolderList[0], mFindFileNameList[0]);
                callback();
            }
        }

        /// <summary>
        /// ファイルを再帰的に検索
        /// </summary>
        /// <param name="folder">検索フォルダ名</param>
        /// <param name="fileName">検索ファイル名</param>
        /// <returns></returns>
        public bool getFindFile(string folder, string fileName)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName))
                return false;
            DirectoryInfo di = new DirectoryInfo(folder);
            FileInfo[] files = di.GetFiles(fileName);
            foreach (FileInfo fi in files)
                    mFindFileList.Add(new FindFileItem(fi));
            //if (0 < files.Length)
            //    callback();
            if (mAbort)
                return false;
            DirectoryInfo[] directories =  di.GetDirectories();
            foreach (DirectoryInfo directory in directories)
                if (!getFindFile(directory.FullName, fileName))
                    return false;
            return true;
        }

        /// <summary>
        /// 検索フォルダとファイル名をリストに設定
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        public void setData(string folder, string fileName)
        {
            addFindFileFolder(folder);
            addFindFileName(fileName);
        }

        /// <summary>
        /// 検索ファイル名をリストに追加
        /// </summary>
        /// <param name="fileName"></param>
        public void addFindFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;
            if (mFindFileNameList.Contains(fileName))
                mFindFileNameList.Remove(fileName);
            mFindFileNameList.Insert(0, fileName);
        }

        /// <summary>
        /// 検索フォルダをリストに追加
        /// </summary>
        /// <param name="folder">フォルダ名</param>
        public void addFindFileFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return;
            if (mFindFileFolderList.Contains(folder))
                mFindFileFolderList.Remove(folder);
            mFindFileFolderList.Insert(0, folder);
        }
        /// <summary>
        /// ソート処理
        /// </summary>
        /// <param name="sortType">ソート対象(Name/Size/DateTime/Attribute)</param>
        /// <param name="sortDirection">ソートの向き(昇順/降順)</param>
        public void sort(FileFindSortType sortType, FileFindSortDirection sortDirection)
        {
            if (mSort.ContainsKey(sortType))
                mSort[sortType] = sortDirection;
            else
                mSort.Add(sortType, sortDirection);
            mFindFileList.Sort((a, b) => compare(a, b, sortType, sortDirection));
        }

        /// <summary>
        /// ソートの比較処理
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="sortType">ソート対象(Name/Size/DateTime/Attribute)</param>
        /// <param name="sortDirection">ソートの向き(昇順/降順)</param>
        /// <returns>比較結果</returns>
        private int compare(FindFileItem a, FindFileItem b, FileFindSortType sortType, FileFindSortDirection sortDirection)
        {
            int ret = 0;
            if (sortType == FileFindSortType.Name) {
                ret = sortDirection == FileFindSortDirection.Ascending ?
                        a.mName.CompareTo(b.mName) : b.mName.CompareTo(a.mName);
            } else if (sortType == FileFindSortType.Size) {
                ret = sortDirection == FileFindSortDirection.Ascending ?
                        a.mSize.CompareTo(b.mSize) : b.mSize.CompareTo(a.mSize);
            } else if (sortType == FileFindSortType.DateTime) {
                ret = sortDirection == FileFindSortDirection.Ascending ?
                        a.mDateTime.CompareTo(b.mDateTime) : b.mDateTime.CompareTo(a.mDateTime);
            } else if (sortType == FileFindSortType.Folder) {
                ret = sortDirection == FileFindSortDirection.Ascending ?
                        a.mFolder.CompareTo(b.mFolder) : b.mFolder.CompareTo(a.mFolder);
            }
            return ret;
        }


        /// <summary>
        /// 検索フォルダリストの読込
        /// </summary>
        public void loadFindFileFolderList()
        {
            mFindFileFolderList = yfile.loadListData(mFindFileFolderListPath);
        }

        /// <summary>
        /// 検索フォルダリストの保存
        /// </summary>
        public void saveFindFIleFolderList()
        {
            yfile.saveListData(mFindFileFolderListPath, 
                mFindFileFolderList.Take(Math.Min(mFindFileFolderList.Count, 100)).ToList());
        }

        /// <summary>
        /// 検索ファイル名リストの読込
        /// </summary>
        public void loadFindFileNameList()
        {
            mFindFileNameList = yfile.loadListData(mFindFileNameListPath);
        }

        /// <summary>
        /// 検索ファイル名リストの保存
        /// </summary>
        public void saveFindFIleNameList()
        {
            yfile.saveListData(mFindFileNameListPath,
                mFindFileNameList.Take(Math.Min(mFindFileNameList.Count, 100)).ToList());
        }
    }
}
