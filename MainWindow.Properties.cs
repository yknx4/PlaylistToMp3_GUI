using System;
using System.IO;
using System.Windows.Forms;

namespace PlaylistToMp3__WF_
{
    public partial class MainWindow : Form
    {
        private FileInfo PlaylistPath { get; set; }

        public FileInfo OutputPath
        {
            get
            {
                try
                {
                    return new FileInfo(txtOuput.Text);
                }
                catch (Exception)
                {
                }
                return new FileInfo("output/");
            }
        }
    }
}