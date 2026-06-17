using System.Diagnostics;

namespace RandomSampleGenerator.Core.Services;

public sealed class DefaultProcessRunner : IProcessRunner
{
	 public Process Start(ProcessStartInfo startInfo)
	 {
		  var process = Process.Start(startInfo);
		  if (process is null)
		  {
				throw new InvalidOperationException("Failed to start process.");
		  }

		  return process;
	 }
}
