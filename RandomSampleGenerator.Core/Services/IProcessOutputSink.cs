namespace RandomSampleGenerator.Core.Services;

public interface IProcessOutputSink
{
	 void AppendSystem(string message);

	 void AppendStdOut(string message);

	 void AppendStdErr(string message);
}
