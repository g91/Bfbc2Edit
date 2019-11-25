using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using bfbc2_edit.FBRB;

namespace bfbc2_edit
{
    public partial class MainForm : Form
    {
        FBRBHandler FbrbHandle;
        List<string> CurrentFbrbFiles;
        int FindPos = 0;
        string LastSearched = "";

        public MainForm()
        {
            InitializeComponent();
        }

        private void AddToLog(string str)
        {
            LogBox.Text += str + Environment.NewLine;
            LogBox.SelectionStart = LogBox.Text.Length;
            LogBox.ScrollToCaret();
            LogBox.Refresh();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();

                ofd.Filter = "FBRB Files (*.*)|*.fbrb";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Directory.Delete(Application.StartupPath + "\\tmp", true);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Directory.CreateDirectory(Application.StartupPath + "\\tmp");
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show(x.Message);
                    }

                    FbrbHandle = new FBRBHandler(ofd.FileName);

                    AddToLog("Extracting FBRB file...");
                    FbrbHandle.ExtractFile();
                    AddToLog(FbrbHandle.LastOutput);

                    DirectoryTree.Nodes.Clear();
                    DirectoryTree.Nodes.Add(FbrbHandle.DirectoryNode);

                    AddToLog("Done!");
                }
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }

        void FillChildNodes(List<string> Dirs, TreeNode node)
        {
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void DirectoryTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                string FilePath = this.FbrbHandle.TempPath + DirectoryTree.SelectedNode.FullPath;

                AddToLog("Locating file \"" + FilePath + "\"");
                EditWindow.Text = FbrbHandle.GetDecyrptedFileByName(FilePath);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }

        private void currentFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string FilePath = this.FbrbHandle.TempPath + DirectoryTree.SelectedNode.FullPath;

                AddToLog("Encrypting file \"" + FilePath + "\"");
                FbrbHandle.EncryptFileByName(FilePath, EditWindow.Text);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                Directory.Delete(Application.StartupPath + "\\tmp", true);
            }
            catch
            {
            }

            try
            {
                Directory.CreateDirectory(Application.StartupPath + "\\tmp");
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }

        private void mapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();

                sfd.Filter = "FBRB Files (*.*)|*.fbrb";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    AddToLog("Saving FBRB");
                    FbrbHandle.OutputFile(sfd.FileName);
                    AddToLog("Done!");
                }
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string str = FindTextBox.Text;

                if (LastSearched == str)
                {
                    FindPos = EditWindow.Find(str, FindPos + LastSearched.Length, RichTextBoxFinds.None);
                    LastSearched = str;
                }
                else
                {
                    FindPos = EditWindow.Find(str, 0, RichTextBoxFinds.None);
                    LastSearched = str;
                }

                EditWindow.SelectionStart = FindPos;
                EditWindow.ScrollToCaret();
                EditWindow.Select(FindPos, LastSearched.Length);
                EditWindow.Refresh();
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }
    }
}
