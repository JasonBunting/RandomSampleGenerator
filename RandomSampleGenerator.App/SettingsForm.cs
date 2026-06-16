using RandomSampleGenerator.Core.Models;

namespace RandomSampleGenerator.App;

public sealed class SettingsForm : Form
{
    private readonly TextBox _sourceFolderBox;
    private readonly TextBox _targetFolderBox;
    private readonly Button _browseSourceButton;
    private readonly Button _browseTargetButton;
    private readonly NumericUpDown _sampleRateInput;
    private readonly ComboBox _bitDepthCombo;
    private readonly CheckBox _autoOpenCheckBox;
    private readonly CheckBox _loggingCheckBox;
    private readonly NumericUpDown _maxStemTypesInput;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public AppConfiguration UpdatedConfig { get; private set; }

    public SettingsForm(AppConfiguration config)
    {
        UpdatedConfig = config;

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 340);

        int y = 15;
        const int labelX = 15;
        const int inputX = 200;
        const int rowHeight = 32;

        // Source folder
        Controls.Add(new Label { Text = "Source Folder:", Left = labelX, Top = y + 4, Width = 170, AutoSize = false });
        _sourceFolderBox = new TextBox { Left = inputX, Top = y, Width = 240, ReadOnly = true, Text = config.SourceFolderPath };
        Controls.Add(_sourceFolderBox);
        _browseSourceButton = new Button { Text = "Browse...", Left = 450, Top = y - 1, Width = 55, Height = 25 };
        _browseSourceButton.Click += OnBrowseSource;
        Controls.Add(_browseSourceButton);

        y += rowHeight;

        // Target folder
        Controls.Add(new Label { Text = "Target Folder:", Left = labelX, Top = y + 4, Width = 170, AutoSize = false });
        _targetFolderBox = new TextBox { Left = inputX, Top = y, Width = 240, ReadOnly = true, Text = config.TargetFolderPath };
        Controls.Add(_targetFolderBox);
        _browseTargetButton = new Button { Text = "Browse...", Left = 450, Top = y - 1, Width = 55, Height = 25 };
        _browseTargetButton.Click += OnBrowseTarget;
        Controls.Add(_browseTargetButton);

        y += rowHeight + 10;

        // Sample rate
        Controls.Add(new Label { Text = "Export Sample Rate (Hz):", Left = labelX, Top = y + 4, Width = 170, AutoSize = false });
        _sampleRateInput = new NumericUpDown { Left = inputX, Top = y, Width = 100, Minimum = 8000, Maximum = 192000, Value = config.ExportSampleRate, Increment = 100 };
        Controls.Add(_sampleRateInput);

        y += rowHeight;

        // Bit depth
        Controls.Add(new Label { Text = "Export Bit Depth:", Left = labelX, Top = y + 4, Width = 170, AutoSize = false });
        _bitDepthCombo = new ComboBox { Left = inputX, Top = y, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        _bitDepthCombo.Items.AddRange(new object[] { 16, 24, 32 });
        _bitDepthCombo.SelectedItem = config.ExportBitDepth;
        if (_bitDepthCombo.SelectedIndex < 0) _bitDepthCombo.SelectedIndex = 0;
        Controls.Add(_bitDepthCombo);

        y += rowHeight;

        // Export format (read-only for v1)
        Controls.Add(new Label { Text = "Export Format:", Left = labelX, Top = y + 4, Width = 170, AutoSize = false });
        Controls.Add(new Label { Text = "WAV (v1 only)", Left = inputX, Top = y + 4, Width = 150, ForeColor = Color.Gray });

        y += rowHeight + 10;

        // Auto-open output folder
        _autoOpenCheckBox = new CheckBox { Text = "Auto-open output folder after run", Left = labelX, Top = y, Width = 300, Checked = config.AutoOpenOutputFolder };
        Controls.Add(_autoOpenCheckBox);

        y += rowHeight;

        // Logging enabled
        _loggingCheckBox = new CheckBox { Text = "Enable logging", Left = labelX, Top = y, Width = 300, Checked = config.LoggingEnabled };
        Controls.Add(_loggingCheckBox);

        y += rowHeight;

        // Max distinct stem types per song per run
        Controls.Add(new Label { Text = "Max stem types per song/run:", Left = labelX, Top = y + 4, Width = 170, AutoSize = false });
        _maxStemTypesInput = new NumericUpDown { Left = inputX, Top = y, Width = 60, Minimum = 1, Maximum = 6, Value = config.MaxDistinctStemTypesPerSongPerRun };
        Controls.Add(_maxStemTypesInput);

        y += rowHeight + 15;

        // OK / Cancel
        _okButton = new Button { Text = "OK", Left = 320, Top = y, Width = 80, Height = 30, DialogResult = DialogResult.OK };
        _okButton.Click += OnOk;
        Controls.Add(_okButton);

        _cancelButton = new Button { Text = "Cancel", Left = 410, Top = y, Width = 80, Height = 30, DialogResult = DialogResult.Cancel };
        Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        ClientSize = new Size(520, y + 45);
    }

    private void OnBrowseSource(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select Source Music Folder",
            UseDescriptionForTitle = true,
            SelectedPath = _sourceFolderBox.Text
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _sourceFolderBox.Text = dlg.SelectedPath;
        }
    }

    private void OnBrowseTarget(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select Target Output Folder",
            UseDescriptionForTitle = true,
            SelectedPath = _targetFolderBox.Text
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            // Verify writability
            var probePath = Path.Combine(dlg.SelectedPath, $".{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(probePath, "probe");
                File.Delete(probePath);
                _targetFolderBox.Text = dlg.SelectedPath;
            }
            catch
            {
                MessageBox.Show("The selected folder is not writable.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnOk(object? sender, EventArgs e)
    {
        if (!Directory.Exists(_sourceFolderBox.Text))
        {
            MessageBox.Show("Source folder does not exist.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.None;
            return;
        }

        if (!Directory.Exists(_targetFolderBox.Text))
        {
            MessageBox.Show("Target folder does not exist.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.None;
            return;
        }

        UpdatedConfig = new AppConfiguration
        {
            SourceFolderPath = _sourceFolderBox.Text,
            TargetFolderPath = _targetFolderBox.Text,
            ExportSampleRate = (int)_sampleRateInput.Value,
            ExportBitDepth = (int)_bitDepthCombo.SelectedItem!,
            ExportFormat = "wav",
            AutoOpenOutputFolder = _autoOpenCheckBox.Checked,
            LoggingEnabled = _loggingCheckBox.Checked,
            MaxDistinctStemTypesPerSongPerRun = (int)_maxStemTypesInput.Value,
            ModelByStemType = new Dictionary<string, string>(UpdatedConfig.ModelByStemType, StringComparer.OrdinalIgnoreCase)
        };
    }
}
