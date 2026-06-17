using RandomSampleGenerator.Core.Constants;
using RandomSampleGenerator.Core.Models;
using RandomSampleGenerator.Core.Services;
using System.Diagnostics;

namespace RandomSampleGenerator.App;

public partial class MainForm : Form
{
    private readonly ConfigurationService _configService;
    private readonly ValidationService _validationService = new();
    private AppConfiguration _config;

    private readonly StemRowControl[] _stemRows;
    private Button _runButton = null!;
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
        Controls.Add(headerPanel);

        // Stem rows
        for (int i = 0; i < StemTypes.Ordered.Length; i++)
        {
            var stemType = StemTypes.Ordered[i];
            var row = new StemRowControl(stemType) { Top = 40 + i * 38, Left = 10 };
            row.QuantityChanged += OnQuantityChanged;
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
        }

        UpdateRunButtonState();
    }

    private void OnQuantityChanged(object? sender, EventArgs e)
    {
        UpdateRunButtonState();
    }

    private void UpdateRunButtonState()
    {
        _runButton.Enabled = _stemRows.Any(r => r.Quantity > 0);
    }

    private void OnRunClick(object? sender, EventArgs e)
    {
        SaveConfigFromUI();

        var runConfiguration = BuildRunConfiguration();
        var errors = _validationService.ValidateBeforeRun(runConfiguration);
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors),
                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var orchestrator = new RunOrchestrator(
                new SourcePoolScanner(),
                new RunFolderService(),
                _validationService,
                new ManifestBuilder(),
                new SampleExportService(),
                new ExportFileNameBuilder());

            var result = orchestrator.Run(runConfiguration);

            if (_config.AutoOpenOutputFolder && Directory.Exists(result.RunFolderPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.RunFolderPath,
                    UseShellExecute = true
                });
            }

            MessageBox.Show($"Run {result.Status}.\nOutput: {result.RunFolderPath}",
                "Run Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Run failed: {ex.Message}",
                "Run Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private RunConfiguration BuildRunConfiguration()
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

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        using var settingsForm = new SettingsForm(_config);
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            _config = settingsForm.UpdatedConfig;
            _configService.Save(_config);
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
