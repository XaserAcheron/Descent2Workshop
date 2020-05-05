﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibDescent.Data;
using Descent2Workshop.Transactions;

namespace Descent2Workshop.EditorPanels
{
    public partial class EClipPanel : UserControl
    {
        private EClip clip;
        private PIGFile piggyFile;
        private Palette palette;
        private bool isLocked = false;
        private int eclipID;
        private TransactionManager transactionManager;
        public EClipPanel(TransactionManager transactionManager)
        {
            this.transactionManager = transactionManager;
            InitializeComponent();
        }

        public void Init(List<string> VClipNames, List<string> EClipNames, List<string> SoundNames, Palette palette)
        {
            cbEClipBreakEClip.Items.Clear(); cbEClipBreakEClip.Items.Add("None");
            cbEClipMineCritical.Items.Clear(); cbEClipMineCritical.Items.Add("None");
            cbEClipBreakVClip.Items.Clear(); cbEClipBreakVClip.Items.Add("None");
            cbEClipBreakSound.Items.Clear(); cbEClipBreakSound.Items.Add("None");
            cbEClipBreakEClip.Items.AddRange(EClipNames.ToArray());
            cbEClipMineCritical.Items.AddRange(EClipNames.ToArray());
            cbEClipBreakVClip.Items.AddRange(VClipNames.ToArray());
            cbEClipBreakSound.Items.AddRange(SoundNames.ToArray());
            this.palette = palette;
        }

        public void Stop()
        {
            if (AnimTimer.Enabled)
            {
                PlayCheckbox.Checked = false;
            }
        }

        public void Update(int number, EClip clip, PIGFile piggyFile)
        {
            isLocked = true;
            eclipID = number;
            this.clip = clip;
            this.piggyFile = piggyFile;
            //vclip specific data
            txtEffectFrameSpeed.Text = clip.Clip.FrameTime.ToString();
            txtEffectTotalTime.Text = clip.Clip.PlayTime.ToString();
            txtEffectLight.Text = clip.Clip.LightValue.ToString();
            txtEffectFrameCount.Text = clip.Clip.NumFrames.ToString();

            //eclip stuff
            cbEClipBreakEClip.SelectedIndex = clip.ExplosionEClip + 1;
            cbEClipBreakVClip.SelectedIndex = clip.ExplosionVClip + 1;
            txtEffectExplodeSize.Text = clip.ExplosionSize.ToString();
            txtEffectBrokenID.Text = clip.DestroyedBitmapNum.ToString();
            cbEClipBreakSound.SelectedIndex = clip.SoundNum + 1;
            cbEClipMineCritical.SelectedIndex = clip.CriticalClip + 1;

            FrameSpinner.Value = 0;
            UpdateEffectFrame(0);

            isLocked = false;
        }

        private void nudEffectFrame_ValueChanged(object sender, EventArgs e)
        {
            if (!isLocked)
            {
                isLocked = true;
                UpdateEffectFrame((int)FrameSpinner.Value);
                isLocked = false;
            }
        }

        private void UpdateEffectFrame(int frame)
        {
            FrameNumTextBox.Text = clip.Clip.Frames[frame].ToString();

            if (pbEffectFramePreview.Image != null)
            {
                Bitmap temp = (Bitmap)pbEffectFramePreview.Image;
                pbEffectFramePreview.Image = null;
                temp.Dispose();
            }
            pbEffectFramePreview.Image = PiggyBitmapUtilities.GetBitmap(piggyFile, palette, clip.Clip.Frames[frame]);
        }

        private void EClipFixedProperty_TextChanged(object sender, EventArgs e)
        {
            if (isLocked || transactionManager.TransactionInProgress)
                return;
            TextBox textBox = (TextBox)sender;
            double value;
            if (double.TryParse(textBox.Text, out value))
            {
                FixTransaction transaction = new FixTransaction("EClip property", clip, (string)textBox.Tag, eclipID, 2, value);
                transactionManager.ApplyTransaction(transaction);
            }
        }

        private void EClipProperty_TextChanged(object sender, EventArgs e)
        {
            if (isLocked || transactionManager.TransactionInProgress)
                return;
            TextBox textBox = (TextBox)sender;
            int value;
            if (int.TryParse(textBox.Text, out value))
            {
                IntegerTransaction transaction = new IntegerTransaction("EClip property", clip, (string)textBox.Tag, eclipID, 2, value);
                transactionManager.ApplyTransaction(transaction);
            }
        }

        private void EClipComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLocked || transactionManager.TransactionInProgress)
                return;
            ComboBox comboBox = (ComboBox)sender;
            IntegerTransaction transaction = new IntegerTransaction("EClip property", clip, (string)comboBox.Tag, eclipID, 2, comboBox.SelectedIndex - 1);
            transactionManager.ApplyTransaction(transaction);
        }

        private void RemapMultiImage_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            ImageSelector selector = new ImageSelector(piggyFile, palette, true);
            if (selector.ShowDialog() == DialogResult.OK)
            {
                int value = selector.Selection;
                isLocked = true;
                VClipRemapTransaction transaction = new VClipRemapTransaction("EClip animation", eclipID, 2, clip.Clip, piggyFile, value);
                transactionManager.ApplyTransaction(transaction);

                txtEffectFrameCount.Text = clip.Clip.NumFrames.ToString();
                txtEffectFrameSpeed.Text = clip.Clip.FrameTime.ToString();
                FrameSpinner.Value = 0;
                UpdateEffectFrame(0);
                isLocked = false;
            }
            selector.Dispose();
        }

        private void RemapSingleImage_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            ImageSelector selector = new ImageSelector(piggyFile, palette, false);
            if (selector.ShowDialog() == DialogResult.OK)
            {
                isLocked = true;
                uint value = (uint)selector.Selection;
                IndexedUnsignedTransaction transaction = new IndexedUnsignedTransaction("VClip image", clip.Clip, "Frames", eclipID, 2, (uint)FrameSpinner.Value, value);
                transaction.undoEvent += IndexedUndoEvent;
                transactionManager.ApplyTransaction(transaction);

                UpdateEffectFrame((int)FrameSpinner.Value);
                FrameNumTextBox.Text = value.ToString();
                isLocked = false;
            }
            selector.Dispose();
        }

        private void PlayCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (PlayCheckbox.Checked)
            {
                txtEffectTotalTime.Enabled = txtEffectFrameCount.Enabled = FrameNumTextBox.Enabled = false;
                RemapAnimationButton.Enabled = FrameSpinner.Enabled = false;
                if (clip.Clip.NumFrames < 0) return;
                //Ah, the horribly imprecise timer. Oh well
                AnimTimer.Interval = (int)(1000.0 * clip.Clip.FrameTime);
                if (AnimTimer.Interval < 10) AnimTimer.Interval = 10;
                AnimTimer.Start();
            }
            else
            {
                txtEffectTotalTime.Enabled = txtEffectFrameCount.Enabled = FrameNumTextBox.Enabled = true;
                RemapAnimationButton.Enabled = FrameSpinner.Enabled = true;
                AnimTimer.Stop();
            }
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (clip.Clip.NumFrames < 0) return;
            int currentFrame = (int)FrameSpinner.Value;
            isLocked = true;
            UpdateEffectFrame(currentFrame);
            currentFrame++;
            if (currentFrame >= clip.Clip.NumFrames)
                currentFrame = 0;
            FrameSpinner.Value = currentFrame;
            isLocked = false;
        }

        //These need to reference the embedded vclip, so it needs to be a separate function
        private void EClipClipProperty_TextChanged(object sender, EventArgs e)
        {
            if (isLocked || transactionManager.TransactionInProgress)
                return;
            TextBox textBox = (TextBox)sender;
            double value;
            if (double.TryParse(textBox.Text, out value))
            {
                FixTransaction transaction = new FixTransaction("EClip property", clip.Clip, (string)textBox.Tag, eclipID, 2, value);
                transactionManager.ApplyTransaction(transaction);
            }
        }

        private void FrameNumTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isLocked || transactionManager.TransactionInProgress)
                return;
            TextBox textBox = (TextBox)sender;
            uint value;
            if (uint.TryParse(textBox.Text, out value))
            {
                IndexedUnsignedTransaction transaction = new IndexedUnsignedTransaction("VClip image", clip.Clip, "Frames", eclipID, 2, (uint)FrameSpinner.Value, value);
                transaction.undoEvent += IndexedUndoEvent;
                transactionManager.ApplyTransaction(transaction);

                UpdateEffectFrame((int)FrameSpinner.Value);
            }
        }

        private void IndexedUndoEvent(object sender, UndoIndexedEventArgs e)
        {
            FrameSpinner.Value = e.Index;
        }
    }
}
