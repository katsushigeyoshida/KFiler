using CoreLib;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace KFiler
{

    //  ディレクトリ、ファイル等の種別
    public enum FileEntityType { Root, Directory, File, Non }
    public enum FileEntitySortType { Name, Ext, DateTime, Size, Attribute, Non }
    public enum FileEntitySortDirection { Ascending, Descending }

    /// <summary>
    /// ファイル構成要素クラス
    /// </summary>
    public class FileEntity
    {
        public string mPath { get; set; } = "";                         //  ファイルパス
        public long mLength { get; set; } = 0;                          //  ファイルサイズ
        public DateTime mLastWriteTime { get; set; } = DateTime.Now;    //  最終更新日
        public FileEntityType mFileEntityType { get; set; } = FileEntityType.Non;   //  ファイル種別(Root/Directory/File)
        public BitmapSource mIcon { get; set; } = new BitmapImage();    //  アイコン
        public string mName { get; set; } = "";                         //  ファイル名/フォルダ名
        public string mDateTime { get; set; } = "";                     //  更新日時(string)
        public string mSize { get; set; } = "";                         //  ファイルサイズ(string))
        public string mAttribute { get; set; } = "";                    //  属性(ACDEHORST)

        private YLib ylib = new YLib();
        private YFile yfile = new YFile();


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="type">構成要素の種別</param>
        /// <param name="path">パス</param>
        /// <param name="ext">ファイルの種類(拡張子)</param>
        public FileEntity(FileEntityType type, string path, string ext = "")
        {
            mPath = path;
            mFileEntityType = type;
            if (type == FileEntityType.Root) {
                //  Root Directory(Disk情報)
                DriveInfo driveInfo = new DriveInfo(path);
                mIcon = new BitmapImage(new Uri("..\\Icon\\HardDisk.ico", UriKind.Relative));
                mDateTime = "";
                if (driveInfo.IsReady) {
                    mName = $"{path} [{driveInfo.VolumeLabel}]";
                    mSize = $"{yfile.FileSize2Str(driveInfo.AvailableFreeSpace)} / {yfile.FileSize2Str(driveInfo.TotalSize)}";
                    mLength = driveInfo.TotalSize;
                    mAttribute = driveInfo.DriveFormat;
                } else {
                    mName = $"{path}";
                    mSize = "";
                }
            } else if (type == FileEntityType.Directory) {
                //  Directory情報
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                mLastWriteTime = directoryInfo.LastWriteTime;
                mIcon = new BitmapImage(new Uri("..\\Icon\\FolderClose.ico", UriKind.Relative)); ;
                mName = directoryInfo.Name;
                mDateTime = directoryInfo.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");
                mSize = "<Directory>";
                mAttribute = yfile.FileAttribute2Str(directoryInfo.Attributes);
            } else if (type == FileEntityType.File) {
                //  ファイル情報
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists) {
                    mLength = fileInfo.Length;
                    mLastWriteTime = fileInfo.LastWriteTime;
                    mIcon = IconManager.GetIcon(fileInfo.FullName);
                    mName = fileInfo.Name;
                    mDateTime = fileInfo.LastWriteTime.ToString();
                    mSize = fileInfo.Length.ToString("N0");
                    mAttribute = yfile.FileAttribute2Str(fileInfo.Attributes);
                }
            }
        }
    }

    /// <summary>
    /// ファイル要素リストクラス
    /// </summary>
    public class FileEntityList
    {
        public List<FileEntity> mFiles = new List<FileEntity>();    //  構成要素リスト
        public Dictionary<FileEntitySortType, FileEntitySortDirection> mSort = 
            new Dictionary<FileEntitySortType, FileEntitySortDirection>();  //  構成要素のソートの向き(昇順/降順)
        public string mTargetFile = "*.*";                          //  検索対象ファイル名(ワイルドカード)
        public string mExceptFile = "";                             //  除外ファイル名
        public string mExceptDirectory = "";                        //  除外フォルダ名
        //public FileAttributes mExceptAttr = FileAttributes.None;  //  除外属性
        public FileAttributes mExceptAttr = FileAttributes.Hidden | FileAttributes.System;  //  除外属性

        private YLib ylib = new YLib();
        private YFile yfile = new YFile();

        /// <summary>
        /// ファイル構成要素の取得
        /// </summary>
        /// <param name="path">パス</param>
        /// <param name="ext">拡張子</param>
        public void getFileEntityList(string path, string ext = "*.*")
        {
            if (!mSort.ContainsKey(FileEntitySortType.Name))
                mSort.Add(FileEntitySortType.Name, FileEntitySortDirection.Ascending);
            if (!mSort.ContainsKey(FileEntitySortType.DateTime))
                mSort.Add(FileEntitySortType.DateTime, FileEntitySortDirection.Ascending);
            if (!mSort.ContainsKey(FileEntitySortType.Size))
                mSort.Add(FileEntitySortType.Size, FileEntitySortDirection.Ascending);
            if (!mSort.ContainsKey(FileEntitySortType.Attribute))
                mSort.Add(FileEntitySortType.Attribute, FileEntitySortDirection.Ascending);

            mFiles = getDirectoryFiles(path, ext);
        }

        /// <summary>
        /// 構成要素の取得
        /// </summary>
        /// <param name="directory">ディレクトリ</param>
        /// <param name="ext">拡張子</param>
        /// <returns>構成要素リスト</returns>
        private List<FileEntity> getDirectoryFiles(string directory, string ext = "*.*")
        {
            List<FileEntity> fileInfos = new List<FileEntity>();
            if (directory == null || directory == "") {
                //  ルート情報(ディスク情報)
                DriveInfo[] driveInfos = DriveInfo.GetDrives();
                foreach (DriveInfo driveInfo in driveInfos) {
                    fileInfos.Add(new FileEntity(FileEntityType.Root, driveInfo.Name, ""));
                }
            } else {
                //  ディレクトリ情報とファイル情報
                List<string> direcries = yfile.getDirectories(directory);
                foreach (string folder in direcries) {
                    if ((File.GetAttributes(folder) & mExceptAttr) == FileAttributes.None)
                        fileInfos.Add(new FileEntity(FileEntityType.Directory, folder));
                }
                string[] files = yfile.getFiles(Path.Combine(directory, ext));
                foreach (string file in files) {
                    if ((File.GetAttributes(file) & mExceptAttr) == FileAttributes.None)
                        fileInfos.Add(new FileEntity(FileEntityType.File, file, ext));
                }
            }
            return fileInfos;
        }

        /// <summary>
        /// ソート処理
        /// </summary>
        /// <param name="sortType">ソート対象(Name/Size/DateTime/Attribute)</param>
        /// <param name="sortDirection">ソートの向き(昇順/降順)</param>
        public void sort(FileEntitySortType sortType, FileEntitySortDirection sortDirection)
        {
            if (mSort.ContainsKey(sortType))
                mSort[sortType] = sortDirection;
            else
                mSort.Add(sortType, sortDirection);
            mFiles.Sort((a, b) => compare(a, b, sortType, sortDirection));
        }

        /// <summary>
        /// ソートの比較処理
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="sortType">ソート対象(Name/Size/DateTime/Attribute)</param>
        /// <param name="sortDirection">ソートの向き(昇順/降順)</param>
        /// <returns>比較結果</returns>
        private int compare(FileEntity a, FileEntity b, FileEntitySortType sortType, FileEntitySortDirection sortDirection)
        {
            int ret = 0;
            if (sortType == FileEntitySortType.Name) {
                ret = a.mFileEntityType == b.mFileEntityType ?
                    (sortDirection == FileEntitySortDirection.Ascending ?
                        a.mName.CompareTo(b.mName) : b.mName.CompareTo(a.mName)) :
                        (a.mFileEntityType == FileEntityType.File ? 1 : -1);
            } else if (sortType == FileEntitySortType.Size) {
                ret = a.mFileEntityType == b.mFileEntityType ?
                    (int)(sortDirection == FileEntitySortDirection.Ascending ?
                        a.mLength - b.mLength : b.mLength - a.mLength) :
                        (a.mFileEntityType == FileEntityType.File ? 1 : -1);
            } else if (sortType == FileEntitySortType.DateTime) {
                ret = a.mFileEntityType == b.mFileEntityType ?
                    (sortDirection == FileEntitySortDirection.Ascending ?
                        a.mDateTime.CompareTo(b.mDateTime) : b.mDateTime.CompareTo(a.mDateTime)) :
                        (a.mFileEntityType == FileEntityType.File ? 1 : -1);
            } else if (sortType == FileEntitySortType.Attribute) {
                ret = a.mFileEntityType == b.mFileEntityType ?
                    (sortDirection == FileEntitySortDirection.Ascending ?
                        a.mAttribute.CompareTo(b.mAttribute) : b.mAttribute.CompareTo(a.mAttribute)) :
                        (a.mFileEntityType == FileEntityType.File ? 1 : -1);
            }
            return ret;
        }

        /// <summary>
        /// 検索ファイルの合計サイズ
        /// </summary>
        /// <returns></returns>
        public long getTotalSize()
        {
            long totalSize = 0;
            foreach (var file in mFiles) {
                totalSize += file.mLength;
            }
            return totalSize;
        }

        /// <summary>
        /// 選択ファイルの合計サイズ
        /// </summary>
        /// <param name="selectFileNo"></param>
        /// <returns></returns>
        public long getSelectTotalSize(List<int> selectFileNo)
        {
            long totalSize = 0;
            foreach (int no in selectFileNo)
                if (no < mFiles.Count)
                    totalSize += mFiles[no].mLength;
            return totalSize;
        }
    }
}
