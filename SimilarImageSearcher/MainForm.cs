using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SimilarImageSearch.Engine;
using System.IO;
using System.Threading;

namespace SimilarImageSearch.UI
{
    public partial class MainForm : Form
    {
        ReverseImageSearchEngine engine = new ReverseImageSearchEngine();
        List<ShapeInfo> SearchResult = null;
        int currentIndex = -1;
        Thread ThAddingToDB = null;
        float similarity;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowsImage_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtFilePath.Text = openFileDialog.FileName;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            lblSearchStatus.Text = "Searching...";
            btnSearch.Enabled = false;
            Application.DoEvents();
            DateTime before = DateTime.Now;
            SearchResult = new List<ShapeInfo>();
            foreach (ShapeInfo item in engine.SearchSimilarImages(txtFilePath.Text, similarity))
            {
                SearchResult.Add(item);
                lblResultsCount.Text = SearchResult.Count.ToString();
            }
            lblResultsCount.Text = SearchResult.Count.ToString();
            currentIndex = 0;
            ChangeResultIndex(currentIndex);
            TimeSpan ts = DateTime.Now-before;
            lblSearchStatus.Text = "Search finished in " + ts.TotalSeconds.ToString() + " sec.";
            btnSearch.Enabled = true;
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            ChangeResultIndex(currentIndex - 1);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            ChangeResultIndex(currentIndex + 1);
        }

        private void btnBrowseDir_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtDirPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void btnAddToDatabase_Click(object sender, EventArgs e)
        {
            if (txtDirPath.Text == "")
                return;
            if (btnAddToDatabase.Text.StartsWith("Start"))
            {
                btnAddToDatabase.Text = btnAddToDatabase.Text.Replace("Start", "Stop");
                ThAddingToDB = new Thread(new ParameterizedThreadStart(AddToDatabase));
                ThAddingToDB.Start(txtDirPath.Text);
                
            }
            else
            {
                btnAddToDatabase.Text = btnAddToDatabase.Text.Replace("Stop", "Start");
                if (ThAddingToDB != null && ThAddingToDB.ThreadState == ThreadState.Running)
                    ThAddingToDB.Abort();
            }
        }


        private void AddToDatabase(object objPath)
        {
            string path = (string)objPath;
            DateTime startTime = DateTime.Now;
            int count = 0;
            ChangeControlTextDelegate del = new ChangeControlTextDelegate(ChangeControlText);
            string status;
            foreach (string file in ReverseImageSearchEngine.SearchImageFiles(txtDirPath.Text, 1024))
            {
                count++;
                status = "Adding image (" + count.ToString() + "): \"" + file + "\".";
                this.Invoke(del,txtStatus, status);
                bool success = engine.InsertToDatabase(file);
                if (!success)
                {
                    status = "Error on adding image (" + count.ToString() + "): \"" + file + "\".";
                    this.Invoke(del, txtStatus, status);
                    this.Invoke(del, btnAddToDatabase, btnAddToDatabase.Text.Replace("Stop", "Start"));
                    return;
                }
            }
            status = "finished adding "+count.ToString()+" images to database in time: "+(DateTime.Now-startTime).ToString();
            this.Invoke(del, txtStatus, status);
            this.Invoke(del, btnAddToDatabase, btnAddToDatabase.Text.Replace("Stop", "Start"));
        }


        delegate void ChangeControlTextDelegate(Control ctrl, string text);
        private void ChangeControlText(Control ctrl, string text)
        {
            ctrl.Text = text;
        }

        private void ChangeResultIndex(int index)
        {
            if (SearchResult == null || SearchResult.Count == 0)
            {
                lblFileSize.Text = lblIndicator.Text = lblSize.Text = lblSimilarity.Text = "";
                pictureBoxResult.Image = null;
                return;
            }
            if (index >= SearchResult.Count)
                index = 0;
            if (index < 0)
                index = SearchResult.Count - 1;

            lblIndicator.Text = (index + 1).ToString() + " of " + SearchResult.Count.ToString();
            lblFileSize.Text = (SearchResult[index].FileLength / 1024F).ToString("#.##") + " KB";
            lblSize.Text = SearchResult[index].ImageSize.Width.ToString() + "x" + SearchResult[index].ImageSize.Height.ToString();
            lblSimilarity.Text = SearchResult[index].Similarity.ToString("#.##");
            string imagePath = SearchResult[index].ImagePath;
            if (File.Exists(imagePath))
                pictureBoxResult.Image = Image.FromFile(imagePath);
            currentIndex = index;
        }

        private void btnClearDB_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Are you sure to delete all images from database?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == System.Windows.Forms.DialogResult.Yes)
            {
                int c = engine.DeleteAllData();
                ChangeControlText(txtStatus, c.ToString() + " items deleted from database.");
            }
        }

        private void trackBarSimilarity_Scroll(object sender, EventArgs e)
        {
            similarity = (float)(trackBarMinSimilarity.Value) / trackBarMinSimilarity.Maximum;
            lblMinSimilarity.Text = similarity.ToString();
        }
    }
}
