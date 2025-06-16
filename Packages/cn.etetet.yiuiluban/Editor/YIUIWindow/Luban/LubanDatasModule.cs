using System;
using System.IO;

namespace YIUILuban.Editor
{
    public class LubanDatasModule : LubanModuleBase
    {
        protected override ELubanEditorShowType EditorShowType => ELubanEditorShowType.Datas;

        public override void Initialize()
        {
            base.Initialize();
            GetAllData($"{this.Data.ConfigPath}");
        }

        protected override void GetAllData(string path)
        {
            m_AllData.Clear();

            if (!Directory.Exists(path))
            {
                return;
            }

            ProcessDirectory(path);

            OnSearchChanged();
        }

        private void ProcessDirectory(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                var extension = Path.GetExtension(file);

                if (extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = Path.GetFileName(file);

                AddData(fileName, file);
            }

            var subDirectories = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);

            foreach (string subDir in subDirectories)
            {
                string dirName = Path.GetFileName(subDir);

                if (dirName.Equals("Base", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ProcessDirectory(subDir);
            }
        }
    }
}