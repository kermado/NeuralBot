using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace AimBot
{
    public class FileFilter
    {
        public delegate void FileChanged(FileFilter obj);

        private string directory;
        private string pattern;
        private string filename;

        public event FileChanged OnFileChanged;

        public FileFilter(string directory, string pattern, string filename)
        {
            this.directory = directory;
            this.pattern = pattern;
            this.filename = filename;
        }

        public string FileName
        {
            get { return filename; }
            set
            {
                if (filename != value)
                {
                    filename = value;
                    OnFileChanged?.Invoke(this);
                }
            }
        }

        public string FilePath
        {
            get { return Path.Combine(directory, filename); }
        }

        [JsonIgnore]
        public IEnumerable<string> FileNames
        {
            get
            {
                foreach (var path in Directory.GetFiles(directory, pattern))
                {
                    yield return Path.GetFileName(path);
                }
            }
        }
    }
}
