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
using System.Windows.Forms;
using System.IO;
using OpenTK.Graphics;

namespace PiggyDump
{
    public partial class StandardUI : Form
    {
        private HOGFile defaultHogFile;
        private PIGFile defaultPigFile;
        private SNDFile defaultSoundFile;
        public static UserOptions options = new UserOptions();

        public bool readyForUse = false;

        public SNDFile DefaultSoundFile { get => defaultSoundFile; }
        public PIGFile DefaultPigFile { get => defaultPigFile; }

        public StandardUI()
        {
            InitializeComponent();

            options.openOptionsFile(Application.StartupPath + "/default.cfg");
            tbLog.Text += "\r\n-----------------------------\r\n";

            string defaultHOG = options.GetOption("HOGFile", "");
            string defaultPIG = options.GetOption("PIGFile", "");
            string defaultSound = options.GetOption("SNDFile", "");
            if (defaultHOG != "")
            {
                defaultHogFile = LoadDefaultHOG(defaultHOG);
                if (defaultHogFile != null)
                {
                    //Piggy is dependent on HOG palettes, so we need a HOG first. 
                    if (defaultPIG != "")
                    {
                        defaultPigFile = LoadDefaultPig(defaultPIG, defaultHogFile);
                    }
                }
            }

            if (defaultSound != "")
            {
                defaultSoundFile = LoadDefaultSND(defaultSound);
            }

            readyForUse = CheckReadyForUse();

            if (!readyForUse)
            {
                MessageBox.Show("It seems you haven't configured your default .HOG, .PIG, and .SXX files yet." +
                    "\nThis must be done before you can use D2 Workshop" +
                    "\nYou can do this in the Options menu");
            }
            GraphicsContext.ShareContexts = false;
        }

        public void AppendConsole(string msg)
        {
            tbLog.Text += msg;
            tbLog.SelectionStart = tbLog.Text.Length - 1;
            tbLog.ScrollToCaret();
        }

        private void menuItem11_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HOG files|*.HOG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                HOGFile archive = new HOGFile();
                archive.LoadDataFile(openFileDialog1.FileName);
                HOGEditor archiveEditor = new HOGEditor(archive);
                archiveEditor.Show();
            }
        }

        private void menuItem12_Click(object sender, EventArgs e)
        {
            HOGFile archive = new HOGFile();
            HOGEditor archiveEditor = new HOGEditor(archive);
            archiveEditor.Show();
        }

        private void mnuSetDefaultPIG_Click(object sender, EventArgs e)
        {
            if (defaultHogFile == null)
            {
                MessageBox.Show("A default HOG file must be loaded before loading the default PIG file");
                return;
            }
            openFileDialog1.Filter = ".PIG files|*.PIG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string paletteName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                PIGFile archive = new PIGFile(defaultHogFile.LookUpPalette(paletteName + ".256"));
                archive.LoadDataFile(openFileDialog1.FileName);
                PIGEditor archiveEditor = new PIGEditor(archive);
                archiveEditor.host = this;
                string[] tempstr = openFileDialog1.SafeFileName.Split('.');
                string palname = tempstr[0] + ".256";
                archiveEditor.Show();
            }
        }

        private void DefaultHOGMenu_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HOG files|*.HOG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                defaultHogFile = LoadDefaultHOG(openFileDialog1.FileName);
            }
        }

        private void StandardUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            options.WriteOptions(Application.StartupPath + "/default.cfg");
        }

        private void DefaultPIGMenu_Click(object sender, EventArgs e)
        {
            if (defaultHogFile == null)
            {
                AppendConsole("A default HOG file must be loaded before loading the default PIG file.");
                return;
            }
            openFileDialog1.Filter = ".PIG files|*.PIG";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                defaultPigFile = LoadDefaultPig(openFileDialog1.FileName, defaultHogFile);
            }
        }

        private void menuItem4_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".HAM files|*.HAM";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                HAMFile archive = new HAMFile(defaultPigFile);
                int res = archive.LoadDataFile(openFileDialog1.FileName);
                if (res == 0)
                {
                    HAMEditor2 archiveEditor = new HAMEditor2(archive, this);
                    archiveEditor.Show();
                }
                else if (res == -1)
                    AppendConsole("HAM file has invalid signature. Appears to be a V-HAM file.\r\n");
                else if (res == -2)
                    AppendConsole("HAM file is unknown version.\r\n");
                else if (res == -3)
                    AppendConsole("Cannot open HAM file: File not found.\r\n");
                else if (res == -4)
                    AppendConsole("Cannot open HAM file: Permission denied.\r\n");
            }
        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            HXMLoadDialog dialog = new HXMLoadDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                HAMFile dataFile = new HAMFile(defaultPigFile);
                dataFile.LoadDataFile(dialog.HAMFilename);
                HXMFile hxmFile = new HXMFile(dataFile);
                hxmFile.LoadDataFile(dialog.HXMFilename);
                HXMEditor editor = new HXMEditor(hxmFile, this);
                editor.Show();
            }
        }

        private void menuItem23_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".POF files|*.POF";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Polymodel model = POFReader.ReadPOFFile(openFileDialog1.FileName);
                PolymodelPreviewer archiveEditor = new PolymodelPreviewer(model, this);
                archiveEditor.Show();
            }
        }

        private void menuItem6_Click(object sender, EventArgs e)
        {
            /*HXMFile hxm = new HXMFile();

            HXMEditor editor = new HXMEditor(hxm, this);
            editor.Show();*/
        }

        private void menuItem24_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".S** files|*.S22;*.S11";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SNDFile archive = new SNDFile();
                archive.LoadDataFile(openFileDialog1.FileName);
                SXXEditor archiveEditor = new SXXEditor(this, archive);
                if (Path.GetExtension(openFileDialog1.FileName).Equals(".S11", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(openFileDialog1.FileName).Equals(".HAM", StringComparison.OrdinalIgnoreCase))
                {
                    archiveEditor.isLowFi = true;
                }
                archiveEditor.Show();
            }
        }

        private void menuItem25_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = ".S** files|*.S22;*.S11";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                defaultSoundFile = new SNDFile();
                defaultSoundFile.LoadDataFile(openFileDialog1.FileName);
                options.SetOption("SNDFile", openFileDialog1.FileName);
                tbLog.Text += "Loaded .SXX!\r\n";
            }
        }

        private void OptionsMenu_Click(object sender, EventArgs e)
        {
            ConfigDialog config = new ConfigDialog();
            if (config.ShowDialog() == DialogResult.OK)
            {
                options.SetOption("HOGFile", config.HogFilename);
                options.SetOption("PIGFile", config.PigFilename);
                options.SetOption("SNDFile", config.SndFilename);
                options.SetOption("CompatObjBitmaps", config.NoPMView.ToString());
                options.SetOption("TraceModels", config.Traces.ToString());
                options.SetOption("TraceDir", config.TraceDir);
                options.SetOption("PMVersion", config.PofVer.ToString());

                defaultHogFile = LoadDefaultHOG(config.HogFilename);
                if (defaultHogFile != null)
                    defaultPigFile = LoadDefaultPig(config.PigFilename, defaultHogFile);
                else
                    AppendConsole("Could not load default PIG, since the default HOG is not loaded");
                defaultSoundFile = LoadDefaultSND(config.SndFilename);
            }
        }

        private void MnuAbout_Click(object sender, EventArgs e)
        {
#if DEBUG
            HAMFile datafile = new HAMFile(DefaultPigFile);
            if (datafile.LoadDataFile("C:/Games/Descent/D2X-Rebirth/GOOD.HAM") == 0)
            {
                Editor.Level level = new Editor.Level();
                level.LoadMine("C:/Games/Descent/D2X-Rebirth/d2leva-1.rl2.good");
                Editor.EditorUI editor = new Editor.EditorUI(level, datafile);
                editor.Show();
                //Editor.ConvertToOverload.WriteOverloadLevel("c:/Games/Descent/D2X-Rebirth/test.overload", level);
            }
            /*level.SaveMine("C:/Games/Descent/D2X-Rebirth/d2leva-1.rl2");
            Editor.Level level2 = new Editor.Level();
            level2.LoadMine("C:/Games/Descent/D2X-Rebirth/d2leva-1.rl2");*/
#else
            AppendConsole("Descent II Workshop by ISB... heh\n");
#endif
        }

        private void menuItem6_Click_1(object sender, EventArgs e)
        {
            VHAMLoadDialog dialog = new VHAMLoadDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                HAMFile dataFile = new HAMFile(defaultPigFile);
                int res = dataFile.LoadDataFile(dialog.HAMFilename);
                if (res != 0)
                {
                    AppendConsole("Error loading base HAM file.\r\n");
                }
                if (res == -1)
                    AppendConsole("HAM file has invalid signature. Appears to be a V-HAM file.\r\n");
                else if (res == -2)
                    AppendConsole("HAM file is unknown version.\r\n");
                else if (res == -3)
                    AppendConsole("Cannot open HAM file: File not found.\r\n");
                else if (res == -4)
                    AppendConsole("Cannot open HAM file: Permission denied.\r\n");
                VHAMFile hxmFile = new VHAMFile(dataFile);
                hxmFile.LoadDataFile(dialog.HXMFilename);
                VHAMEditor editor = new VHAMEditor(hxmFile, this);
                editor.Show();
                //HXMEditor editor = new HXMEditor(hxmFile, this);
                //editor.Show();
            }
        }

        private HOGFile LoadDefaultHOG(string filename)
        {
            HOGFile defaultHOG = new HOGFile();
            try
            {
                defaultHOG.LoadDataFile(filename);
                AppendConsole("Loaded default HOG file!\r\n");
            }
            catch (Exception exc)
            {
                AppendConsole("Failed to load default HOG file!\r\n");
                FileExceptionHandler(exc, "HOG file");
                defaultHOG = null;
            }
            return defaultHOG;
        }

        private PIGFile LoadDefaultPig(string filename, HOGFile hogFile)
        {
            string paletteName = Path.ChangeExtension(Path.GetFileName(filename), ".256");
            PIGFile defaultPIG = new PIGFile(hogFile.LookUpPalette(paletteName));
            try
            {
                defaultPIG.LoadDataFile(filename);
                AppendConsole("Loaded default PIG file!\r\n");
            }
            catch (Exception exc)
            {
                AppendConsole("Failed to load default PIG file!\r\n");
                FileExceptionHandler(exc, "PIG file");
                defaultPIG = null;
            }
            return defaultPIG;
        }

        private SNDFile LoadDefaultSND(string filename)
        {
            SNDFile defaultSND = new SNDFile();
            try
            {
                defaultSND.LoadDataFile(filename);
                AppendConsole("Loaded default SND file!\r\n");
                if (defaultSoundFile != null)
                    defaultSoundFile.CloseDataFile();
            }
            catch (Exception exc)
            {
                AppendConsole("Failed to load default SND file!\r\n");
                FileExceptionHandler(exc, "SND file");
                defaultSND = null;
            }
            return defaultSND;
        }

        private void FileExceptionHandler(Exception error, string context)
        {
            if (error is FileNotFoundException)
            {
                AppendConsole(String.Format("The specified {0} was not found.\r\n", context));
            }
            else if (error is UnauthorizedAccessException)
            {
                AppendConsole(String.Format("You do not have permission to access the specified {0}.\r\n", context));
            }
            else
            {
                AppendConsole(String.Format("Unhandled error loading {0}: {1}.\r\n", context, error.Message));
            }
        }

        //Dear brave reader who's decided to look at my shitty code
        //when you change your mind on things while working a project for roughly 8 years
        //stupid things like these two functions happen. 
        private void FileErrorCodeHandler(int code, string context)
        {
            if (code == -1)
                AppendConsole(String.Format("{0} has invalid signature and may be corrupt, or of the wrong format.\r\n", context));
            else if (code == -2)
                AppendConsole(String.Format("{0} is unknown version.\r\n", context));
            else if (code == -3)
                AppendConsole(String.Format("The specified {0} was not found.\r\n", context));
            else if (code == -4)
                AppendConsole(String.Format("You do not have permission to access the specified {0}.\r\n", context));
        }

        private bool CheckReadyForUse()
        {
            return (defaultHogFile != null) && (defaultPigFile != null) && (defaultSoundFile != null);
        }

        private void OpenHXMMenu_Click(object sender, EventArgs e)
        {
            HXMLoadDialog dialog = new HXMLoadDialog();
            int res;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                HAMFile dataFile = new HAMFile(defaultPigFile);
                VHAMFile augmentFile = null;
                res = dataFile.LoadDataFile(dialog.HAMFilename);
                if (res != 0)
                {
                    FileErrorCodeHandler(res, "HAM file");
                    return;
                }
                if (dialog.VHAMFilename != "")
                {
                    augmentFile = new VHAMFile(dataFile);
                    res = augmentFile.LoadDataFile(dialog.VHAMFilename);
                    if (res != 0)
                    {
                        FileErrorCodeHandler(res, "VHAM file");
                        return;
                    }
                }
                HXMFile hxmFile = new HXMFile(dataFile);
                if (augmentFile != null)
                    hxmFile.augmentFile = augmentFile;

                hxmFile.LoadDataFile(dialog.HXMFilename);
                HXMEditor editor = new HXMEditor(hxmFile, this);
                editor.Show();
            }
        }
    }
}