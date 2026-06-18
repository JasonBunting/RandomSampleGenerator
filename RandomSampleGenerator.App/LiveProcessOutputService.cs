using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.App;

public sealed class LiveProcessOutputService : IProcessOutputSink
{
	 private readonly Control _uiInvoker;
	 private ProcessOutputWindow? _window;
	 private bool _hasBeenShown;

	 public LiveProcessOutputService(Control uiInvoker)
	 {
		  _uiInvoker = uiInvoker;
	 }

	 public void AppendSystem(string message) => Append("[system]", message);

	 public void AppendStdOut(string message) => Append("[stdout]", message);

	 public void AppendStdErr(string message) => Append("[stderr]", message);

	 private void Append(string prefix, string message)
	 {
		  if (string.IsNullOrWhiteSpace(message))
		  {
				return;
		  }

		  if (_uiInvoker.IsDisposed)
		  {
				return;
		  }

		  if (_uiInvoker.InvokeRequired)
		  {
				_uiInvoker.BeginInvoke(() => Append(prefix, message));
				return;
		  }

		  var window = GetOrCreateWindow();
		  if (!_hasBeenShown)
		  {
				window.ShowOrBringToFront();
				_hasBeenShown = true;
		  }
		  else
		  {
				window.EnsureVisible();
		  }
		  window.AppendLine($"{DateTimeOffset.Now:HH:mm:ss} {prefix} {message}");
	 }

	 private ProcessOutputWindow GetOrCreateWindow()
	 {
		  if (_window is null || _window.IsDisposed)
		  {
				_window = new ProcessOutputWindow();
				_hasBeenShown = false;
		  }

		  return _window;
	 }
}
