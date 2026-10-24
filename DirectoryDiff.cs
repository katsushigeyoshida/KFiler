using CoreLib;
using System.IO;

namespace KFiler
{
    /// <summary>
    /// フォルダの比較用データ
    /// </summary>
    public class FilesData
    {
        public string mRelPath;
        public FileInfo mSrcFile;
        public FileInfo mDstFile;
        public ulong mSrcCrc;
        public ulong mDstCrc;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="relPath">相対パス</param>
        /// <param name="srcFile">比較元ファイル情報</param>
        /// <param name="dstFile">比較先ファイル情報</param>
        public FilesData(string relPath, FileInfo srcFile, FileInfo dstFile)
        {
            mRelPath = relPath;
            mSrcFile = srcFile;
            mDstFile = dstFile;
        }
    }

    /// <summary>
    /// ファイル比較表示用データクラス
    /// </summary>
    public class DiffFile
    {
        public string mFileName { get; set; }       //  ファイル名
        public string mRelPath { get; set; }        //  相対パス
        public DateTime mSrcLastDate { get; set; }  //  比較元ファイル日付
        public long mSrcSize { get; set; }          //  比較元ファイルサイズ
        public ulong mSrcCrc { get; set; }          //  比較元ファイルハッシュ
        public DateTime mDstLastDate { get; set; }  //  比較先ファイル日付
        public long mDstSize { get; set; }          //  比較先ファイルサイズ
        public ulong mDstCrc { get; set; }          //  比較先ファイルハッシュ

        /// <summary>
        /// データ設定
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="relPath">相対パス</param>
        /// <param name="srcLastDate">比較元ファイル日付</param>
        /// <param name="srcSize">比較元ファイルサイズ</param>
        /// <param name="srcCrc">比較元ファイルハッシュ</param>
        /// <param name="dstLastDate">比較先ファイル日付</param>
        /// <param name="dstSize">比較先ファイルサイズ</param>
        /// <param name="dstCrc">比較先ファイルハッシュ</param>
        public DiffFile(string fileName, string relPath, DateTime srcLastDate, int srcSize, ulong srcCrc, DateTime dstLastDate, int dstSize, ulong dstCrc)
        {
            mFileName = fileName;
            mRelPath = relPath;
            mSrcLastDate = srcLastDate;
            mSrcSize = srcSize;
            mSrcCrc = srcCrc;
            mDstLastDate = dstLastDate;
            mDstSize = dstSize;
            mDstCrc = dstCrc;
        }

        /// <summary>
        /// FilesDataを設定
        /// </summary>
        /// <param name="fileData">FilesData</param>
        public DiffFile(FilesData fileData)
        {
            mFileName = Path.GetFileName(fileData.mRelPath);
            mRelPath = Path.GetDirectoryName(fileData.mRelPath);
            mSrcLastDate = fileData.mSrcFile == null ? new DateTime() : fileData.mSrcFile.LastWriteTime;
            mSrcSize = fileData.mSrcFile == null ? 0 : fileData.mSrcFile.Length;
            mSrcCrc = fileData.mSrcCrc;
            mDstLastDate = fileData.mDstFile == null ? new DateTime() : fileData.mDstFile.LastWriteTime;
            mDstSize = fileData.mDstFile == null ? 0 : fileData.mDstFile.Length;
            mDstCrc = fileData.mDstCrc;
        }

        /// <summary>
        /// パス名の取得
        /// </summary>
        /// <returns>ファイルパス</returns>
        public string getPath()
        {
            return Path.Combine(mRelPath, mFileName);
        }

        /// <summary>
        /// フォルダを指定してパス名の取得
        /// </summary>
        /// <param name="folder">フォルダ名</param>
        /// <returns>ファイルパス</returns>
        public string getPath(string folder)
        {
            return Path.Combine(folder, Path.Combine(mRelPath, mFileName));
        }
    }

    /// <summary>
    /// ディレクトリ比較
    /// </summary>
    public class DirectoryDiff
    {
        private List<FileInfo> mSrcFiles = new List<FileInfo>();
        private List<FileInfo> mDestFiles = new List<FileInfo>();
        private string mSrcDir;
        private string mDestDir;
        public List<FilesData> mFiles = new List<FilesData>();
        public bool mHashChk = true;
        private CoreLib.HashCode mHash = new CoreLib.HashCode(CoreLib.HashCode.HashType.CRC32L);
        public bool mCallBackOn = false;            //  コールバック有り
        public Action callback;                     //  コールバック関数

        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DirectoryDiff()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// 比較元と比較先のファイルリストの登録
        /// </summary>
        /// <param name="srcDir"></param>
        /// <param name="destDir"></param>
        public DirectoryDiff(string srcDir, string destDir, bool hashChk = true, string targetFile = "", string exceptFile = "", string exceptDir = "")
        {
            char[] trimChar = { '\\' };
            mSrcDir = srcDir.TrimEnd(trimChar);
            mDestDir = destDir.TrimEnd(trimChar);
            mSrcFiles = getDirectories(mSrcDir, targetFile, exceptFile, exceptDir);
            mDestFiles = getDirectories(mDestDir, targetFile, exceptFile, exceptDir);
            mHashChk = hashChk;
        }

        /// <summary>
        /// 比較処理の実行
        /// </summary>
        public void execute() {
            foreach (FileInfo srcFile in mSrcFiles) {
                mFiles.Add(new FilesData(srcFile.FullName.Substring(mSrcDir.Length + 1), srcFile, null));
                if (mHashChk)
                    mFiles[mFiles.Count - 1].mSrcCrc = mHash.GetCrc32L(srcFile.FullName);
                if (mCallBackOn)
                    callback();
            }
            foreach (FileInfo destFile in mDestFiles) {
                string destRelPath = destFile.FullName.Substring(mDestDir.Length + 1);
                int n = mFiles.FindIndex(x => x.mRelPath.ToLower() == destRelPath.ToLower());
                if (0 <= n) {
                    mFiles[n].mDstFile = destFile;
                    if (mHashChk)
                        mFiles[n].mDstCrc = mHash.GetCrc32L(destFile.FullName);
                } else {
                    mFiles.Add(new FilesData(destRelPath, null, destFile));
                    if (mHashChk)
                        mFiles[mFiles.Count - 1].mDstCrc = mHash.GetCrc32L(destFile.FullName);
                }
                if (mCallBackOn)
                    callback();
            }
        }

        /// <summary>
        /// 比較するファイルの合計(比較元+比較先)
        /// </summary>
        /// <returns>ファイル数</returns>
        public int getTotalFilesCount()
        {
            return mSrcFiles.Count + mDestFiles.Count;
        }

        /// <summary>
        /// ファイルリストから同一ファイルを除く
        /// 日付とサイズをチェック
        /// </summary>
        /// <param name="same">同一ファイルのみ</param>
        /// <returns>ファイルリスト</returns>
        public List<FilesData> stripSameFile(bool same)
        {
            List<FilesData> files = new List<FilesData>();
            if (!same)
                files = mFiles.GetRange(0, mFiles.Count);
            else
                for (int i = mFiles.Count - 1; i >= 0; i--) {
                    if (mHashChk) {
                        if (mFiles[i].mSrcCrc != mFiles[i].mDstCrc)
                            files.Add(mFiles[i]);
                    } else {
                        if (!sameFile(mFiles[i].mSrcFile, mFiles[i].mDstFile))
                            files.Add(mFiles[i]);
                    }
                }
            return files;
        }

        /// <summary>
        /// ファイル同士の同一性をチェック
        /// </summary>
        /// <param name="srcFile">比較元ファイル</param>
        /// <param name="dstFile">比較先ファイル</param>
        /// <returns></returns>
        private bool sameFile(FileInfo srcFile, FileInfo dstFile)
        {
            if (srcFile == null || dstFile == null)
                return false;
            //  日時の比較は1秒以下の誤差がある場合があるので文字列に変換して比較
            if (srcFile.LastWriteTime.ToString().CompareTo(dstFile.LastWriteTime.ToString()) != 0)
                return false;
            if (srcFile.Length != dstFile.Length)
                return false;
            return true;
        }

        /// <summary>
        /// 比較リストへの変換
        /// </summary>
        /// <returns></returns>
        public List<List<string>> getStringList()
        {
            List<List<string>> fileStringLiset = new List<List<string>>();
            foreach (FilesData fileData in mFiles) {
                List<string> buf = new List<string>();
                buf.Add(Path.GetFileName(fileData.mRelPath));
                buf.Add(Path.GetDirectoryName(fileData.mRelPath));
                buf.Add(fileData.mSrcFile == null ? "" : fileData.mSrcFile.LastWriteTime.ToString());
                buf.Add(fileData.mSrcFile == null ? "" : fileData.mSrcFile.Length.ToString());
                buf.Add(fileData.mDstFile == null ? "" : fileData.mDstFile.LastWriteTime.ToString());
                buf.Add(fileData.mDstFile == null ? "" : fileData.mDstFile.Length.ToString());
                fileStringLiset.Add(buf);
            }
            return fileStringLiset;
        }

        /// <summary>
        /// 更新ファイルリスト
        /// </summary>
        /// <returns></returns>
        public List<FilesData> getUpdateFile()
        {
            List<FilesData> files = new List<FilesData>();
            foreach (var file in mFiles) {
                if (file.mSrcFile != null && file.mDstFile != null) {
                    //  両方に存在するファイル
                    if (file.mSrcFile.LastWriteTime > file.mDstFile.LastWriteTime)
                        files.Add(file);
                } else if (file.mSrcFile != null && file.mDstFile == null) {
                    //  コピー先にないファイル
                    files.Add(file);
                }
            }
            return files;
        }

        /// <summary>
        /// 比較元にないファイル
        /// </summary>
        /// <returns></returns>
        public List<FilesData> getNoExistFile()
        {
            List<FilesData> files = new List<FilesData>();
            foreach (var file in mFiles) {
                if (file.mSrcFile == null) {
                    files.Add(file);
                }
            }
            return files;
        }

        /// <summary>
        /// ファイルの更新コピー
        /// </summary>
        /// <returns>更新ファイル数</returns>
        public int updateCopy()
        {
            List<FilesData> fileList = getUpdateFile();
            int count = 0;
            foreach (var file in fileList) {
                if (file.mSrcFile == null)
                    continue;
                string dstPath;
                if (file.mDstFile == null) {
                    //  コピー先にないファイル
                    dstPath = Path.Combine(mDestDir, file.mRelPath);
                } else {
                    dstPath = file.mDstFile.FullName;
                }
                if (fileCopy(file.mSrcFile.FullName, dstPath))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// コピー元にないファイルの削除
        /// </summary>
        /// <returns>削除ファイル数</returns>
        public int noExistDestFileRemove()
        {
            List<FilesData> fileList = getNoExistFile();
            int count = 0;
            foreach (var file in fileList) {
                File.Delete(file.mDstFile.FullName);
                if (!File.Exists(file.mDstFile.FullName))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// コピー元にコピー先を同期させる
        /// コピー元にないファイルは削除する
        /// </summary>
        /// <returns>コピーファイル数+削除ファイル数</returns>
        public int syncFolder()
        {
            int count = 0;
            count += updateCopy();
            count += noExistDestFileRemove();
            return count;
        }

        /// <summary>
        /// ファイルコピー
        /// </summary>
        /// <param name="srcPath">コピー元ファイル名</param>
        /// <param name="dstPath">コピー先ファイル名</param>
        /// <returns>コピーの可否</returns>
        public bool fileCopy(string srcPath, string dstPath)
        {
            try {
                string dstDir = Path.GetDirectoryName(dstPath);
                if (!Directory.Exists(dstDir)) {
                    Directory.CreateDirectory(dstDir);
                }
                File.Copy(srcPath, dstPath, true);
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.ToString() + srcPath);
                return false;
            }
            return true;
        }

        /// <summary>
        /// ファイル検索
        /// </summary>
        /// <param name="folder">検索フォルダ</param>
        /// <param name="targetFile">対象ファイル</param>
        /// <param name="exceptFile">除外ファイル</param>
        /// <param name="exceptDir">除外フォルダ</param>
        /// <returns>ファイルリスト</returns>
        public List<FileInfo> getDirectories(string folder, string targetFile = "", string exceptFile = "", string exceptDir = "")
        {
            List<FileInfo> fileList = new List<FileInfo>();
            List<string> targetFiles = targetFile.Split(' ').ToList();
            List<string> exceptFiles = exceptFile.Split(' ').ToList();
            List<string> exceptDirectories = exceptDir.Split(' ').ToList();
            targetFiles.RemoveAll(p => p.Length == 0);
            exceptFiles.RemoveAll(p => p.Length == 0);
            exceptDirectories.RemoveAll(p => p.Length == 0);
            try {
                DirectoryInfo di = new DirectoryInfo(folder);
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    if (!exceptChk(dir.Name, exceptDirectories)) {
                        List<FileInfo> filesInfo = getDirectories(dir.FullName, targetFile, exceptFile, exceptDir);
                        if (filesInfo != null && 0 < filesInfo.Count) {
                            fileList.AddRange(filesInfo);
                        }
                    }
                }
                string[] files = Directory.GetFiles(folder);
                foreach (var file in files) {
                    string fileName = Path.GetFileName(file);
                    if (targetChk(fileName, targetFiles) && !exceptChk(file, exceptFiles)) {
                        FileInfo fi = new FileInfo(file);
                        if (fi != null)
                            fileList.Add(fi);
                    }
                }
                return fileList;
            } catch (Exception e) {
                return null;
            }
        }

        /// <summary>
        /// 対象ファイルチェック
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="targetFiles">対象ファイルリスト(ワイルドカード)</param>
        /// <returns>対象の有無</returns>
        private bool targetChk(string fileName, List<string> targetFiles)
        {
            if (0 < targetFiles.Count) {
                foreach (string wc in targetFiles) {
                    if (ylib.wcMatch(fileName, wc))
                        return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 除外ファイルチェック
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <param name="exceptFiles">除外ファイルリスト(ワイルドカード)</param>
        /// <returns>除外の有無</returns>
        private bool exceptChk(string fileName, List<string> exceptFiles)
        {
            if (0 < exceptFiles.Count) {
                foreach (string wc in exceptFiles) {
                    if (ylib.wcMatch(fileName, wc))
                        return true;
                }
            }
            return false;
        }
    }
}
