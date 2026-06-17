using RandomSampleGenerator.Core.Constants;
using RandomSampleGenerator.Core.Models;
using RandomSampleGenerator.Core.Services;
using System.Diagnostics;

namespace RandomSampleGenerator.App;

public partial class MainForm : Form
{
    private readonly ConfigurationService _configService;
    private readonly SourcePoolScanner _sourcePoolScanner = new();
    private readonly ValidationService _validationService = new();
    private AppConfiguration _config;
    private bool _isRunInProgress;
    private IReadOnlyList<string> _currentSourcePool = [];
    private CancellationTokenSource? _runCancellationTokenSource;

    private readonly StemRowControl[] _stemRows;
    private Button _runButton = null!;
    private Button _cancelButton = null!;
    private Button _settingsButton = null!;

    public MainForm(ConfigurationService configService, AppConfiguration config)
    {
        _configService = configService;
        _config = config;
        _stemRows = new StemRowControl[StemTypes.Ordered.Length];

        InitializeComponent();
        BuildUI();
        LoadConfigIntoUI();
    }

    private void BuildUI()
    {
        Text = "Random Sample Generator";
        ClientSize = new Size(780, 340);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // Header row
        var headerPanel = new Panel { Top = 10, Left = 10, Width = 760, Height = 25 };
        headerPanel.Controls.Add(new Label { Text = "Stem", Left = 0, Top = 4, Width = 70, Font = new Font(Font, FontStyle.Bold) });
        headerPanel.Controls.Add(new Label { Text = "Model", Left = 80, Top = 4, Width = 120, Font = new Font(Font, FontStyle.Bold) });
        headerPanel.Controls.Add(new Label { Text = "Quantity", Left = 220, Top = 4, Width = 60, Font = new Font(Font, FontStyle.Bold) });
        headerPanel.Controls.Add(new Label { Text = "Chunk (s)", Left = 295, Top = 4, Width = 70, Font = new Font(Font, FontStyle.Bold) });
        headerPanel.Controls.Add(new Label { Text = "Sample (s)", Left = 380, Top = 4, Width = 75, Font = new Font(Font, FontStyle.Bold) });
        headerPanel.Controls.Add(new Label { Text = "Status", Left = 470, Top = 4, Width = 60, Font = new Font(Font, FontStyle.Bold) });
        headerPanel.Controls.Add(new Label { Text = "Progress", Left = 560, Top = 4, Width = 70, Font = new Font(Font, FontStyle.Bold) });
        Controls.Add(headerPanel);

        // Stem rows
        for (int i = 0; i < StemTypes.Ordered.Length; i++)
        {
            var stemType = StemTypes.Ordered[i];
            var row = new StemRowControl(stemType) { Top = 40 + i * 38, Left = 10 };
            row.QuantityChanged += OnQuantityChanged;
            row.ModelChanged += OnModelChanged;
            Controls.Add(row);
            _stemRows[i] = row;
        }

        // Buttons
        int buttonTop = 40 + StemTypes.Ordered.Length * 38 + 15;

        _runButton = new Button
        {
            Text = "Run",
            Left = 10,
            Top = buttonTop,
            Width = 100,
            Height = 32,
            Enabled = false
        };
        _runButton.Click += OnRunClick;
        Controls.Add(_runButton);

        _settingsButton = new Button
        {
            Text = "Settings...",
            Left = 120,
            Top = buttonTop,
            Width = 100,
            Height = 32
        };
        _settingsButton.Click += OnSettingsClick;
        Controls.Add(_settingsButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Left = 230,
            Top = buttonTop,
            Width = 100,
            Height = 32,
            Enabled = false
        };
        _cancelButton.Click += OnCancelClick;
        Controls.Add(_cancelButton);

        ClientSize = new Size(780, buttonTop + 50);
    }

    private void LoadConfigIntoUI()
    {
        foreach (var row in _stemRows)
        {
            if (_config.ModelByStemType.TryGetValue(row.StemType, out var model))
            {
                row.SelectedModel = model;
            }

            row.SetProgress(0, row.Quantity);
            row.SetVisualState(row.Quantity == 0
                ? StemRowControl.RowVisualState.Skipped
                : StemRowControl.RowVisualState.Idle);
            row.ConfirmCurrentModelSelection();
        }

        UpdateRunButtonState();
    }

    private void OnQuantityChanged(object? sender, EventArgs e)
    {
        UpdateRunButtonState();
    }

    private void UpdateRunButtonState()
    {
        _runButton.Enabled = !_isRunInProgress && _stemRows.Any(r => r.Quantity > 0);
    }

    private void OnRunClick(object? sender, EventArgs e)
    {
        SaveConfigFromUI();

        var runConfiguration = BuildRunConfigurationFromUI();
        var preflight = ValidatePreflight(runConfiguration);
        if (!preflight.IsValid)
        {
            MessageBox.Show(string.Join(Environment.NewLine, preflight.Errors),
                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (preflight.Warnings.Count > 0)
        {
            var warningMessage = string.Join(Environment.NewLine, preflight.Warnings) + Environment.NewLine + Environment.NewLine + "Continue anyway?";
            var choice = MessageBox.Show(warningMessage, "Preflight Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (choice != DialogResult.Yes)
            {
                return;
            }
        }

        EnterRunMode();
        _ = ExecuteRunAsync(runConfiguration);
    }

    private void EnterRunMode()
    {
        _isRunInProgress = true;

        var activeAssigned = false;
        foreach (var row in _stemRows)
        {
            row.SetProgress(0, row.Quantity);
            if (row.Quantity == 0)
            {
                row.SetVisualState(StemRowControl.RowVisualState.Skipped);
                continue;
            }

            if (!activeAssigned)
            {
                row.SetVisualState(StemRowControl.RowVisualState.Active);
                activeAssigned = true;
            }
            else
            {
                row.SetVisualState(StemRowControl.RowVisualState.Idle);
            }
        }

        SetRunInputLockState(isRunning: true);
    }

    private void SetRunInputLockState(bool isRunning)
    {
        foreach (var row in _stemRows)
        {
            row.SetEnabled(!isRunning);
        }

        _settingsButton.Enabled = !isRunning;
        _cancelButton.Enabled = isRunning;
        UpdateRunButtonState();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        if (!_isRunInProgress)
        {
            return;
        }

        var confirm = MessageBox.Show("Cancel the current run?", "Confirm Cancel",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _runCancellationTokenSource?.Cancel();
    }

    private async Task ExecuteRunAsync(RunConfiguration runConfiguration)
    {
        foreach (var row in _stemRows)
        {
            row.SetProgress(0, row.Quantity);
        }

        var orchestrator = new RunOrchestrator(
            new RunFolderService(),
            _validationService,
            new CandidateChunkService(),
            new StemSeparationService(),
            new SampleExportService(),
            new ExportFileNameBuilder(),
            new ManifestBuilder());
        _runCancellationTokenSource = new CancellationTokenSource();

        try
        {
            var result = await Task.Run(() => orchestrator.Run(
                runConfiguration,
                _currentSourcePool,
                _runCancellationTokenSource.Token,
                update => BeginInvoke(() => ApplyRowProgressUpdate(update))));

            ApplyFinalRowStates(result.RowResults);
            ShowRunCompletionMessage(result);
            if (result.Status == RunStatus.Completed && _config.AutoOpenOutputFolder)
            {
                TryOpenOutputFolder(result.RunFolderPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Run failed: {ex.Message}", "Run Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _runCancellationTokenSource?.Dispose();
            _runCancellationTokenSource = null;
            _isRunInProgress = false;
            SetRunInputLockState(isRunning: false);
        }
    }

    private RunConfiguration BuildRunConfigurationFromUI()
    {
        return new RunConfiguration
        {
            AppConfiguration = _config,
            StemRows =
            [
                .. _stemRows.Select(row => new StemRowConfiguration
                {
                    StemType = row.StemType,
                    Model = row.SelectedModel,
                    Quantity = row.Quantity,
                    CandidateChunkLengthSeconds = row.CandidateChunkLengthSeconds,
                    FinalSampleLengthSeconds = row.FinalSampleLengthSeconds
                })
            ]
        };
    }

    private void ApplyRowProgressUpdate(RowProgressUpdate update)
    {
        var row = _stemRows.FirstOrDefault(candidate => candidate.StemType.Equals(update.StemType, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        row.SetProgress(update.ProducedCount, update.RequestedCount);

        if (update.FinalStatus.HasValue)
        {
            row.SetVisualState(ToVisualState(update.FinalStatus.Value));
            return;
        }

        foreach (var candidate in _stemRows.Where(item => item.Quantity > 0 && item != row && item.CurrentState == StemRowControl.RowVisualState.Active))
        {
            candidate.SetVisualState(StemRowControl.RowVisualState.Idle);
        }

        if (row.Quantity > 0)
        {
            row.SetVisualState(StemRowControl.RowVisualState.Active);
        }
    }

    private void ApplyFinalRowStates(IReadOnlyList<RowResult> rowResults)
    {
        foreach (var result in rowResults)
        {
            var row = _stemRows.FirstOrDefault(candidate => candidate.StemType.Equals(result.StemType, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                continue;
            }

            row.SetProgress(result.ProducedCount, result.RequestedCount);
            row.SetVisualState(ToVisualState(result.Status));
        }
    }

    private static StemRowControl.RowVisualState ToVisualState(RowStatus status) => status switch
    {
        RowStatus.Skipped => StemRowControl.RowVisualState.Skipped,
        RowStatus.Completed => StemRowControl.RowVisualState.Completed,
        RowStatus.Partial => StemRowControl.RowVisualState.Partial,
        RowStatus.Failed => StemRowControl.RowVisualState.Failed,
        RowStatus.Cancelled => StemRowControl.RowVisualState.Cancelled,
        _ => StemRowControl.RowVisualState.Idle
    };

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        using var settingsForm = new SettingsForm(_config);
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            _config = settingsForm.UpdatedConfig;
            _configService.Save(_config);
        }
    }

    private void OnModelChanged(object? sender, ModelChangedEventArgs e)
    {
        if (_isRunInProgress)
        {
            return;
        }

        if (sender is not StemRowControl row)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(e.PreviousModel))
        {
            row.ConfirmCurrentModelSelection();
            return;
        }

        var confirm = MessageBox.Show(
            $"Change model for '{row.StemType}' from '{e.PreviousModel}' to '{e.CurrentModel}'?",
            "Confirm Model Change",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            row.ConfirmCurrentModelSelection();
            _config.ModelByStemType[row.StemType] = row.SelectedModel;
            _configService.Save(_config);
        }
        else
        {
            row.RevertModelSelection();
        }
    }

    private PreflightValidationResult ValidatePreflight(RunConfiguration runConfiguration)
    {
        try
        {
            _currentSourcePool = _sourcePoolScanner.Scan(_config.SourceFolderPath);
        }
        catch (Exception ex)
        {
            return new PreflightValidationResult
            {
                Errors = [$"Configured source folder is inaccessible: {ex.Message}"],
                Warnings = []
            };
        }

        return _validationService.ValidatePreflight(runConfiguration, _currentSourcePool.Count);
    }

    private void ShowRunCompletionMessage(RunResult result)
    {
        var text = result.Status switch
        {
            RunStatus.Completed => $"Run completed. Output folder:{Environment.NewLine}{result.RunFolderPath}",
            RunStatus.Cancelled => $"Run cancelled. Partial outputs were kept in:{Environment.NewLine}{result.RunFolderPath}",
            _ => $"Run failed. Available outputs were kept in:{Environment.NewLine}{result.RunFolderPath}"
        };

        var icon = result.Status switch
        {
            RunStatus.Completed => MessageBoxIcon.Information,
            RunStatus.Cancelled => MessageBoxIcon.Warning,
            _ => MessageBoxIcon.Error
        };

        MessageBox.Show(text, "Run Result", MessageBoxButtons.OK, icon);
    }

    private static void TryOpenOutputFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Keep post-run UX best-effort only.
        }
    }

    private void SaveConfigFromUI()
    {
        foreach (var row in _stemRows)
        {
            _config.ModelByStemType[row.StemType] = row.SelectedModel;
        }

        _configService.Save(_config);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveConfigFromUI();
        base.OnFormClosing(e);
    }
}
