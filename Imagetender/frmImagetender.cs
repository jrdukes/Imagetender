using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MVB = Microsoft.VisualBasic;

namespace JohnSkosnik.Imagetender
{
    // Structure to track an image's rotation status
    public struct ImageDisplay : IComparable<ImageDisplay>
    {
        public string fileName;
        public RotateFlipType rotateFlipType;

        public ImageDisplay(string fileName, RotateFlipType rotateFlipType)
        {
            this.fileName = fileName;
            this.rotateFlipType = rotateFlipType;
        }

        public int CompareTo(ImageDisplay imageDisplay)
        {
            return String.Compare(this.fileName, imageDisplay.fileName, false);
        }
    }

    public partial class frmImagetender : Form
    {
        private string[] m_imageExtensions;
        private int m_imageDisplaysIndex;
        private List<ImageDisplay> m_imageDisplays = new List<ImageDisplay>();

        public frmImagetender()
        {
            InitializeComponent();

            openToolStripMenuItem.Click += new EventHandler(openToolStripMenuItem_Click);
            exitToolStripMenuItem.Click += delegate (object o, EventArgs e) { Application.Exit(); };
            allowDeleteToolStripMenuItem.Click += delegate (object o, EventArgs e)
                { allowDeleteToolStripMenuItem.Checked = !allowDeleteToolStripMenuItem.Checked; };
            KeyDown += new KeyEventHandler(frm_KeyDown);
            Resize += delegate (object o, EventArgs e) { ShowImage(m_imageDisplays, m_imageDisplaysIndex); };

            // Convert semi-colon delimited appsetting list of image extensions to an array, for easy searching.
            string imageExtensions = ConfigurationManager.AppSettings["ImageExtensions"];
            char[] delimiterChars = { ';' };
            m_imageExtensions = imageExtensions.Split(delimiterChars);
            for (int i = 0; i < m_imageExtensions.Count(); i++)
            {
                m_imageExtensions[i] = m_imageExtensions[i].Trim();
            }
        }

        #region UI_Functionality

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            DialogResult dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                this.ShowImages(folderBrowserDialog.SelectedPath);
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(ConfigurationManager.AppSettings["Description"], this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void frm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs k)
        {
            switch (k.KeyCode)
            {
                case (Keys.Left):
                    {
                        // Show previous image (or the one at the end of the list, if we're at the beginning of the list).
                        m_imageDisplaysIndex = (m_imageDisplaysIndex == 0 ? m_imageDisplays.Count - 1 : m_imageDisplaysIndex - 1);
                        ShowImage(m_imageDisplays, m_imageDisplaysIndex);
                        break;
                    }
                case (Keys.Right):
                    {
                        // Show next image (or the first one, if we're at the end of the list).
                        m_imageDisplaysIndex = (m_imageDisplaysIndex == m_imageDisplays.Count - 1 ? 0 : m_imageDisplaysIndex + 1);
                        ShowImage(m_imageDisplays, m_imageDisplaysIndex);
                        break;
                    }
                case (Keys.Home):
                    {
                        // Show first image.
                        m_imageDisplaysIndex = 0;
                        ShowImage(m_imageDisplays, m_imageDisplaysIndex);
                        break;
                    }
                case (Keys.End):
                    {
                        // Show last image.
                        m_imageDisplaysIndex = m_imageDisplays.Count - 1;
                        ShowImage(m_imageDisplays, m_imageDisplaysIndex);
                        break;
                    }
                case (Keys.Delete):
                    {
                        // Delete an image, if the "Allow Deletions" menu item is checked.
                        if (allowDeleteToolStripMenuItem.Checked == false)
                        {
                            MessageBox.Show("Delete feature disabled. To enable, select “Edit, Allow Deletions” from the menu."
                                , this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                        if (picImage.Image == null)
                        {
                            MessageBox.Show("No image to delete.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                        DeleteImage(m_imageDisplays, m_imageDisplaysIndex);
                        break;
                    }
                case (Keys.Oemcomma):
                case (Keys.OemPeriod):
                    {
                        // Rotate the image 90 degrees clockwise (">") or counterclockwise ("<").
                        ImageDisplay imageDisplay = m_imageDisplays[m_imageDisplaysIndex];
                        // Get the image's current rotation enum, converted to int (will be between 0 and 3).
                        int rotateFlipTypeInt = (int)imageDisplay.rotateFlipType;
                        // Add to modifier, amount depending on which key was pressed.
                        if (k.KeyCode == Keys.Oemcomma) { rotateFlipTypeInt += 3; }
                        if (k.KeyCode == Keys.OemPeriod) { rotateFlipTypeInt += 1; }
                        // Convert modifier back to RotateFlipType. Used modulus to keep in the original 0 to 3 bound.
                        imageDisplay.rotateFlipType = (RotateFlipType)(rotateFlipTypeInt % 4);
                        // Store the new image rotation value, and display image.
                        m_imageDisplays[m_imageDisplaysIndex] = imageDisplay;
                        ShowImage(m_imageDisplays, m_imageDisplaysIndex);
                        break;
                    }
            }
            k.Handled = true;
        }

        #endregion

        #region BasicFunctionality

        public void ShowImages(string filePath)
        {
            lblFilepathname.Text = "Getting images in " + filePath + "…";
            m_imageDisplays = GetImages(filePath);
            m_imageDisplaysIndex = 0;
            ShowImage(m_imageDisplays, m_imageDisplaysIndex);
        }

        private void ShowImage(List<ImageDisplay> imageDisplays, int index)
        {
            if (imageDisplays.Count == 0)
            {
                lblFilepathname.Text = "(No images found.)";
                picImage.Image = null;
                return;
            }

            // Dispose of picImage's currently displayed image, to prevent out-of-memory errors (image on disk is unaffected).
            if (picImage.Image != null && picImage.Image != picImage.ErrorImage)
            {
                picImage.Image.Dispose();
            }
            Image image = null;

            // Ensure the index to the array of images isn't out of bounds:
            m_imageDisplaysIndex = Math.Max(m_imageDisplaysIndex, 0);
            m_imageDisplaysIndex = Math.Min(m_imageDisplaysIndex, m_imageDisplays.Count - 1);

            try
            {
                image = Image.FromFile(m_imageDisplays[m_imageDisplaysIndex].fileName);
                image.RotateFlip(m_imageDisplays[m_imageDisplaysIndex].rotateFlipType);
            }
            catch
            {
                image = null;
                lblFilepathname.Text = "Could not load the image “" + m_imageDisplays[m_imageDisplaysIndex] + "”, it may be invalid.";
                picImage.SizeMode = PictureBoxSizeMode.CenterImage;
                picImage.Image = picImage.ErrorImage;
                return;
            }

            lblFilepathname.Text = String.Format("{0} (Image {1} of {2}) ({3} × {4}) ({5})"
                , m_imageDisplays[m_imageDisplaysIndex].fileName, m_imageDisplaysIndex + 1, m_imageDisplays.Count
                , image.Width, image.Height
                , m_imageDisplays[m_imageDisplaysIndex].rotateFlipType.ToString());
            picImage.SizeMode = (image.Width > picImage.Width || image.Height > picImage.Height
                ? PictureBoxSizeMode.Zoom : PictureBoxSizeMode.CenterImage);
            picImage.Image = image;
        }

        private List<ImageDisplay> GetImages(string filePath)
        {
            List<ImageDisplay> imageDisplays = new List<ImageDisplay>();
            try
            {
                string[] directories = new string[] { filePath }.Concat(Directory.GetDirectories(filePath, "*", SearchOption.AllDirectories)).ToArray();
                foreach (string directory in directories)
                {
                    foreach (string fileName in Directory.GetFiles(directory))
                    {
                        // Only add files with common image extensions, taken from project properties. This should probably be customizable via the UI.
                        for (int i = 0; i < m_imageExtensions.Count(); i++)
                        {
                            if (fileName.ToLower().EndsWith(m_imageExtensions[i]))
                            {
                                imageDisplays.Add(new ImageDisplay(fileName, RotateFlipType.RotateNoneFlipNone));
                                break;
                            }
                        }
                    }
                }
                imageDisplays.Sort();
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occured: “" + e.Message + "”.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return imageDisplays;
        }

        private void DeleteImage(List<ImageDisplay> imageDisplays, int index)
        {
            // C# does not have "move file to Recycle Bin" functionality. So we'll use Visual Basic, which does:
            try
            {
                if (picImage.Image != picImage.ErrorImage)
                {
                    picImage.Image.Dispose();
                }

                MVB.FileIO.FileSystem.DeleteFile(m_imageDisplays[m_imageDisplaysIndex].fileName, MVB.FileIO.UIOption.AllDialogs
                    , MVB.FileIO.RecycleOption.SendToRecycleBin, MVB.FileIO.UICancelOption.ThrowException);

                this.lblFilepathname.Text = "Deleted “" + m_imageDisplays[m_imageDisplaysIndex] + "”.";
                m_imageDisplays.Remove(m_imageDisplays[m_imageDisplaysIndex]);
                // No need to increment m_imageFilenamesIndex, the deletion ensures the nth+1 element is now the nth.
                ShowImage(m_imageDisplays, m_imageDisplaysIndex);
            }
            catch (OperationCanceledException)
            {
                // Do nothing. An OperationCanceledException is thrown, but the file is unaffected.
            }
        }

        #endregion

    }

}