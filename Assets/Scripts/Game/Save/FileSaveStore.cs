using System;
using System.IO;
using System.Text;
using PlumSword.Core.Save;

namespace PlumSword.Game.Save
{
    public sealed class FileSaveStore : ISaveStore
    {
        private readonly string directory;

        public FileSaveStore(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (directory.Length == 0) throw new ArgumentException("Directory must not be empty.", nameof(directory));
            this.directory = directory;
        }

        public SaveLoadResult Load()
        {
            try
            {
                string path = Path.Combine(directory, "save.dat");
                return SaveCodec.Decode(File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null);
            }
            catch (Exception)
            {
                return new SaveLoadResult(SaveLoadStatus.Corrupt, null);
            }
        }

        public bool TryWrite(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            string temporaryPath = null;
            try
            {
                temporaryPath = Path.Combine(directory, "save.tmp");
                string savePath = Path.Combine(directory, "save.dat");
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporaryPath, text, new UTF8Encoding(false));
                if (File.Exists(savePath)) File.Replace(temporaryPath, savePath, null);
                else File.Move(temporaryPath, savePath);
                return true;
            }
            catch (Exception)
            {
                try { if (temporaryPath != null) File.Delete(temporaryPath); }
                catch (Exception) { }
                return false;
            }
        }
    }
}
