using System;
using System.IO;


namespace HlidacStatu.Connectors.IO
{
    public abstract class DistributedFilePath<T>
         where T : class
    {
        int hashLen = 0;
        string _root = "";
        protected Func<T, string> funcToGetId = null;
        public DistributedFilePath(int hashLength, string rootPath, Func<T, string> getId)
        {
            hashLen = hashLength;
            this._root = rootPath.Trim();


            // Normalize all separators to the OS-specific one
            if (Path.DirectorySeparatorChar == '\\')
                this._root = rootPath.Replace('/', Path.DirectorySeparatorChar);
            if (Path.DirectorySeparatorChar == '/')
                this._root = rootPath.Replace('\\', Path.DirectorySeparatorChar);
            
            // Ensure the path ends with a directory separator
            if (!this._root.EndsWith(Path.DirectorySeparatorChar))
                this._root += Path.DirectorySeparatorChar;
            
            funcToGetId = getId;
        }

        public string Root { get { return _root; } }

        public void InitPaths()
        {
            if (!System.IO.Directory.Exists(_root))
            {
                System.IO.Directory.CreateDirectory(_root);

                for (int i = 0; i < Math.Pow(16, hashLen); i++)
                {
                    string hash = i.ToString("X" + hashLen);
                    if (!System.IO.Directory.Exists(_root + hash))
                        System.IO.Directory.CreateDirectory(_root + hash);
                }
            }
        }

        public virtual string GetFullPath(T obj, string filename)
        {
            return GetFullDir(obj) + filename;
        }

        public virtual string GetFullDir(T obj)
        {
            return string.Format("{0}{1}{2}"
                , _root, GetHash(obj),Path.DirectorySeparatorChar);
        }

        public virtual string GetRelativeDir(T obj)
        {
            return GetHash(obj) + Path.DirectorySeparatorChar;

        }
        
        public virtual string GetRelativePath(T obj, string filename)
        {
            return GetRelativeDir(obj) + filename;

        }
        
        protected virtual string GetHash(T obj)
        {
            return Devmasters.Crypto.Hash.ComputeHashToHex(funcToGetId(obj)).Substring(0, hashLen);
        }
    }
}

