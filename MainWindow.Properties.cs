using System;
using System.IO;
using System.Windows.Forms;

namespace PlaylistToMp3__WF_
{
    public partial class MainWindow : Form
    {
        /// <summary>
        /// Gets or sets the playlist path.
        /// </summary>
        /// <value>
        /// The playlist path.
        /// </value>
        private FileInfo PlaylistPath { get; set; }

        /// <summary>
        /// Gets the output path.
        /// </summary>
        /// <value>
        /// The output path.
        /// </value>
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