using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Program_Settings = PlaylistToMp3__WF_.Properties.Settings;

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
