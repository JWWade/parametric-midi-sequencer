using Melanchall.DryWetMidi.Core;
using Newtonsoft.Json;
using ParametricMidiSequencer.Midi;
using ParametricMidiSequencer.Models;
using ParametricMidiSequencer.UI.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ParametricMidiSequencer.UI
{
    public partial class MainForm : Form
    {
        private HarmonySpec _currentSpec;
        private MetaSpec _currentMeta;
        private string _currentJsonPath;
        private int? _sequenceLength;

        private BindingList<ProgressionRow> _progressionRows;

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Parametric MIDI Sequencer - PoC12 UI";
            Size = new System.Drawing.Size(1200, 820);
            MinimumSize = new System.Drawing.Size(1000, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = System.Drawing.SystemColors.Control;

            // Create main layout
            var mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 580
            };
            Controls.Add(mainSplitter);

            // Create top layout with left/center/right panels
            var topLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0)
            };
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainSplitter.Panel1.Controls.Add(topLayout);

            // ===== LEFT PANEL =====
            CreateLeftPanel(topLayout);

            // ===== CENTER PANEL =====
            CreateCenterPanel(topLayout);

            // ===== RIGHT PANEL =====
            CreateRightPanel(topLayout);

            // ===== BOTTOM PANEL =====
            CreateBottomPanel(mainSplitter.Panel2);
        }

        private void CreateLeftPanel(Control parent)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            ((TableLayoutPanel)parent).Controls.Add(panel, 0, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = false,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(layout);

            // Title
            var titleLabel = new Label 
            { 
                Text = "JSON Specification", 
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Load button
            var loadButton = new Button
            {
                Text = "Load JSON File",
                Width = 380,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10)
            };
            loadButton.Click += LoadJsonFile_Click;
            loadButton.Dock = DockStyle.Top;
            layout.Controls.Add(loadButton, 0, 1);

            // File path display
            var pathLabel = new Label { Text = "No file loaded", AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            layout.Controls.Add(pathLabel, 0, 2);
            _pathLabel = pathLabel;

            // Summary group
            var summaryLabel = new Label
            {
                Text = "Summary",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 10)
            };
            layout.Controls.Add(summaryLabel, 0, 3);

            // Summary content
            var summaryText = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Font = new System.Drawing.Font("Courier New", 9),
                Margin = new Padding(0),
                BorderStyle = BorderStyle.Fixed3D
            };
            summaryText.Dock = DockStyle.Fill;
            layout.Controls.Add(summaryText, 0, 4);
            _summaryText = summaryText;
        }

        private void CreateCenterPanel(Control parent)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            ((TableLayoutPanel)parent).Controls.Add(panel, 1, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                AutoSize = false,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var titleLabel = new Label
            {
                Text = "Progression Editor",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                Enabled = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(ProgressionRow.Time),
                HeaderText = "Time",
                DataPropertyName = nameof(ProgressionRow.Time),
                Width = 70,
                MinimumWidth = 70
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(ProgressionRow.Degree),
                HeaderText = "Degree",
                DataPropertyName = nameof(ProgressionRow.Degree),
                Width = 70,
                MinimumWidth = 70
            });
            grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = nameof(ProgressionRow.Type),
                HeaderText = "Type",
                DataPropertyName = nameof(ProgressionRow.Type),
                Width = 100,
                MinimumWidth = 90,
                DataSource = new[] { "triad", "seventh" }
            });
            grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = nameof(ProgressionRow.Inversion),
                HeaderText = "Inversion",
                DataPropertyName = nameof(ProgressionRow.Inversion),
                Width = 110,
                MinimumWidth = 100
            });
            grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = nameof(ProgressionRow.BorrowMode),
                HeaderText = "Borrow Mode",
                DataPropertyName = nameof(ProgressionRow.BorrowMode),
                Width = 140,
                MinimumWidth = 120,
                DataSource = BorrowModes
            });
            grid.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = "Remove",
                Text = "Remove",
                UseColumnTextForButtonValue = true,
                Width = 75,
                MinimumWidth = 70
            });

            grid.CellValueChanged += ProgressionGrid_CellValueChanged;
            grid.CellContentClick += ProgressionGrid_CellContentClick;
            grid.DataError += ProgressionGrid_DataError;
            grid.CurrentCellDirtyStateChanged += ProgressionGrid_CurrentCellDirtyStateChanged;
            layout.Controls.Add(grid, 0, 1);
            _progressionGrid = grid;

            var addButton = new Button
            {
                Text = "Add Chord",
                Height = 34,
                Dock = DockStyle.Top,
                Enabled = false,
                Margin = new Padding(0, 10, 0, 0)
            };
            addButton.Click += AddChord_Click;
            layout.Controls.Add(addButton, 0, 2);
            _addChordButton = addButton;

            // ===== Voice-Leading Constraint Section =====
            var constraintSectionLabel = new Label
            {
                Text = "Voice-Leading Constraint",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 6)
            };
            layout.Controls.Add(constraintSectionLabel, 0, 3);

            var constraintRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };

            var minSharedLabel = new Label
            {
                Text = "Minimum shared pitches between chords:",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0)
            };

            var minSharedInput = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3,
                Value = 0,
                Width = 55,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            minSharedInput.ValueChanged += MinSharedPitches_ValueChanged;
            _minSharedPitchesInput = minSharedInput;

            constraintRow.Controls.Add(minSharedLabel);
            constraintRow.Controls.Add(minSharedInput);
            layout.Controls.Add(constraintRow, 0, 4);

            var constraintDescLabel = new Label
            {
                Text = "Higher values create smoother voice-leading. Lower values allow more harmonic contrast.",
                AutoSize = true,
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 0)
            };
            layout.Controls.Add(constraintDescLabel, 0, 5);
        }

        private void CreateRightPanel(Control parent)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            ((TableLayoutPanel)parent).Controls.Add(panel, 2, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                AutoSize = false,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(layout);

            // Title
            var titleLabel = new Label 
            { 
                Text = "MIDI Generation", 
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            // Generate button
            var generateButton = new Button
            {
                Text = "Generate MIDI",
                Width = 380,
                Height = 40,
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 15)
            };
            generateButton.Click += GenerateMidi_Click;
            generateButton.Dock = DockStyle.Top;
            layout.Controls.Add(generateButton, 0, 1);
            _generateButton = generateButton;

            // Output path section
            var outputLabel = new Label
            {
                Text = "Output Path:",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(outputLabel, 0, 2);

            var pathContainer = new Panel { Width = 380, Height = 30, Margin = new Padding(0, 0, 0, 20), Padding = new Padding(0) };
            var outputPath = new TextBox { Dock = DockStyle.Fill, Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "output.mid") };
            var browsePath = new Button { Dock = DockStyle.Right, Text = "…", Width = 30, Margin = new Padding(5, 0, 0, 0) };
            browsePath.Click += BrowseOutputPath_Click;
            pathContainer.Controls.Add(outputPath);
            pathContainer.Controls.Add(browsePath);
            pathContainer.Dock = DockStyle.Top;
            layout.Controls.Add(pathContainer, 0, 3);
            _outputPath = outputPath;

            // Events summary
            var eventsLabel = new Label
            {
                Text = "Generated Events Summary",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(eventsLabel, 0, 4);

            var eventsText = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Font = new System.Drawing.Font("Courier New", 9),
                Margin = new Padding(0),
                BorderStyle = BorderStyle.Fixed3D,
                ScrollBars = ScrollBars.Vertical
            };
            eventsText.Dock = DockStyle.Fill;
            layout.Controls.Add(eventsText, 0, 5);
            _eventsText = eventsText;
        }

        private void CreateBottomPanel(Control parent)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            parent.Controls.Add(panel);

            var bottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(bottomLayout);

            var logLabel = new Label
            {
                Text = "Status Log",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            bottomLayout.Controls.Add(logLabel, 0, 0);

            var logText = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Courier New", 9),
                BorderStyle = BorderStyle.Fixed3D,
                ScrollBars = ScrollBars.Vertical
            };
            bottomLayout.Controls.Add(logText, 0, 1);
            _logText = logText;

            LogMessage("UI initialized. Ready to load JSON file.");
        }

        private void LoadJsonFile_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Load Harmony Specification",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "examples")
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _currentJsonPath = dialog.FileName;
                    var json = File.ReadAllText(_currentJsonPath);
                    
                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                        {
                            NamingStrategy = new Newtonsoft.Json.Serialization.DefaultNamingStrategy()
                        },
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        Converters = { new HarmonySpecConverter() }
                    };

                    // Parse as root object and extract harmony section
                    var rootObject = Newtonsoft.Json.Linq.JObject.Parse(json);

                    _currentMeta = null;
                    _sequenceLength = null;

                    if (rootObject.ContainsKey("meta"))
                    {
                        _currentMeta = rootObject["meta"].ToObject<MetaSpec>(JsonSerializer.Create(settings));
                        if (_currentMeta != null && _currentMeta.Steps > 0 && _currentMeta.Bars > 0)
                            _sequenceLength = _currentMeta.Steps * _currentMeta.Bars;
                    }
                    
                    if (rootObject.ContainsKey("harmony"))
                    {
                        // Standard format with harmony section
                        var harmonyJson = rootObject["harmony"].ToString();
                        _currentSpec = JsonConvert.DeserializeObject<HarmonySpec>(harmonyJson, settings);
                    }
                    else
                    {
                        // Try to parse entire file as HarmonySpec (direct harmony format)
                        _currentSpec = JsonConvert.DeserializeObject<HarmonySpec>(json, settings);
                    }

                    if (_currentSpec != null)
                    {
                        _pathLabel.Text = _currentJsonPath;
                        LoadProgressionEditor();
                        LogMessage($"✓ Loaded: {Path.GetFileName(_currentJsonPath)}");
                    }
                    else
                    {
                        LogMessage("✗ Could not parse JSON as HarmonySpec.");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"✗ Error loading file: {ex.Message}");
                    _summaryText.Text = "";
                }
            }
        }

        private void DisplaySummary()
        {
            if (_currentSpec == null)
                return;

            var summary = HarmonySummaryFormatter.CreateSummary(_currentSpec);
            var text = $@"Scale:
  {summary.ScaleDescription}

Chords:
  {summary.ChordCount} chord events

Transforms:
  {summary.TransformDescription}

Inversions:
  {(summary.HasInversions ? $"Yes ({summary.InversionCount} chords)" : "No")}

Velocity: {summary.Velocity}
Channel: {summary.Channel}
Duration: {summary.Duration}";

            _summaryText.Text = text;
        }

        private void LoadProgressionEditor()
        {
            if (_currentSpec == null)
                return;

            _progressionRows = new BindingList<ProgressionRow>();
            foreach (var chord in _currentSpec.Progression)
            {
                _progressionRows.Add(new ProgressionRow
                {
                    Time = chord.Time,
                    Degree = chord.Degree,
                    Type = string.IsNullOrWhiteSpace(chord.Type) ? "triad" : chord.Type,
                    Inversion = chord.Inversion,
                    BorrowMode = string.IsNullOrWhiteSpace(chord.BorrowMode) ? "none" : chord.BorrowMode
                });
            }

            _progressionGrid.DataSource = _progressionRows;
            foreach (DataGridViewRow row in _progressionGrid.Rows)
                UpdateInversionCell(row);

            _progressionGrid.Enabled = true;
            _addChordButton.Enabled = true;

            var minShared = _currentSpec.Constraints?.MinSharedPitches ?? 0;
            _minSharedPitchesInput.Value = Math.Clamp(minShared, (int)_minSharedPitchesInput.Minimum, (int)_minSharedPitchesInput.Maximum);
            _minSharedPitchesInput.Enabled = true;

            UpdateSummaryAndValidation();
        }

        private void MinSharedPitches_ValueChanged(object sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            _currentSpec.Constraints.MinSharedPitches = (int)_minSharedPitchesInput.Value;
            DisplaySummary();
        }

        private void AddChord_Click(object sender, EventArgs e)
        {
            if (_progressionRows == null)
                return;

            int nextTime = 0;
            if (_progressionRows.Count > 0)
                nextTime = _progressionRows.Max(r => r.Time) + 4;

            if (_sequenceLength.HasValue)
                nextTime = Math.Min(Math.Max(0, nextTime), _sequenceLength.Value - 1);

            _progressionRows.Add(new ProgressionRow
            {
                Time = nextTime,
                Degree = 1,
                Type = "triad",
                Inversion = 0,
                BorrowMode = "none"
            });

            var lastRowIndex = _progressionGrid.Rows.Count - 1;
            if (lastRowIndex >= 0)
                UpdateInversionCell(_progressionGrid.Rows[lastRowIndex]);

            UpdateSummaryAndValidation();
        }

        private void ProgressionGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_progressionGrid.IsCurrentCellDirty)
                _progressionGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void ProgressionGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var row = _progressionGrid.Rows[e.RowIndex];
            if (_progressionGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(ProgressionRow.Type))
                UpdateInversionCell(row);

            UpdateSummaryAndValidation();
        }

        private void ProgressionGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (_progressionGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                _progressionRows.RemoveAt(e.RowIndex);
                UpdateSummaryAndValidation();
            }
        }

        private void ProgressionGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void UpdateInversionCell(DataGridViewRow row)
        {
            if (row == null)
                return;

            var typeCell = row.Cells[nameof(ProgressionRow.Type)] as DataGridViewComboBoxCell;
            var inversionCell = row.Cells[nameof(ProgressionRow.Inversion)] as DataGridViewComboBoxCell;

            if (typeCell == null || inversionCell == null)
                return;

            var typeValue = typeCell.Value?.ToString() ?? "triad";
            var allowed = typeValue == "seventh" ? new[] { 0, 1, 2, 3 } : new[] { 0, 1, 2 };

            inversionCell.DataSource = allowed;
            if (!allowed.Contains(Convert.ToInt32(inversionCell.Value ?? 0)))
                inversionCell.Value = 0;
        }

        private void UpdateSummaryAndValidation()
        {
            if (_currentSpec == null || _progressionRows == null)
                return;

            bool isValid = ValidateProgression(out var validationMessage);

            _currentSpec.Progression = _progressionRows.Select(r => new ChordEvent
            {
                Time = r.Time,
                Degree = r.Degree,
                Type = string.IsNullOrWhiteSpace(r.Type) ? "triad" : r.Type,
                Inversion = r.Inversion,
                BorrowMode = string.Equals(r.BorrowMode, "none", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : r.BorrowMode
            }).ToList();

            DisplaySummary();
            _generateButton.Enabled = isValid;

            if (!isValid && !string.IsNullOrWhiteSpace(validationMessage))
            {
                if (!string.Equals(_lastValidationMessage, validationMessage, StringComparison.Ordinal))
                {
                    LogMessage($"✗ Progression validation: {validationMessage}");
                    _lastValidationMessage = validationMessage;
                }
            }
            else
            {
                _lastValidationMessage = null;
            }
        }

        private bool ValidateProgression(out string message)
        {
            message = null;
            var errors = new List<string>();
            var timeCounts = new Dictionary<int, int>();

            foreach (var row in _progressionRows)
            {
                if (!timeCounts.ContainsKey(row.Time))
                    timeCounts[row.Time] = 0;
                timeCounts[row.Time] += 1;
            }

            for (int i = 0; i < _progressionRows.Count; i++)
            {
                var row = _progressionRows[i];
                var rowErrors = new List<string>();

                if (row.Time < 0)
                    rowErrors.Add("time must be >= 0");
                if (_sequenceLength.HasValue && row.Time >= _sequenceLength.Value)
                    rowErrors.Add($"time must be < {_sequenceLength.Value}");

                if (row.Degree < 1 || row.Degree > 7)
                    rowErrors.Add("degree must be 1-7");

                var normalizedType = string.IsNullOrWhiteSpace(row.Type) ? "triad" : row.Type;
                if (!ChordTypes.Contains(normalizedType))
                    rowErrors.Add("type must be triad or seventh");

                var maxInversion = string.Equals(normalizedType, "seventh", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
                if (row.Inversion < 0 || row.Inversion > maxInversion)
                    rowErrors.Add("invalid inversion for type");

                var normalizedBorrow = string.IsNullOrWhiteSpace(row.BorrowMode) ? "none" : row.BorrowMode;
                if (!BorrowModes.Contains(normalizedBorrow))
                    rowErrors.Add("invalid borrow mode");

                if (timeCounts.TryGetValue(row.Time, out var count) && count > 1)
                    rowErrors.Add("duplicate time");

                var gridRow = _progressionGrid.Rows[i];
                gridRow.DefaultCellStyle.BackColor = rowErrors.Count > 0 ? Color.MistyRose : Color.White;

                if (rowErrors.Count > 0)
                    errors.Add($"row {i + 1}: {string.Join(", ", rowErrors)}");
            }

            if (errors.Count > 0)
            {
                message = errors[0];
                return false;
            }

            return true;
        }

        private void GenerateMidi_Click(object sender, EventArgs e)
        {
            if (_currentSpec == null)
            {
                LogMessage("✗ No specification loaded.");
                return;
            }

            if (!ValidateProgression(out var validationMessage))
            {
                LogMessage($"✗ Progression validation: {validationMessage}");
                return;
            }

            try
            {
                LogMessage("Generating MIDI...");
                var generator = new MidiGenerator(_currentSpec);
                var midiFile = generator.GenerateMidiFile();

                var outputPath = _outputPath.Text;
                if (string.IsNullOrWhiteSpace(outputPath))
                    outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "output.mid");

                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                midiFile.Write(outputPath);

                var eventCount = midiFile.GetTrackChunks().SelectMany(t => t.Events).Count();
                _eventsText.Text = EventFormatter.FormatGenerationSummary(eventCount, _currentSpec);

                LogMessage($"✓ MIDI generated successfully!");
                LogMessage($"  File: {outputPath}");
                LogMessage($"  Events: {eventCount}");
            }
            catch (Exception ex)
            {
                LogMessage($"✗ Error generating MIDI: {ex.Message}");
            }
        }

        private void BrowseOutputPath_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save MIDI File",
                Filter = "MIDI Files (*.mid)|*.mid|All Files (*.*)|*.*",
                FileName = "output.mid",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _outputPath.Text = dialog.FileName;
            }
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logText.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        }

        private static readonly string[] BorrowModes =
        {
            "none",
            "ionian",
            "dorian",
            "phrygian",
            "lydian",
            "mixolydian",
            "aeolian",
            "locrian"
        };

        private static readonly HashSet<string> ChordTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "triad",
            "seventh"
        };

        // Control references
        private Label _pathLabel;
        private TextBox _summaryText;
        private TextBox _eventsText;
        private TextBox _logText;
        private Button _generateButton;
        private TextBox _outputPath;
        private DataGridView _progressionGrid;
        private Button _addChordButton;
        private NumericUpDown _minSharedPitchesInput;
        private string _lastValidationMessage;

        private class ProgressionRow
        {
            public int Time { get; set; }
            public int Degree { get; set; }
            public string Type { get; set; }
            public int Inversion { get; set; }
            public string BorrowMode { get; set; }
        }
    }
}
