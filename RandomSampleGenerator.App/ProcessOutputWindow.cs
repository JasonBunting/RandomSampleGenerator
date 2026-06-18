namespace RandomSampleGenerator.App;

public sealed class ProcessOutputWindow : Form
{
	 private readonly TextBox _outputTextBox;

	 public ProcessOutputWindow()
	 {
		  Text = "Demucs Live Output";
		  Width = 980;
		  Height = 560;
		  StartPosition = FormStartPosition.CenterScreen;

		  _outputTextBox = new TextBox
		  {
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Both,
				WordWrap = false,
				Dock = DockStyle.Fill,
				Font = new Font("Consolas", 9F),
				HideSelection = false
		  };

		  Controls.Add(_outputTextBox);
	 }

	 public void AppendLine(string line)
	 {
		  if (IsDisposed)
		  {
				return;
		  }

		  if (InvokeRequired)
		  {
				BeginInvoke(() => AppendLine(line));
				return;
		  }

		  _outputTextBox.AppendText(line + Environment.NewLine);
	 }

	 public void ShowOrBringToFront(IWin32Window? owner = null)
	 {
		  if (IsDisposed)
		  {
				return;
		  }

		  if (InvokeRequired)
		  {
				BeginInvoke(() => ShowOrBringToFront(owner));
				return;
		  }

		  if (!Visible)
		  {
				Show(owner);
		  }
		  else
		  {
				BringToFront();
				Activate();
		  }
	 }

	 public void EnsureVisible(IWin32Window? owner = null)
	 {
		  if (IsDisposed)
		  {
				return;
		  }

		  if (InvokeRequired)
		  {
				BeginInvoke(() => EnsureVisible(owner));
				return;
		  }

		  if (!Visible)
		  {
				Show(owner);
		  }
	 }
}
