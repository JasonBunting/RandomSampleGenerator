using System.Diagnostics;

namespace RandomSampleGenerator.Core.Services;

public interface IProcessRunner
{
	 Process Start(ProcessStartInfo startInfo);
}
