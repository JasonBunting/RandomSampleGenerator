namespace RandomSampleGenerator.Core.Models;

public sealed class PreflightValidationResult
{
	 public IReadOnlyList<string> Errors { get; init; } = [];

	 public IReadOnlyList<string> Warnings { get; init; } = [];

	 public bool IsValid => Errors.Count == 0;
}
