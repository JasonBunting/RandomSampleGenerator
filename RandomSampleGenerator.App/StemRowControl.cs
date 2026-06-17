using System.ComponentModel;
using RandomSampleGenerator.Core.Constants;

namespace RandomSampleGenerator.App;

public sealed class StemRowControl : Panel
{
    public enum RowVisualState
    {
        Skipped,
        Idle,
        Active,
        Completed,
        Partial,
        Failed,
        Cancelled
    }

    private readonly Label _stemLabel;
    private readonly ComboBox _modelCombo;
    private readonly NumericUpDown _quantityInput;
    private readonly NumericUpDown _chunkLengthInput;
    private readonly NumericUpDown _sampleLengthInput;
    private readonly Label _statusLabel;
    private readonly Label _progressLabel;

    private int _producedCount;
    private int _requestedCount;

    public event EventHandler? QuantityChanged;

    public string StemType { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SelectedModel
    {
        get => _modelCombo.SelectedItem?.ToString() ?? string.Empty;
        set
        {
            var idx = _modelCombo.Items.IndexOf(value);
            if (idx >= 0) _modelCombo.SelectedIndex = idx;
        }
    }

    public int Quantity => (int)_quantityInput.Value;

    public int CandidateChunkLengthSeconds => (int)_chunkLengthInput.Value;

    public int FinalSampleLengthSeconds => (int)_sampleLengthInput.Value;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RowVisualState CurrentState { get; private set; }

    public StemRowControl(string stemType)
    {
        StemType = stemType;
        Height = 34;
        Width = 760;

        _stemLabel = new Label
        {
            Text = stemType,
            Left = 0,
            Top = 7,
            Width = 70
        };

        _modelCombo = new ComboBox
        {
            Left = 80,
            Top = 4,
            Width = 130,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        PopulateModels();

        _quantityInput = new NumericUpDown
        {
            Left = 220,
            Top = 4,
            Width = 60,
            Minimum = 0,
            Maximum = 99,
            Value = 0,
            DecimalPlaces = 0,
            ThousandsSeparator = false
        };
        _quantityInput.ValueChanged += (s, e) =>
        {
            _requestedCount = Quantity;
            _producedCount = 0;
            SetStateFromQuantity();
            UpdateProgressLabel();
            QuantityChanged?.Invoke(this, EventArgs.Empty);
        };

        _chunkLengthInput = new NumericUpDown
        {
            Left = 295,
            Top = 4,
            Width = 70,
            Minimum = 10,
            Maximum = 30,
            Value = 10,
            DecimalPlaces = 0,
            ReadOnly = true
        };

        _sampleLengthInput = new NumericUpDown
        {
            Left = 380,
            Top = 4,
            Width = 75,
            Minimum = 1,
            Maximum = 10, // Initially constrained by chunk length default of 10
            Value = 1,
            DecimalPlaces = 0,
            ReadOnly = true
        };

        _chunkLengthInput.ValueChanged += (s, e) =>
        {
            // Ensure sample length <= chunk length
            _sampleLengthInput.Maximum = _chunkLengthInput.Value;
            if (_sampleLengthInput.Value > _chunkLengthInput.Value)
                _sampleLengthInput.Value = _chunkLengthInput.Value;
        };

        _statusLabel = new Label
        {
            Left = 470,
            Top = 7,
            Width = 80,
            Text = "Skipped",
            ForeColor = Color.Gray
        };

        _progressLabel = new Label
        {
            Left = 560,
            Top = 7,
            Width = 120,
            Text = "0/0",
            ForeColor = Color.DimGray
        };

        Controls.Add(_stemLabel);
        Controls.Add(_modelCombo);
        Controls.Add(_quantityInput);
        Controls.Add(_chunkLengthInput);
        Controls.Add(_sampleLengthInput);
        Controls.Add(_statusLabel);
        Controls.Add(_progressLabel);

        _requestedCount = Quantity;
        _producedCount = 0;
        SetStateFromQuantity();
        UpdateProgressLabel();
    }

    private void PopulateModels()
    {
        foreach (var kvp in StemTypes.SupportedModels)
        {
            if (StemTypes.ModelSupportsStem(kvp.Key, StemType))
            {
                _modelCombo.Items.Add(kvp.Key);
            }
        }

        if (_modelCombo.Items.Count > 0)
            _modelCombo.SelectedIndex = 0;
    }

    private void SetStateFromQuantity()
    {
        SetVisualState(Quantity == 0 ? RowVisualState.Skipped : RowVisualState.Idle);
    }

    private void UpdateProgressLabel()
    {
        _progressLabel.Text = $"{_producedCount}/{_requestedCount}";
    }

    public void SetVisualState(RowVisualState state)
    {
        CurrentState = state;

        switch (state)
        {
            case RowVisualState.Skipped:
                _statusLabel.Text = "Skipped";
                _statusLabel.ForeColor = Color.Gray;
                break;
            case RowVisualState.Idle:
                _statusLabel.Text = "Idle";
                _statusLabel.ForeColor = Color.Black;
                break;
            case RowVisualState.Active:
                _statusLabel.Text = "Active";
                _statusLabel.ForeColor = Color.RoyalBlue;
                break;
            case RowVisualState.Completed:
                _statusLabel.Text = "Completed";
                _statusLabel.ForeColor = Color.ForestGreen;
                break;
            case RowVisualState.Partial:
                _statusLabel.Text = "Partial";
                _statusLabel.ForeColor = Color.DarkGoldenrod;
                break;
            case RowVisualState.Failed:
                _statusLabel.Text = "Failed";
                _statusLabel.ForeColor = Color.Firebrick;
                break;
            case RowVisualState.Cancelled:
                _statusLabel.Text = "Cancelled";
                _statusLabel.ForeColor = Color.DarkSlateGray;
                break;
        }
    }

    public void SetProgress(int producedCount, int requestedCount)
    {
        _producedCount = Math.Max(0, producedCount);
        _requestedCount = Math.Max(0, requestedCount);
        UpdateProgressLabel();
    }

    public void SetEnabled(bool enabled)
    {
        _modelCombo.Enabled = enabled;
        _quantityInput.Enabled = enabled;
        _chunkLengthInput.Enabled = enabled;
        _sampleLengthInput.Enabled = enabled;
    }
}
