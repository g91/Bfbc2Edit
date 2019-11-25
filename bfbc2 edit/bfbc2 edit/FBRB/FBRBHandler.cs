using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

// tools\quickbms.exe -o tools\fbrb.bms unpack\level-00.fbrb unpack\level-00

namespace bfbc2_edit.FBRB
{
    class FBRBHandler
    {
        private string FileName;
        private string FBRBBMSScript = Application.StartupPath + "\\bin\\fbrb.bms";
        private string QuickbmsBin = Application.StartupPath + "\\bin\\quickbms.exe";
        private string XMLPythonScript = Application.StartupPath + "\\bin\\xml.py";
        private string ExtractBin = Application.StartupPath + "\\bin\\extract.bat";
        private string PackBin = Application.StartupPath + "\\bin\\pack.bat";
        private string DecryptDbxBin = Application.StartupPath + "\\bin\\decryptdbx.bat";
        private string EncryptDbxBin = Application.StartupPath + "\\bin\\encryptdbx.bat";
        private string CryptDbxPy = Application.StartupPath + "\\bin\\xml.py";
        private string PackPerlBin = Application.StartupPath + "\\bin\\fbrb.pl";
        private string GzipBin = Application.StartupPath + "\\bin\\tools\\gzip.exe";

        public DirectoryInfo TempDir;
        public List<string> Directories;
        public TreeNode DirectoryNode;
        public string TempPath = Application.StartupPath + "\\tmp\\";
        public string CurrentTempFile = "";
        public string LastOutput = "";

        public FBRBHandler(string FileName)
        {
            this.FileName = FileName;

            if (!CheckBinDirectory())
                throw new Exception("Your bin directory is missing files");

            InitFile();
        }

        private bool CheckBinDirectory()
        {
            if (!File.Exists(FBRBBMSScript) || !File.Exists(QuickbmsBin) || !File.Exists(XMLPythonScript) || !File.Exists(GzipBin))
                return false;

            return true;
        }

        private void InitFile()
        {
            if (String.IsNullOrEmpty(this.FileName))
                throw new Exception("FileName is null or empty");

            if (!File.Exists(this.FileName))
                throw new Exception("Unable to find file " + this.FileName);
        }

        public void RunProcess(string FileName)
        {
            ProcessStartInfo psi = new ProcessStartInfo(FileName);
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            Process p = Process.Start(psi);
            
            LastOutput = p.StandardOutput.ReadToEnd();

            p.WaitForExit();
        }

        public void OutputFile(string FileName)
        {
            File.WriteAllText(PackBin, string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"\r\n", PackPerlBin, this.FileName, TempDir.FullName, FileName));
            RunProcess(PackBin);
        }

        public void ExtractFile()
        {
            TempDir = Directory.CreateDirectory(TempPath + Path.GetFileNameWithoutExtension(this.FileName));

            if (!TempDir.Exists)
                throw new Exception("Unable to create temp directory \'" + TempDir.FullName + "\'");

            File.WriteAllText(ExtractBin, string.Format("\"{0}\" -o \"{1}\" \"{2}\" \"{3}\"\r\n", QuickbmsBin, FBRBBMSScript, this.FileName, TempDir.FullName));
            RunProcess(ExtractBin);

            Directories = new List<string>();
            DirectoryNode = new TreeNode(Path.GetFileNameWithoutExtension(this.FileName));
            PopulateList(ref Directories, TempDir.FullName, ref DirectoryNode, 0);
        }

        private void PopulateList(ref List<string> StrList, string Dir, ref TreeNode node, int prev)
        {
            string[] dirs = Directory.GetDirectories(Dir);

            foreach (string str in dirs)
            {
                string fstr = str.Substring(this.TempDir.FullName.Length + 1 + prev) + "\\";
                int i = fstr.IndexOf('\\');
                fstr = fstr.Substring(0, i);

                TreeNode newnode = new TreeNode(fstr);
                

                foreach (string s in Directory.GetFiles(str))
                {
                    string RealName = s.Substring(this.TempDir.FullName.Length + 1 + prev + i + 1);
                    newnode.Nodes.Add(RealName);
                }

                node.Nodes.Add(newnode);

                PopulateList(ref StrList, str, ref newnode, fstr.Length + 1 + prev);
            }
        }

        public string GetDecyrptedFileByName(string Name)
        {
            string temp = TempPath + "\\" + Path.GetFileName(Name);
            File.Copy(Name, temp, true);
            File.WriteAllText(DecryptDbxBin, string.Format("\"{0}\" \"{1}\"\r\n", CryptDbxPy, temp));
            RunProcess(DecryptDbxBin);

            CurrentTempFile = Path.GetDirectoryName(temp) + "\\" + Path.GetFileNameWithoutExtension(temp);

            return File.ReadAllText(CurrentTempFile + ".xml");
        }

        public void EncryptFileByName(string Name, string Data)
        {
            File.WriteAllText(EncryptDbxBin, string.Format("\"{0}\" \"{1}.xml\"\r\n", CryptDbxPy, CurrentTempFile));
            File.WriteAllText(CurrentTempFile + ".xml", Data.Replace("\n", "\r\n"));
            RunProcess(EncryptDbxBin);

            File.Copy(CurrentTempFile + ".dbx", Name, true);
        }
    }
}
