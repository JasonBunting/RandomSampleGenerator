using System.ComponentModel;
using RandomSampleGenerator.Core.Constants;

namespace RandomSampleGenerator.App;

public sealed class StemRowControl : Panel
{
    private readonly Label _stemLabel;
    private readonly ComboBox _modelCombo;
    private readonly NumericUpDown _quantityInput;
    private readonly NumericUpDown _chunkLengthInput;
    private readonly NumericUpDown _sampleLengthInput;
    private readonly Label _statusLabel;

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
            DecimalPlaces = 0
        };
        _quantityInput.ValueChanged += (s, e) =>
        {
            UpdateStatus();
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
            DecimalPlaces = 0
        };
        _chunkLengthInput.ValueChanged += (s, e) =>
        {
            // Ensure sample length <= chunk length
            _sampleLengthInput!.Maximum = _chunkLengthInput.Value;
            if (_sampleLengthInput.Value > _chunkLengthInput.Value)
                _sampleLengthInput.Value = _chunkLengthInput.Value;
        };

        _sampleLengthInput = new NumericUpDown
        {
            Left = 380,
            Top = 4,
            Width = 75,
            Minimum = 1,
            Maximum = 10, // Initially constrained by chunk length default of 10
            Value = 1,
            DecimalPlaces = 0
        };

        _statusLabel = new Label
        {
            Left = 470,
            Top = 7,
            Width = 80,
            Text = "Skipped",
            ForeColor = Color.Gray
        };

        Controls.Add(_stemLabel);
        Controls.Add(_modelCombo);
        Controls.Add(_quantityInput);
        Controls.Add(_chunkLengthInput);
        Controls.Add(_sampleLengthInput);
        Controls.Add(_statusLabel);

        UpdateStatus();
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

    private void UpdateStatus()
    {
        if (_quantityInput.Value == 0)
        {
            _statusLabel.Text = "Skipped";
            _statusLabel.ForeColor = Color.Gray;
        }
        else
        {
            _statusLabel.Text = "Idle";
            _statusLabel.ForeColor = Color.Black;
        }
    }

    public void SetEnabled(bool enabled)
    {
        _modelCombo.Enabled = enabled;
        _quantityInput.Enabled = enabled;
        _chunkLengthInput.Enabled = enabled;
        _sampleLengthInput.Enabled = enabled;
    }
}
