using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace P3DDebinarizer
{
	internal sealed class MainForm : Form
	{
		private readonly List<string> sourceFiles = new List<string>();
		private readonly ListBox fileList = new ListBox();
		private readonly TextBox outputFolder = new TextBox();
		private readonly TextBox log = new TextBox();
		private readonly ProgressBar progress = new ProgressBar();
		private readonly Label status = new Label();
		private readonly Button addFilesButton = new Button();
		private readonly Button addFolderButton = new Button();
		private readonly Button removeButton = new Button();
		private readonly Button clearButton = new Button();
		private readonly Button browseOutputButton = new Button();
		private readonly Button convertButton = new Button();
		private readonly BackgroundWorker worker = new BackgroundWorker();

		public MainForm()
		{
			this.Text = "P3D Debinarizer";
			this.MinimumSize = new Size(620, 430);
			this.Size = new Size(760, 560);
			this.StartPosition = FormStartPosition.CenterScreen;

			this.addFilesButton.Text = "Add P3D files...";
			this.addFolderButton.Text = "Add folder...";
			this.removeButton.Text = "Remove selected";
			this.clearButton.Text = "Clear";
			this.browseOutputButton.Text = "Browse...";
			this.convertButton.Text = "Debinarize";

			FlowLayoutPanel fileActions = new FlowLayoutPanel();
			fileActions.Dock = DockStyle.Fill;
			fileActions.AutoSize = true;
			fileActions.Controls.Add(this.addFilesButton);
			fileActions.Controls.Add(this.addFolderButton);
			fileActions.Controls.Add(this.removeButton);
			fileActions.Controls.Add(this.clearButton);

			TableLayoutPanel outputRow = new TableLayoutPanel();
			outputRow.Dock = DockStyle.Fill;
			outputRow.ColumnCount = 3;
			outputRow.RowCount = 1;
			outputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			outputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			outputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			Label outputLabel = new Label();
			outputLabel.Text = "Output folder (optional):";
			outputLabel.AutoSize = true;
			outputLabel.Anchor = AnchorStyles.Left;
			this.outputFolder.Dock = DockStyle.Fill;
			this.browseOutputButton.AutoSize = true;
			outputRow.Controls.Add(outputLabel, 0, 0);
			outputRow.Controls.Add(this.outputFolder, 1, 0);
			outputRow.Controls.Add(this.browseOutputButton, 2, 0);

			this.fileList.Dock = DockStyle.Fill;
			this.fileList.SelectionMode = SelectionMode.MultiExtended;
			this.fileList.HorizontalScrollbar = true;

			this.log.Dock = DockStyle.Fill;
			this.log.Multiline = true;
			this.log.ReadOnly = true;
			this.log.ScrollBars = ScrollBars.Vertical;
			this.log.BackColor = SystemColors.Window;

			this.progress.Dock = DockStyle.Fill;
			this.status.AutoSize = true;
			this.status.Anchor = AnchorStyles.Left;
			this.convertButton.AutoSize = true;

			TableLayoutPanel layout = new TableLayoutPanel();
			layout.Dock = DockStyle.Fill;
			layout.Padding = new Padding(10);
			layout.ColumnCount = 2;
			layout.RowCount = 6;
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.Controls.Add(fileActions, 0, 0);
			layout.SetColumnSpan(fileActions, 2);
			layout.Controls.Add(outputRow, 0, 1);
			layout.SetColumnSpan(outputRow, 2);
			layout.Controls.Add(this.fileList, 0, 2);
			layout.SetColumnSpan(this.fileList, 2);
			layout.Controls.Add(this.progress, 0, 3);
			layout.SetColumnSpan(this.progress, 2);
			layout.Controls.Add(this.log, 0, 4);
			layout.SetColumnSpan(this.log, 2);
			layout.Controls.Add(this.status, 0, 5);
			layout.Controls.Add(this.convertButton, 1, 5);
			this.Controls.Add(layout);

			this.addFilesButton.Click += this.AddFiles;
			this.addFolderButton.Click += this.AddFolder;
			this.removeButton.Click += this.RemoveSelected;
			this.clearButton.Click += this.ClearFiles;
			this.browseOutputButton.Click += this.BrowseOutput;
			this.convertButton.Click += this.StartConversion;
			this.fileList.SelectedIndexChanged += this.UpdateButtons;
			this.worker.WorkerReportsProgress = true;
			this.worker.DoWork += this.ConvertFiles;
			this.worker.ProgressChanged += this.UpdateProgress;
			this.worker.RunWorkerCompleted += this.ConversionCompleted;
			this.FormClosing += this.ConfirmCloseWhileWorking;

			this.UpdateButtons(null, EventArgs.Empty);
		}

		private void AddFiles(object sender, EventArgs e)
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.Filter = "ArmA P3D files (*.p3d)|*.p3d";
				dialog.Multiselect = true;
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					this.AddSources(dialog.FileNames);
				}
			}
		}

		private void AddFolder(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dialog = new FolderBrowserDialog())
			{
				dialog.Description = "Select a folder containing P3D files";
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					string[] files = Directory.GetFiles(dialog.SelectedPath, "*.p3d", SearchOption.TopDirectoryOnly);
					if (files.Length == 0)
					{
						MessageBox.Show(this, "The selected folder contains no P3D files.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
						return;
					}
					this.AddSources(files);
				}
			}
		}

		private void AddSources(IEnumerable<string> paths)
		{
			foreach (string path in paths)
			{
				string fullPath = Path.GetFullPath(path);
				if (!this.sourceFiles.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
				{
					this.sourceFiles.Add(fullPath);
					this.fileList.Items.Add(fullPath);
				}
			}
			this.UpdateButtons(null, EventArgs.Empty);
		}

		private void RemoveSelected(object sender, EventArgs e)
		{
			List<string> selectedPaths = this.fileList.SelectedItems.Cast<string>().ToList();
			foreach (string path in selectedPaths)
			{
				this.sourceFiles.Remove(path);
				this.fileList.Items.Remove(path);
			}
			this.UpdateButtons(null, EventArgs.Empty);
		}

		private void ClearFiles(object sender, EventArgs e)
		{
			this.sourceFiles.Clear();
			this.fileList.Items.Clear();
			this.UpdateButtons(null, EventArgs.Empty);
		}

		private void BrowseOutput(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dialog = new FolderBrowserDialog())
			{
				dialog.Description = "Select where converted P3D files will be saved";
				if (Directory.Exists(this.outputFolder.Text))
				{
					dialog.SelectedPath = this.outputFolder.Text;
				}
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					this.outputFolder.Text = dialog.SelectedPath;
				}
			}
		}

		private void StartConversion(object sender, EventArgs e)
		{
			if (this.sourceFiles.Count == 0)
			{
				return;
			}
			string destination = this.outputFolder.Text.Trim();
			if (destination.Length > 0 && !Directory.Exists(destination))
			{
				MessageBox.Show(this, "The output folder does not exist.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			this.log.Clear();
			this.progress.Value = 0;
			this.progress.Maximum = this.sourceFiles.Count;
			this.status.Text = "Starting...";
			this.SetControlsEnabled(false);
			this.worker.RunWorkerAsync(new ConversionRequest(this.sourceFiles.ToArray(), destination.Length == 0 ? null : destination));
		}

		private void ConvertFiles(object sender, DoWorkEventArgs e)
		{
			ConversionRequest request = (ConversionRequest)e.Argument;
			ConversionSummary summary = new ConversionSummary();
			TraceListener listener = new CallbackTraceListener(this.AppendLog);
			Trace.Listeners.Add(listener);
			try
			{
				for (int index = 0; index < request.SourceFiles.Length; index++)
				{
					string source = request.SourceFiles[index];
					string destination = request.OutputFolder == null ? null : Path.Combine(request.OutputFolder, Path.GetFileName(source));
					try
					{
						if (Program.convertP3dFile(source, destination, false))
						{
							summary.Converted++;
						}
						else
						{
							summary.NotConverted++;
						}
					}
					catch (Exception exception)
					{
						summary.Failed++;
						Trace.WriteLine(string.Format("Failed to convert '{0}': {1}", source, exception));
					}
					((BackgroundWorker)sender).ReportProgress(index + 1, Path.GetFileName(source));
				}
			}
			finally
			{
				Trace.Listeners.Remove(listener);
				listener.Dispose();
			}
			e.Result = summary;
		}

		private void UpdateProgress(object sender, ProgressChangedEventArgs e)
		{
			this.progress.Value = e.ProgressPercentage;
			this.status.Text = string.Format("Processed {0} of {1}: {2}", e.ProgressPercentage, this.progress.Maximum, e.UserState);
		}

		private void ConversionCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.SetControlsEnabled(true);
			if (e.Error != null)
			{
				this.AppendLog("Batch conversion stopped: " + e.Error.Message + Environment.NewLine);
				this.status.Text = "Conversion stopped.";
				return;
			}

			ConversionSummary summary = (ConversionSummary)e.Result;
			this.status.Text = string.Format("Finished: {0} converted, {1} not converted, {2} failed.", summary.Converted, summary.NotConverted, summary.Failed);
			this.AppendLog(this.status.Text + Environment.NewLine);
		}

		private void AppendLog(string message)
		{
			if (this.IsDisposed)
			{
				return;
			}
			if (this.InvokeRequired)
			{
				this.BeginInvoke(new Action<string>(this.AppendLog), message);
				return;
			}
			this.log.AppendText(message);
		}

		private void SetControlsEnabled(bool enabled)
		{
			this.addFilesButton.Enabled = enabled;
			this.addFolderButton.Enabled = enabled;
			this.removeButton.Enabled = enabled && this.fileList.SelectedItems.Count > 0;
			this.clearButton.Enabled = enabled && this.sourceFiles.Count > 0;
			this.browseOutputButton.Enabled = enabled;
			this.outputFolder.Enabled = enabled;
			this.fileList.Enabled = enabled;
			this.convertButton.Enabled = enabled && this.sourceFiles.Count > 0;
		}

		private void UpdateButtons(object sender, EventArgs e)
		{
			if (!this.worker.IsBusy)
			{
				this.removeButton.Enabled = this.fileList.SelectedItems.Count > 0;
				this.clearButton.Enabled = this.sourceFiles.Count > 0;
				this.convertButton.Enabled = this.sourceFiles.Count > 0;
			}
		}

		private void ConfirmCloseWhileWorking(object sender, FormClosingEventArgs e)
		{
			if (this.worker.IsBusy && MessageBox.Show(this, "A batch is still running. Close anyway?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
			{
				e.Cancel = true;
			}
		}

		private sealed class ConversionRequest
		{
			public ConversionRequest(string[] sourceFiles, string outputFolder)
			{
				this.SourceFiles = sourceFiles;
				this.OutputFolder = outputFolder;
			}

			public string[] SourceFiles { get; private set; }
			public string OutputFolder { get; private set; }
		}

		private sealed class ConversionSummary
		{
			public int Converted;
			public int NotConverted;
			public int Failed;
		}

		private sealed class CallbackTraceListener : TraceListener
		{
			private readonly Action<string> callback;

			public CallbackTraceListener(Action<string> callback)
			{
				this.callback = callback;
			}

			public override void Write(string message)
			{
				this.callback(message);
			}

			public override void WriteLine(string message)
			{
				this.callback(message + Environment.NewLine);
			}
		}
	}
}
