﻿/*
    Copyright (c) 2019 SaladBadger

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Media;
using LibDescent.Data;
using Descent2Workshop.SaveHandlers;

namespace Descent2Workshop
{
    public partial class SXXEditor : Form
    {
        public SNDFile datafile;
        public StandardUI host;
        public bool isLowFi = false;
        public bool closeOnExit = true;
        private SoundCache cache;

        private SaveHandler saveHandler;
        public SXXEditor(StandardUI host, SNDFile datafile, SoundCache cache, string FileName)
        {
            InitializeComponent();
            this.datafile = datafile;
            this.host = host;
            this.cache = cache;
            Text = string.Format("{0} - Sound Editor", FileName);
        }

        private void SXXEditor_Load(object sender, EventArgs e)
        {
            SoundData sound;
            for (int i = 0; i < datafile.Sounds.Count; i++)
            {
                sound = datafile.Sounds[i];
                ListViewItem lvi = new ListViewItem(sound.Name);
                lvi.SubItems.Add(sound.Length.ToString());
                lvi.SubItems.Add(sound.Offset.ToString());
                lvi.SubItems.Add(i.ToString());
                //int compressionPercentage = (int)(image.compressionratio * 100f);
                //lvi.SubItems.Add(compressionPercentage.ToString() + "%");
                listView1.Items.Add(lvi);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            PlaySelected();
        }

        private void PlaySelected()
        {
            if (listView1.SelectedIndices.Count != 1) return;
            //byte[] sndData = datafile.LoadSound(listView1.SelectedIndices[0]);
            byte[] sndData = cache.GetSound(listView1.SelectedIndices[0]);
            byte[] buffer = new byte[sndData.Length * (isLowFi ? 8 : 4) + 44];

            WriteSound(new BinaryWriter(new MemoryStream(buffer, true)), sndData);

            using (MemoryStream memStream = new MemoryStream(buffer))
            using (SoundPlayer sp = new SoundPlayer(memStream))
            {
                sp.Load();
                sp.Play();
            };
        }

        private void WriteSoundToFile(string filename, byte[] data)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create));
            WriteSound(bw, data);
        }

        private void WriteSound(BinaryWriter bw, byte[] data)
        {
            bw.Write(0x46464952); //RIFF
            bw.Write(36 + data.Length);
            bw.Write(0x45564157);
            bw.Write(0x20746D66);
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            if (isLowFi)
            {
                bw.Write(11025);
                bw.Write(11025);
            }
            else
            {
                bw.Write(22050);
                bw.Write(22050);
            }
            bw.Write((short)1);
            bw.Write((short)8);
            bw.Write(0x61746164);
            bw.Write(data.Length);
            bw.Write(data);
            bw.Flush();
            bw.Close();
        }

        private void SXXEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*if (closeOnExit)
            {
                datafile.CloseDataFile();
            }*/
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            PlaySelected();
        }

        private void ExtractMenu_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            saveFileDialog1.Filter = "WAV Files|*.wav";
            if (listView1.SelectedIndices.Count > 1)
            {
                saveFileDialog1.FileName = "ignored";
            }
            else
            {
                saveFileDialog1.FileName = listView1.Items[listView1.SelectedIndices[0]].Text;
            }
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (listView1.SelectedIndices.Count > 1)
                {
                    string directory = Path.GetDirectoryName(saveFileDialog1.FileName);
                    foreach (int index in listView1.SelectedIndices)
                    {
                        string newpath = directory + Path.DirectorySeparatorChar + listView1.Items[index].Text + ".wav";
                        byte[] data = datafile.LoadSound(index);
                        WriteSoundToFile(newpath, data);
                    }
                }
                else
                {
                    if (saveFileDialog1.FileName != "")
                    {
                        byte[] data = datafile.LoadSound(listView1.SelectedIndices[0]);
                        WriteSoundToFile(saveFileDialog1.FileName, data);
                    }
                }
            }
        }

        private void SaveSNDFile()
        {
            if (saveHandler == null) //No save handler, so can't actually save right now
            {
                //HACK: Just call the save as handler for now... This needs restructuring
                SaveAsMenu_Click(this, new EventArgs());
                return; //and don't repeat the save code when we recall ourselves.
            }
            Stream stream = saveHandler.GetStream();
            if (stream == null)
            {
                MessageBox.Show(this, string.Format("Error opening save file {0}:\r\n{1}", saveHandler.GetUIName(), saveHandler.GetErrorMsg()));
            }
            else
            {
                //TODO: The source stream is closed, so cached data must be used
                for (int i = 0; i < datafile.Sounds.Count; i++)
                {
                    datafile.Sounds[i].Data = cache.GetSound(i);
                }
                datafile.Write(stream);
                stream.Dispose();
                if (saveHandler.FinalizeStream())
                {
                    MessageBox.Show(this, string.Format("Error writing save file {0}:\r\n{1}", saveHandler.GetUIName(), saveHandler.GetErrorMsg()));
                }
                else
                {
                    //transactionManager.UnsavedFlag = false;
                }
            }
        }

        private void SaveAsMenu_Click(object sender, EventArgs e)
        {
            if (isLowFi)
                saveFileDialog1.Filter = "S11 Files|*.s11";
            else
                saveFileDialog1.Filter = "S22 Files|*.s22";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (saveFileDialog1.FileName != "")
                {
                    //Ensure the old save handler is detached from any UI properly.
                    if (saveHandler != null) saveHandler.Destroy();
                    saveHandler = new FileSaveHandler(saveFileDialog1.FileName);
                    SaveSNDFile();
                    this.Text = string.Format("{0} - SND Editor", saveHandler.GetUIName());
                }
            }
        }
    }
}
