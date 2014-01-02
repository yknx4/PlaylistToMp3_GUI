using Equin.ApplicationFramework;
using ffmpeg_convert;
using PlaylistToMp3_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Program_Settings = PlaylistToMp3__WF_.Properties.Settings;

namespace PlaylistToMp3__WF_
{
    public partial class MainWindow : Form
    {
        /// <summary>
        /// Logs the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// 
        private void log(params string[] args)
        {
            foreach (string log in args)
            {
                string full_log = DateTime.Now.ToShortTimeString() + ": " + log + Environment.NewLine;
#if (DEBUG)
                logfile.Write(full_log);
                logfile.AutoFlush = true;
#endif

                if (txtLog.InvokeRequired)
                {
                    txtLog.BeginInvoke((MethodInvoker)delegate()
                    {
                        txtLog.AppendText(full_log);
                    });
                }
                else txtLog.AppendText(full_log);
            }
        }
        private void setStatus(string text)
        {
            //Dirty hack way
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke((MethodInvoker)delegate()
                {
                    tslblStatus.Text = text;
                });
            }
            else tslblStatus.Text = text;
        }
        private void LoadPlaylist(string inputFileName)
        {
            log(inputFileName + " playlist selected.");
            BackgroundWorker mPlaylistLoader = new BackgroundWorker();
            mPlaylistLoader.DoWork += (sender, EventArgs) =>
            {
                EventArgs.Result = PlaylistToMp3_DLL.PlaylistLoader.GetPlaylist(inputFileName);
            };
            mPlaylistLoader.RunWorkerCompleted += (sender, EventArgs) => {
                if (EventArgs.Result != null)
                {
                    var result = (List<MusicFile>)EventArgs.Result;
                    playlist = new BindingList<MusicFile>(result);
                    log(inputFileName + " playlist loaded.");
                    BindingListView<MusicFile> view = new BindingListView<MusicFile>(playlist);
                    dtgrPlaylist.DataSource = view;

                    tslblStatus.Text = playlist.Count + " song loaded.";
                    log(playlist.Count + " song loaded.");
                    PlaylistPath = new FileInfo(inputFileName);
                }
                else
                {
                    tslblStatus.Text = inputFileName + " failed to load loaded.";
                    log(inputFileName + " failed to load loaded.");
                }
            };
            mPlaylistLoader.RunWorkerAsync();
        }

        private void BeginRefreshDatagrid()
        {
            tmr.Enabled = true;
            tmr.Start();
            tmr.Tick += (sender, args) =>
            {
                dtgrPlaylist.Refresh();
                if (pgrConvert.Value == pgrConvert.Maximum)
                {
                    ((System.Windows.Forms.Timer)sender).Stop();
                }
            };
        }

        private void Convert()
        {
            log("Converting " + playlist.Count + " items.");
            tslblStatus.Text = "Converting " + playlist.Count + " items.";
            pgrConvert.Value = 0;
            pgrConvert.Maximum = playlist.Count;
            List<Tuple<BackgroundWorker, convertArgs>> Conversions = new List<Tuple<BackgroundWorker, convertArgs>>();
            ffmpeg_convert.FFmpeg.Mp3ConversionArgs mp3Args = new FFmpeg.Mp3ConversionArgs
            {
                isVariable = rdbVBR.Checked,
                Preset = (int)cmbPresets.SelectedItem,
                //MinBitrate = (int)cmbMinBR.SelectedItem
            };
            log("FFmpeg mp3 conversion args:" +
                Environment.NewLine +
                "         VBR: " + mp3Args.isVariable +
                Environment.NewLine +
                "         Preset: " + mp3Args.Preset );
            foreach (MusicFile source in playlist)
            {
                source.resetEvents();
                source.Progress = 0;
                source.isConverted = false;
                source.Converted += source_Converted;
                //log("Preparing " + source.FileName + ".");
#if (DEBUG)
                string outputFileName = Extensions.CombineWithValidate(source.Artist, source.Album, source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
#else
                //string outputFileName = source.Artist+"\\"+source.Album+"\\" + source.FileInformation.Name.Replace(source.FileInformation.Extension,"") + ".mp3";
                //string outputFileName = Extensions.CombineWithValidate("output",source.Artist , source.Album , source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
                string outputFileName = Extensions.CombineWithValidate(source.Artist, source.Album, source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
#endif
                if (outputFileName.Length > 200)
                {
                    outputFileName = Extensions.CombineWithValidate(source.Artist.Substring(0, 20), source.Album.Substring(0, 20), source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
                }
                if (OutputPath.Directory.Exists)
                {
                    outputFileName = Path.Combine(OutputPath.FullName, outputFileName);
                }
                FileInfo output = new FileInfo(outputFileName);
                //log("Output: " + output.FullName);

                #region IMPLEMENT_MESSAGEBOX

                if (output.Exists)
                {
                    log(output.Name + " already exists.", "Skipping");
                    source.isConverted = true;
                    continue;
                }
                //while (output.Exists)
                //{
                //    int cnt = 1;
                //    output = new FileInfo(Extensions.CombineWithValidate("output", source.Artist.Substring(0, 20), source.Album.Substring(0, 20), source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + "(" + cnt + ").mp3"));
                //    cnt++;
                //}
                output.Directory.Create();
                if (MusicFile.CompareBitRate(source, mp3Args.isVariable?(int)cmbMinBR.SelectedItem:(int)cmbPresets.SelectedItem))
                {
                    log(source.ShortFileName + " has lower bitrate than minimum.", "Copying directly to output");
                    source.FileInformation.CopyTo(output.FullName);
                    source.isConverted = true;
                    continue;
                }

                #endregion IMPLEMENT_MESSAGEBOX

                convertArgs conversionArgs = new convertArgs
                {
                    Input = source,
                    Arguments = mp3Args,
                    Output = output
                };

                BackgroundWorker mConvert = new BackgroundWorker();
                mConvert.WorkerReportsProgress = true;
                mConvert.RunWorkerCompleted += (sender, EventArgs) =>
                {
                    if (EventArgs.Error != null)
                    {
                        log(conversionArgs.Output.Name + "conversion failed.", "Exception: " + EventArgs.Error.ToString());
                    }
                    conversionArgs.Input.isConverted = (bool)EventArgs.Result;
                    log(conversionArgs.Input.ShortFileName + " => " + conversionArgs.Output.FullName + " conversion completed.");
                };
                mConvert.ProgressChanged += (sender, EventArgs) =>
                {
                    conversionArgs.Input.Progress = EventArgs.ProgressPercentage;
                };
                mConvert.DoWork += convertingProcess;
                Conversions.Add(new Tuple<BackgroundWorker, convertArgs>(mConvert, conversionArgs));
                //log("Conversion " + conversionArgs.Input.ShortFileName + " => " + conversionArgs.Output.FullName + " queued.");
            }

            foreach (var task in Conversions)
            {
                task.Item1.RunWorkerCompleted += runNewConversion;
            }

            for (int threads = 0; threads < Conversions.Count; threads++)
            {
                var task = Conversions[threads];
                this.Conversions.Enqueue(task);
                if (threads < thread_no) startNewConversion();
            }
        }

        private void SaveSettings()
        {
            Program_Settings.Default.Save();
        }
        private void startNewConversion()
        {
            if (Conversions.Count != 0)
            {
                var task = Conversions.Dequeue();
                task.Item1.RunWorkerAsync(task.Item2);
                log("Conversion of " + task.Item2.Input.ShortFileName + " started.");
            }
        }
        private void openPlaylistDialog()
        {
            m_open.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            if (m_open.ShowDialog() == DialogResult.OK && m_open.FileName != string.Empty)
            {
                LoadPlaylist(m_open.FileName);
            }
        }
    }
}