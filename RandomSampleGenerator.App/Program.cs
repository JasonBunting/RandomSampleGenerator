using RandomSampleGenerator.Core.Services;

namespace RandomSampleGenerator.App;

static class Program
{
    internal static string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RandomSampleGenerator", "config.json");

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configService = new ConfigurationService(ConfigPath);
        var config = configService.LoadOrDefault();

        // First-run setup: prompt for source and target folders if not configured
        if (string.IsNullOrWhiteSpace(config.SourceFolderPath) || !Directory.Exists(config.SourceFolderPath))
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select Source Music Folder",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("A source folder is required. The application will now exit.",
                    "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            config.SourceFolderPath = dlg.SelectedPath;
        }

        if (string.IsNullOrWhiteSpace(config.TargetFolderPath) || !Directory.Exists(config.TargetFolderPath))
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select Target Output Folder",
                UseDescriptionForTitle = true
            };

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("A target folder is required. The application will now exit.",
                    "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verify target folder is writable
            if (!FolderValidation.IsFolderWritable(dlg.SelectedPath))
            {
                MessageBox.Show("The selected target folder is not writable. The application will now exit.",
                    "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            config.TargetFolderPath = dlg.SelectedPath;
        }

        configService.Save(config);
        Application.Run(new MainForm(configService, config));
    }
}