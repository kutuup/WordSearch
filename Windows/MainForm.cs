﻿namespace Windows
{
    using System.Linq;
    using Model.Data;
    using Model.Search;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private IEngineData searchEngineData;
        private SearchEngine searchEngine;
        private BackgroundWorker workerThread;

        private Pen gridPen = new Pen(Color.IndianRed, 4F);
        private Brush foundWordBackgroundBrush = new SolidBrush(Color.Khaki);
        private Brush searchIndexesBackgroundBrush = new SolidBrush(Color.SlateBlue);
        private Brush letterBrush = new SolidBrush(Color.Indigo);

        private int letterWidth;
        private IList<int> indexesFound = new List<int>();
        private IList<int> indexesBeingSearched = new List<int>();

        private bool allowExpectedWordsCheck;

        private void Form1_Load(object sender, EventArgs e)
        {
            workerThread = new BackgroundWorker();
            workerThread.DoWork += workerThread_DoWork;
            workerThread.RunWorkerCompleted += workerThread_RunWorkerCompleted;
            workerThread.WorkerSupportsCancellation = true;

            findExpectedWordsToolStripMenuItem_Click(null, null);
        }

        void workerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            searchToolStripMenuItem.Enabled = true;
            cancelToolStripMenuItem.Enabled = false;
            indexesBeingSearched.Clear();
            wordSearchPictureBox.Invalidate(false);
        }

        private void SearchEngineFoundWord(string direction, List<int> charIndexes, string word)
        {

            Action appendFoundWordToTextBox = () =>
                {
                    if (expectedWordsListBox.Items.IndexOf(word) >= 0)
                    {
                        this.allowExpectedWordsCheck = true;
                        expectedWordsListBox.SetItemCheckState(expectedWordsListBox.Items.IndexOf(word), CheckState.Checked);
                        this.allowExpectedWordsCheck = false;
                    }
                    foundWordsTextbox.AppendText(word + Environment.NewLine);

                    if (searchForAllWordsToolStripMenuItem.Checked)
                    {
                        foreach (var i in charIndexes.Where(i => !this.indexesFound.Contains(i)))
                            this.indexesFound.Add(i);
                    }
                    else
                    {
                        if (expectedWordsListBox.Items.IndexOf(word) >= 0)
                            foreach (var i in charIndexes.Where(i => !this.indexesFound.Contains(i)))
                                this.indexesFound.Add(i);
                    }
                };
            Invoke(appendFoundWordToTextBox);

            Action redraw = () => wordSearchPictureBox.Invalidate(false);
            Invoke(redraw);
        }

        private void SearchEngineBoxesBeingSearched(string direction, List<int> charIndexes)
        {
            indexesBeingSearched = charIndexes;

            Action redraw = () => wordSearchPictureBox.Invalidate(false);

            Invoke(redraw);
        }

        void workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            searchEngine.CheckAllPossibleWords();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (searchEngine != null)
            {
                searchEngine.BoxesBeingSearched -= SearchEngineBoxesBeingSearched;
                searchEngine.FoundWord -= SearchEngineFoundWord;
                searchEngine.Cancel = true;
            }
        }

        private void wordSearchPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (this.searchEngineData == null) return;

            var font = new Font("consolas", 32);

            for (var i = 0; i < this.searchEngineData.Letters.Length; i++)
            {
                var point = new Point(letterWidth * (i % this.searchEngineData.Width), letterWidth * (i / this.searchEngineData.Width));
                var rect = new Rectangle(point, new Size(letterWidth, letterWidth));

                if (indexesFound.Contains(i) && !indexesBeingSearched.Contains(i))
                    e.Graphics.FillRectangle(foundWordBackgroundBrush, rect);

                if (indexesBeingSearched.Contains(i))
                    e.Graphics.FillRectangle(searchIndexesBackgroundBrush, rect);

                e.Graphics.DrawRectangle(gridPen, rect);

                e.Graphics.DrawString(
                    this.searchEngineData.Letters[i].ToString(CultureInfo.InvariantCulture),
                    font, letterBrush, point
                );
            }
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchEngine = SearchEngineFactory.Get(this.searchEngineData);
            searchEngine.BoxesBeingSearched += SearchEngineBoxesBeingSearched;
            searchEngine.FoundWord += SearchEngineFoundWord;
            this.ClearFormState();
            SetButtonStateToOperating();
            workerThread.RunWorkerAsync();
        }

        private void ClearFormState()
        {
            this.letterWidth = this.wordSearchPictureBox.Width / this.searchEngineData.Width;
            this.foundWordsTextbox.Clear();
            this.indexesBeingSearched.Clear();
            this.indexesFound.Clear();
            this.cancelToolStripMenuItem.Enabled = false;
            this.searchToolStripMenuItem.Enabled = true;
        }

        private void SetButtonStateToOperating()
        {
            this.cancelToolStripMenuItem.Enabled = true;
            this.searchToolStripMenuItem.Enabled = false;
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchEngine.Cancel = true;
            searchToolStripMenuItem.Enabled = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void computerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LoadNewWordSearch("Computers");
        }

        private void LoadNewWordSearch(string wordsearchName)
        {
            this.searchEngineData = new WordSearchResourceData(wordsearchName);
            this.ClearFormState();
            this.wordSearchPictureBox.Invalidate(false);

            expectedWordsListBox.Items.Clear();
            expectedWordsListBox.Items.AddRange(this.searchEngineData.ExpectedWords.ToArray());
        }

        private void wikipediaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LoadNewWordSearch("Wikipedia");
        }

        private void testsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LoadNewWordSearch("Test");
        }

        private void simpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.LoadNewWordSearch("Simple");
        }

        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void searchForAllWordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchForAllWordsToolStripMenuItem.Checked = true;
            findExpectedWordsToolStripMenuItem.Checked = false;

            foundWordsTextbox.Visible = true;
            expectedWordsListBox.Visible = false;
        }

        private void findExpectedWordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            searchForAllWordsToolStripMenuItem.Checked = false;
            findExpectedWordsToolStripMenuItem.Checked = true;

            foundWordsTextbox.Visible = false;
            expectedWordsListBox.Visible = true;
        }

        private void expectedWordsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!this.allowExpectedWordsCheck)
            {
                e.NewValue = e.NewValue == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
            }
        }
    }
}
