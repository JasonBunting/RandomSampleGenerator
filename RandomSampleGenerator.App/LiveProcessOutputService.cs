using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.App;

public sealed class LiveProcessOutputService : IProcessOutputSink
{
	 private readonly Lock _windowLock = new();
	 private ProcessOutputWindow? _window;

	 public void AppendSystem(string message) => Append("[system]", message);

	 public void AppendStdOut(string message) => Append("[stdout]", message);

	 public void AppendStdErr(string message) => Append("[stderr]", message);

	 private void Append(string prefix, string message)
	 {
		  if (string.IsNullOrWhiteSpace(message))
		  {
				return;
		  }

		  var window = GetOrCreateWindow();
		  window.ShowOrBringToFront();
		  window.AppendLine($"{DateTimeOffset.Now:HH:mm:ss} {prefix} {message}");
	 }

	 private ProcessOutputWindow GetOrCreateWindow()
	 {
		  lock (_windowLock)
		  {
				if (_window is null || _window.IsDisposed)
				{
					 _window = new ProcessOutputWindow();
				}

				return _window;
		  }
	 }
}
