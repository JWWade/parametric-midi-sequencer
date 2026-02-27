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
        private HarmonySpec? _currentSpec;
        private MetaSpec? _currentMeta;
        private string _currentJsonPath = string.Empty;
        private int? _sequenceLength;

        private BindingList<ProgressionRow> _progressionRows = new BindingList<ProgressionRow>();

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Parametric MIDI Sequencer - PoC15 UI";
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
                RowCount = 18,
                AutoSize = false,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 0: Scale title
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 1: Scale type
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 2: Scale context
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 3: Scale desc
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 4: Progression title
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 5: Grid
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 6: Add Chord
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 7
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 8
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 9
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 10
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 11
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 12
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 13
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 14
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 15
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 16
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 17
            panel.Controls.Add(layout);

            // ===== Scale / Mode Section =====
            var scaleSectionLabel = new Label
            {
                Text = "Scale / Mode",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            layout.Controls.Add(scaleSectionLabel, 0, 0);

            var scaleTypeRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };
            var scaleTypeLabel = new Label
            {
                Text = "Scale Type:",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0)
            };
            var scaleTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            scaleTypeCombo.Items.AddRange(new object[] { "Major / Minor", "Mode", "Custom Pitch-Class Set" });
            scaleTypeCombo.SelectedIndex = 0;
            scaleTypeCombo.SelectedIndexChanged += ScaleType_Changed;
            _scaleTypeCombo = scaleTypeCombo;

            var showDegreesCheck = new CheckBox
            {
                Text = "Show scale degrees",
                AutoSize = true,
                Margin = new Padding(14, 5, 0, 0)
            };
            showDegreesCheck.CheckedChanged += ShowDegrees_CheckedChanged;
            _showDegreesCheck = showDegreesCheck;

            scaleTypeRow.Controls.Add(scaleTypeLabel);
            scaleTypeRow.Controls.Add(scaleTypeCombo);
            scaleTypeRow.Controls.Add(showDegreesCheck);
            layout.Controls.Add(scaleTypeRow, 0, 1);

            var scaleContextRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };
            var scaleRootLabel = new Label
            {
                Text = "Root:",
                AutoSize = true,
                Margin = new Padding(0, 5, 4, 0)
            };
            var scaleRootCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 65,
                Enabled = false,
                Margin = new Padding(0, 2, 10, 0)
            };
            scaleRootCombo.Items.AddRange(NoteNames);
            scaleRootCombo.SelectedIndex = 0;
            scaleRootCombo.SelectedIndexChanged += ScaleRoot_Changed;
            _scaleRootLabel = scaleRootLabel;
            _scaleRootCombo = scaleRootCombo;

            var scaleQualityLabel = new Label
            {
                Text = "Quality:",
                AutoSize = true,
                Margin = new Padding(0, 5, 4, 0)
            };
            var scaleQualityCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 80,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            scaleQualityCombo.Items.AddRange(new object[] { "Major", "Minor" });
            scaleQualityCombo.SelectedIndex = 0;
            scaleQualityCombo.SelectedIndexChanged += ScaleQuality_Changed;
            _scaleQualityLabel = scaleQualityLabel;
            _scaleQualityCombo = scaleQualityCombo;

            var scaleModeLabel = new Label
            {
                Text = "Mode:",
                AutoSize = true,
                Margin = new Padding(0, 5, 4, 0),
                Visible = false
            };
            var scaleModeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0),
                Visible = false
            };
            scaleModeCombo.Items.AddRange(new object[] { "Ionian", "Dorian", "Phrygian", "Lydian", "Mixolydian", "Aeolian", "Locrian" });
            scaleModeCombo.SelectedIndex = 0;
            scaleModeCombo.SelectedIndexChanged += ScaleMode_Changed;
            _scaleModeLabel = scaleModeLabel;
            _scaleModeCombo = scaleModeCombo;

            var scaleCustomLabel = new Label
            {
                Text = "Pitch classes (0\u201311):",
                AutoSize = true,
                Margin = new Padding(0, 5, 4, 0),
                Visible = false
            };
            var scaleCustomInput = new TextBox
            {
                Width = 200,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0),
                Visible = false
            };
            scaleCustomInput.TextChanged += ScaleCustom_TextChanged;
            _scaleCustomLabel = scaleCustomLabel;
            _scaleCustomInput = scaleCustomInput;

            scaleContextRow.Controls.Add(scaleRootLabel);
            scaleContextRow.Controls.Add(scaleRootCombo);
            scaleContextRow.Controls.Add(scaleQualityLabel);
            scaleContextRow.Controls.Add(scaleQualityCombo);
            scaleContextRow.Controls.Add(scaleModeLabel);
            scaleContextRow.Controls.Add(scaleModeCombo);
            scaleContextRow.Controls.Add(scaleCustomLabel);
            scaleContextRow.Controls.Add(scaleCustomInput);
            layout.Controls.Add(scaleContextRow, 0, 2);

            var scaleDescLabel = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(scaleDescLabel, 0, 3);
            _scaleDescLabel = scaleDescLabel;

            var titleLabel = new Label
            {
                Text = "Progression Editor",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(titleLabel, 0, 4);

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
            grid.SelectionChanged += ProgressionGrid_SelectionChanged;
            layout.Controls.Add(grid, 0, 5);
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
            layout.Controls.Add(addButton, 0, 6);
            _addChordButton = addButton;

            // ===== Voice-Leading Constraint Section =====
            var constraintSectionLabel = new Label
            {
                Text = "Voice-Leading Constraint",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 6)
            };
            layout.Controls.Add(constraintSectionLabel, 0, 7);

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
            layout.Controls.Add(constraintRow, 0, 8);

            var constraintDescLabel = new Label
            {
                Text = "Higher values create smoother voice-leading. Lower values allow more harmonic contrast.",
                AutoSize = true,
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 0)
            };
            layout.Controls.Add(constraintDescLabel, 0, 9);

            // ===== Pitch-Center Cycling Section =====
            var pitchCenterSectionLabel = new Label
            {
                Text = "Pitch-Center Cycling",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 6)
            };
            layout.Controls.Add(pitchCenterSectionLabel, 0, 10);

            var pitchCenterRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };

            var pitchCenterLabel = new Label
            {
                Text = "Pitch-center rotation (semitones):",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0)
            };

            var pitchCenterInput = new NumericUpDown
            {
                Minimum = -11,
                Maximum = 11,
                Value = 0,
                Width = 55,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            pitchCenterInput.ValueChanged += PitchCenterCycle_ValueChanged;
            _pitchCenterCycleInput = pitchCenterInput;

            pitchCenterRow.Controls.Add(pitchCenterLabel);
            pitchCenterRow.Controls.Add(pitchCenterInput);
            layout.Controls.Add(pitchCenterRow, 0, 11);

            var pitchCenterDescLabel = new Label
            {
                Text = "Rotates all chords around the chromatic circle. Positive values shift upward; negative values shift downward.",
                AutoSize = true,
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 0)
            };
            layout.Controls.Add(pitchCenterDescLabel, 0, 12);

            // ===== Geometric Transform Section =====
            var geometricSectionLabel = new Label
            {
                Text = "Geometric Transform",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 6)
            };
            layout.Controls.Add(geometricSectionLabel, 0, 13);

            var geometricTypeRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };

            var geometricTypeLabel = new Label
            {
                Text = "Geometric chord-shape transform:",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0)
            };

            var geometricTypeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Enabled = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            geometricTypeCombo.Items.AddRange(new object[] { "None", "Rotate", "Reflect", "Expand" });
            geometricTypeCombo.SelectedIndex = 0;
            geometricTypeCombo.SelectedIndexChanged += ShapeTransformType_Changed;
            _shapeTransformTypeCombo = geometricTypeCombo;

            geometricTypeRow.Controls.Add(geometricTypeLabel);
            geometricTypeRow.Controls.Add(geometricTypeCombo);
            layout.Controls.Add(geometricTypeRow, 0, 14);

            var geometricTypeDescLabel = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 0)
            };
            layout.Controls.Add(geometricTypeDescLabel, 0, 15);
            _shapeTransformTypeDescLabel = geometricTypeDescLabel;

            // Parameter row (shown/hidden based on selected type)
            var geometricParamRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 4),
                Visible = false
            };

            // Rotate param
            var rotateParamLabel = new Label
            {
                Text = "Rotation amount (semitones):",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0),
                Visible = false
            };
            var rotateInput = new NumericUpDown
            {
                Minimum = -11,
                Maximum = 11,
                Value = 0,
                Width = 65,
                Margin = new Padding(0, 2, 0, 0),
                Visible = false
            };
            rotateInput.ValueChanged += ShapeTransformRotate_ValueChanged;
            _shapeTransformRotateInput = rotateInput;

            // Reflect param
            var reflectParamLabel = new Label
            {
                Text = "Reflection axis (pitch class):",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0),
                Visible = false
            };
            var reflectInput = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 90,
                Margin = new Padding(0, 2, 0, 0),
                Visible = false
            };
            reflectInput.Items.AddRange(new object[] { "0 (C)", "1 (C#)", "2 (D)", "3 (D#)", "4 (E)", "5 (F)", "6 (F#)", "7 (G)", "8 (G#)", "9 (A)", "10 (A#)", "11 (B)" });
            reflectInput.SelectedIndex = 0;
            reflectInput.SelectedIndexChanged += ShapeTransformReflect_Changed;
            _shapeTransformReflectInput = reflectInput;

            // Expand param
            var expandParamLabel = new Label
            {
                Text = "Expansion factor:",
                AutoSize = true,
                Margin = new Padding(0, 5, 6, 0),
                Visible = false
            };
            var expandInput = new NumericUpDown
            {
                Minimum = 0.5m,
                Maximum = 3.0m,
                Value = 1.0m,
                DecimalPlaces = 1,
                Increment = 0.1m,
                Width = 70,
                Margin = new Padding(0, 2, 0, 0),
                Visible = false
            };
            expandInput.ValueChanged += ShapeTransformExpand_ValueChanged;
            _shapeTransformExpandInput = expandInput;

            geometricParamRow.Controls.Add(rotateParamLabel);
            geometricParamRow.Controls.Add(rotateInput);
            geometricParamRow.Controls.Add(reflectParamLabel);
            geometricParamRow.Controls.Add(reflectInput);
            geometricParamRow.Controls.Add(expandParamLabel);
            geometricParamRow.Controls.Add(expandInput);
            layout.Controls.Add(geometricParamRow, 0, 16);
            _shapeTransformParamRow = geometricParamRow;
            _shapeTransformRotateLabel = rotateParamLabel;
            _shapeTransformReflectLabel = reflectParamLabel;
            _shapeTransformExpandLabel = expandParamLabel;

            var geometricParamDescLabel = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = System.Drawing.SystemColors.GrayText,
                Margin = new Padding(0, 0, 0, 0),
                Visible = false
            };
            layout.Controls.Add(geometricParamDescLabel, 0, 17);
            _shapeTransformParamDescLabel = geometricParamDescLabel;
        }

        private void CreateRightPanel(Control parent)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            ((TableLayoutPanel)parent).Controls.Add(panel, 2, 0);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9,
                AutoSize = false,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
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

            // Chromatic Circle section
            var circleLabel = new Label
            {
                Text = "Chromatic Circle",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 4)
            };
            layout.Controls.Add(circleLabel, 0, 4);

            var chromaticCircle = new ChromaticCircleControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            layout.Controls.Add(chromaticCircle, 0, 5);
            _chromaticCircle = chromaticCircle;

            // Chord Shape label (PoC17)
            var chordShapeLabel = new Label
            {
                Text = "Select a chord to view its shape",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 4, 0, 4)
            };
            layout.Controls.Add(chordShapeLabel, 0, 6);
            _chordShapeLabel = chordShapeLabel;

            // Events summary
            var eventsLabel = new Label
            {
                Text = "Generated Events Summary",
                Font = new System.Drawing.Font(SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(eventsLabel, 0, 7);

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
            layout.Controls.Add(eventsText, 0, 8);
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

        private void LoadJsonFile_Click(object? sender, EventArgs e)
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

                    if (rootObject.TryGetValue("meta", out var metaToken) && metaToken != null)
                    {
                        _currentMeta = metaToken.ToObject<MetaSpec>(JsonSerializer.Create(settings));
                        if (_currentMeta != null && _currentMeta.Steps > 0 && _currentMeta.Bars > 0)
                            _sequenceLength = _currentMeta.Steps * _currentMeta.Bars;
                    }
                    
                    if (rootObject.TryGetValue("harmony", out var harmonyToken) && harmonyToken != null)
                    {
                        // Standard format with harmony section
                        var harmonyJson = harmonyToken.ToString();
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

            // Load scale settings into scale editor
            _scaleTypeCombo.Enabled = true;
            _scaleRootCombo.Enabled = true;
            _scaleQualityCombo.Enabled = true;
            _scaleModeCombo.Enabled = true;
            _scaleCustomInput.Enabled = true;

            if (_currentSpec.CustomScale != null && _currentSpec.CustomScale.Count > 0)
            {
                _scaleTypeCombo.SelectedItem = "Custom Pitch-Class Set";
                _scaleCustomInput.Text = string.Join(",", _currentSpec.CustomScale);
            }
            else
            {
                var scaleName = (_currentSpec.ScaleName ?? "major").ToLowerInvariant();
                bool isMode = ModeDescriptions.ContainsKey(scaleName);
                if (isMode)
                {
                    _scaleTypeCombo.SelectedItem = "Mode";
                    var rootItem = _currentSpec.Root ?? "C";
                    _scaleRootCombo.SelectedItem = rootItem;
                    if (_scaleRootCombo.SelectedIndex < 0) _scaleRootCombo.SelectedIndex = 0;
                    var modeDisplay = scaleName.Length > 0 ? char.ToUpperInvariant(scaleName[0]) + scaleName.Substring(1) : scaleName;
                    _scaleModeCombo.SelectedItem = modeDisplay;
                    if (_scaleModeCombo.SelectedIndex < 0) _scaleModeCombo.SelectedIndex = 0;
                }
                else
                {
                    _scaleTypeCombo.SelectedItem = "Major / Minor";
                    var rootItem = _currentSpec.Root ?? "C";
                    _scaleRootCombo.SelectedItem = rootItem;
                    if (_scaleRootCombo.SelectedIndex < 0) _scaleRootCombo.SelectedIndex = 0;
                    _scaleQualityCombo.SelectedItem = string.Equals(scaleName, "minor", StringComparison.OrdinalIgnoreCase) ? "Minor" : "Major";
                }
            }

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

            var pitchCycle = _currentSpec.Constraints?.PitchCenterCycle ?? 0;
            _pitchCenterCycleInput.Value = Math.Clamp(pitchCycle, (int)_pitchCenterCycleInput.Minimum, (int)_pitchCenterCycleInput.Maximum);
            _pitchCenterCycleInput.Enabled = true;

            var shapeTransform = _currentSpec.Constraints?.ShapeTransform;
            var shapeType = shapeTransform?.Type?.ToLowerInvariant() ?? "none";
            string shapeDisplayItem = shapeType switch
            {
                "rotate" => "Rotate",
                "reflect" => "Reflect",
                "expand" => "Expand",
                _ => "None"
            };
            _shapeTransformTypeCombo.SelectedItem = shapeDisplayItem;
            if (shapeType == "rotate")
                _shapeTransformRotateInput.Value = Math.Clamp((decimal)(shapeTransform?.Amount ?? 0), -11, 11);
            else if (shapeType == "reflect")
                _shapeTransformReflectInput.SelectedIndex = Math.Clamp(shapeTransform?.Axis ?? 0, 0, 11);
            else if (shapeType == "expand")
                _shapeTransformExpandInput.Value = Math.Clamp((decimal)(shapeTransform?.Amount ?? 1.0), 0.5m, 3.0m);
            _shapeTransformTypeCombo.Enabled = true;

            UpdateSummaryAndValidation();
            UpdateChromaticCircle();
        }

        private void MinSharedPitches_ValueChanged(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            _currentSpec.Constraints.MinSharedPitches = (int)_minSharedPitchesInput.Value;
            DisplaySummary();
            UpdateChordShapeVisualization();
        }

        private void PitchCenterCycle_ValueChanged(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            _currentSpec.Constraints.PitchCenterCycle = (int)_pitchCenterCycleInput.Value;
            DisplaySummary();
            UpdateChordShapeVisualization();
        }

        private void ShapeTransformType_Changed(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            var selected = _shapeTransformTypeCombo.SelectedItem?.ToString() ?? "None";

            // Show/hide param controls based on type
            bool showRotate = selected == "Rotate";
            bool showReflect = selected == "Reflect";
            bool showExpand = selected == "Expand";
            bool showParams = showRotate || showReflect || showExpand;

            _shapeTransformRotateLabel.Visible = showRotate;
            _shapeTransformRotateInput.Visible = showRotate;
            _shapeTransformReflectLabel.Visible = showReflect;
            _shapeTransformReflectInput.Visible = showReflect;
            _shapeTransformExpandLabel.Visible = showExpand;
            _shapeTransformExpandInput.Visible = showExpand;
            _shapeTransformParamRow.Visible = showParams;
            _shapeTransformParamDescLabel.Visible = showParams;

            // Update type description and param description
            if (showRotate)
            {
                _shapeTransformTypeDescLabel.Text = "Rotates the chord shape around the chromatic circle.";
                _shapeTransformParamDescLabel.Text = "Integer semitone shift: -11 to +11.";
            }
            else if (showReflect)
            {
                _shapeTransformTypeDescLabel.Text = "Mirrors the chord shape across the chosen axis.";
                _shapeTransformParamDescLabel.Text = "Pitch class (0-11) to reflect across.";
            }
            else if (showExpand)
            {
                _shapeTransformTypeDescLabel.Text = "Stretches or compresses the chord\u2019s internal spacing.";
                _shapeTransformParamDescLabel.Text = "Values >1 expand the chord shape; values <1 contract it.";
            }
            else
            {
                _shapeTransformTypeDescLabel.Text = "";
                _shapeTransformParamDescLabel.Text = "";
            }

            // Update spec
            if (showRotate)
                _currentSpec.Constraints.ShapeTransform = new ShapeTransform { Type = "rotate", Amount = (double)_shapeTransformRotateInput.Value };
            else if (showReflect)
                _currentSpec.Constraints.ShapeTransform = new ShapeTransform { Type = "reflect", Axis = _shapeTransformReflectInput.SelectedIndex };
            else if (showExpand)
                _currentSpec.Constraints.ShapeTransform = new ShapeTransform { Type = "expand", Amount = (double)_shapeTransformExpandInput.Value };
            else
                _currentSpec.Constraints.ShapeTransform = null;

            DisplaySummary();
            UpdateChordShapeVisualization();
        }

        private void ShapeTransformRotate_ValueChanged(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            if (_currentSpec.Constraints.ShapeTransform == null)
                _currentSpec.Constraints.ShapeTransform = new ShapeTransform { Type = "rotate" };

            _currentSpec.Constraints.ShapeTransform.Amount = (double)_shapeTransformRotateInput.Value;
            DisplaySummary();
            UpdateChordShapeVisualization();
        }

        private void ShapeTransformReflect_Changed(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            if (_currentSpec.Constraints.ShapeTransform == null)
                _currentSpec.Constraints.ShapeTransform = new ShapeTransform { Type = "reflect" };

            _currentSpec.Constraints.ShapeTransform.Axis = _shapeTransformReflectInput.SelectedIndex;
            DisplaySummary();
            UpdateChordShapeVisualization();
        }

        private void ShapeTransformExpand_ValueChanged(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            if (_currentSpec.Constraints == null)
                _currentSpec.Constraints = new HarmonyConstraints();

            if (_currentSpec.Constraints.ShapeTransform == null)
                _currentSpec.Constraints.ShapeTransform = new ShapeTransform { Type = "expand" };

            _currentSpec.Constraints.ShapeTransform.Amount = (double)_shapeTransformExpandInput.Value;
            DisplaySummary();
            UpdateChordShapeVisualization();
        }

        private void ScaleType_Changed(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;

            var selected = _scaleTypeCombo.SelectedItem?.ToString() ?? "Major / Minor";
            bool isMajorMinor = selected == "Major / Minor";
            bool isMode = selected == "Mode";
            bool isCustom = selected == "Custom Pitch-Class Set";

            _scaleRootLabel.Visible = isMajorMinor || isMode;
            _scaleRootCombo.Visible = isMajorMinor || isMode;
            _scaleQualityLabel.Visible = isMajorMinor;
            _scaleQualityCombo.Visible = isMajorMinor;
            _scaleModeLabel.Visible = isMode;
            _scaleModeCombo.Visible = isMode;
            _scaleCustomLabel.Visible = isCustom;
            _scaleCustomInput.Visible = isCustom;

            if (isMode)
                UpdateModeDescription();
            else if (!isCustom)
                _scaleDescLabel.Text = "";

            UpdateScaleSpec();
        }

        private void ScaleRoot_Changed(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;
            UpdateScaleSpec();
        }

        private void ScaleQuality_Changed(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;
            UpdateScaleSpec();
        }

        private void ScaleMode_Changed(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;
            UpdateModeDescription();
            UpdateScaleSpec();
        }

        private void ScaleCustom_TextChanged(object? sender, EventArgs e)
        {
            if (_currentSpec == null)
                return;
            UpdateScaleSpec();
        }

        private void UpdateModeDescription()
        {
            var mode = _scaleModeCombo.SelectedItem?.ToString()?.ToLowerInvariant() ?? "";
            _scaleDescLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            _scaleDescLabel.Text = ModeDescriptions.TryGetValue(mode, out var desc) ? desc : "";
        }

        private void UpdateScaleSpec()
        {
            if (_currentSpec == null)
                return;

            var selected = _scaleTypeCombo.SelectedItem?.ToString() ?? "Major / Minor";
            if (selected == "Custom Pitch-Class Set")
            {
                var parsed = ParseCustomPitchClasses(_scaleCustomInput.Text, out var validationMsg);
                bool isValid = parsed != null;
                _scaleCustomInput.BackColor = isValid || string.IsNullOrWhiteSpace(_scaleCustomInput.Text)
                    ? System.Drawing.SystemColors.Window
                    : Color.MistyRose;
                if (!string.IsNullOrWhiteSpace(_scaleCustomInput.Text))
                {
                    _scaleDescLabel.ForeColor = isValid ? System.Drawing.SystemColors.GrayText : Color.Red;
                    _scaleDescLabel.Text = isValid
                        ? "Enter at least 3 pitch classes. Duplicates will be removed."
                        : (validationMsg ?? "Invalid pitch classes.");
                }
                else
                {
                    _scaleDescLabel.ForeColor = System.Drawing.SystemColors.GrayText;
                    _scaleDescLabel.Text = "Enter at least 3 pitch classes. Duplicates will be removed.";
                }
                if (string.IsNullOrWhiteSpace(_scaleCustomInput.Text))
                {
                    // When the custom pitch-class input is cleared, also clear the custom scale
                    _currentSpec.CustomScale = null;
                    _currentSpec.Scale = new List<int>();
                }
                else if (isValid)
                {
                    _currentSpec.CustomScale = parsed;
                    _currentSpec.Scale = new List<int>();
                }
            }
            else
            {
                _currentSpec.CustomScale = null;
                _currentSpec.Scale = new List<int>();
                var root = _scaleRootCombo.SelectedItem?.ToString() ?? "C";
                _currentSpec.Root = root;
                if (selected == "Mode")
                {
                    var mode = _scaleModeCombo.SelectedItem?.ToString()?.ToLowerInvariant() ?? "ionian";
                    _currentSpec.ScaleName = mode;
                }
                else
                {
                    var quality = _scaleQualityCombo.SelectedItem?.ToString() ?? "Major";
                    _currentSpec.ScaleName = quality.ToLowerInvariant();
                }
            }

            DisplaySummary();
            UpdateChromaticCircle();
        }

        private void UpdateChromaticCircle()
        {
            if (_currentSpec == null)
                return;

            List<int> activePcs;
            if (_currentSpec.CustomScale != null && _currentSpec.CustomScale.Count > 0)
            {
                activePcs = _currentSpec.CustomScale;
            }
            else
            {
                var scaleName = _currentSpec.ScaleName ?? "major";
                var root = _currentSpec.Root ?? "C";
                activePcs = ModeBuilder.IsModeName(scaleName)
                    ? ModeBuilder.BuildModeScale(scaleName, root)
                    : ScaleBuilder.BuildScale(scaleName, root);
            }

            _chromaticCircle.ActivePitchClasses = activePcs;
            _chromaticCircle.ShowDegrees = _showDegreesCheck.Checked;
            UpdateChordShapeVisualization();
        }

        private void ShowDegrees_CheckedChanged(object? sender, EventArgs e)
        {
            _chromaticCircle.ShowDegrees = _showDegreesCheck.Checked;
        }

        private void ProgressionGrid_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateChordShapeVisualization();
        }

        private void UpdateChordShapeVisualization()
        {
            if (_currentSpec == null || _progressionGrid.CurrentRow == null || !_progressionGrid.Enabled)
            {
                _chromaticCircle.ChordPitchClasses = null;
                _chromaticCircle.NextChordPitchClasses = null;
                _chordShapeLabel.Text = "Select a chord to view its shape";
                _chordShapeLabel.ForeColor = SystemColors.GrayText;
                return;
            }

            int rowIndex = _progressionGrid.CurrentRow.Index;
            if (rowIndex < 0 || rowIndex >= _progressionRows.Count)
            {
                _chromaticCircle.ChordPitchClasses = null;
                _chromaticCircle.NextChordPitchClasses = null;
                _chordShapeLabel.Text = "Select a chord to view its shape";
                _chordShapeLabel.ForeColor = SystemColors.GrayText;
                return;
            }

            try
            {
                var allChordPcs = HarmonyGenerator.ComputeTransformedChordPitchClasses(_currentSpec);
                if (rowIndex < allChordPcs.Count)
                {
                    _chromaticCircle.ChordPitchClasses = allChordPcs[rowIndex];

                    bool hasNext = rowIndex + 1 < allChordPcs.Count;
                    _chromaticCircle.NextChordPitchClasses = hasNext ? allChordPcs[rowIndex + 1] : null;

                    var row = _progressionRows[rowIndex];
                    int time = row.Time;
                    int degree = row.Degree;
                    string type = string.IsNullOrWhiteSpace(row.Type) ? "triad" : row.Type;
                    int inversion = row.Inversion;

                    string roman = degree >= 1 && degree <= 7 ? RomanNumerals[degree - 1] : degree.ToString();
                    string chordName = type == "seventh" ? $"{roman}7" : roman;
                    string inversionText = inversion switch
                    {
                        1 => " (1st inversion)",
                        2 => " (2nd inversion)",
                        3 => " (3rd inversion)",
                        _ => " (root position)"
                    };
                    string vlInversionText = inversion switch
                    {
                        1 => " (1st inversion)",
                        2 => " (2nd inversion)",
                        3 => " (3rd inversion)",
                        _ => ""
                    };

                    if (hasNext)
                    {
                        var nextRow = _progressionRows[rowIndex + 1];
                        int nextTime = nextRow.Time;
                        int nextDegree = nextRow.Degree;
                        string nextType = string.IsNullOrWhiteSpace(nextRow.Type) ? "triad" : nextRow.Type;
                        int nextInversion = nextRow.Inversion;
                        string nextRoman = nextDegree >= 1 && nextDegree <= 7 ? RomanNumerals[nextDegree - 1] : nextDegree.ToString();
                        string nextChordName = nextType == "seventh" ? $"{nextRoman}7" : nextRoman;
                        string nextInversionText = nextInversion switch
                        {
                            1 => " (1st inversion)",
                            2 => " (2nd inversion)",
                            3 => " (3rd inversion)",
                            _ => ""
                        };
                        _chordShapeLabel.Text = $"Voice‑leading: {chordName}{vlInversionText} (t={time}) → {nextChordName}{nextInversionText} (t={nextTime})";
                    }
                    else
                    {
                        var pcs = allChordPcs[rowIndex];
                        string pcList = string.Join(", ", pcs);
                        _chordShapeLabel.Text = $"Chord at time {time}: {chordName}{inversionText}  [{pcList}]";
                    }
                    _chordShapeLabel.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    _chromaticCircle.ChordPitchClasses = null;
                    _chromaticCircle.NextChordPitchClasses = null;
                    _chordShapeLabel.Text = "Select a chord to view its shape";
                    _chordShapeLabel.ForeColor = SystemColors.GrayText;
                }
            }
            catch (Exception ex)
            {
                _chromaticCircle.ChordPitchClasses = null;
                _chromaticCircle.NextChordPitchClasses = null;
                _chordShapeLabel.Text = "Error computing chord shape";
                _chordShapeLabel.ForeColor = SystemColors.GrayText;
                LogMessage($"✗ Chord shape error: {ex.Message}");
            }
        }

        private static readonly string[] RomanNumerals = { "I", "II", "III", "IV", "V", "VI", "VII" };

        private static List<int>? ParseCustomPitchClasses(string text, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                errorMessage = "Enter pitch classes (0\u201311), separated by commas.";
                return null;
            }

            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>();
            var seen = new HashSet<int>();
            foreach (var part in parts)
            {
                if (!int.TryParse(part.Trim(), out var pc) || pc < 0 || pc > 11)
                {
                    errorMessage = $"Invalid pitch class '{part.Trim()}'. Values must be integers 0\u201311.";
                    return null;
                }
                if (seen.Add(pc))
                    result.Add(pc);
            }
            if (result.Count < 3)
            {
                errorMessage = "Enter at least 3 pitch classes.";
                return null;
            }
            return result;
        }


        private void AddChord_Click(object? sender, EventArgs e)
        {
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

        private void ProgressionGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_progressionGrid.IsCurrentCellDirty)
                _progressionGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void ProgressionGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            var row = _progressionGrid.Rows[e.RowIndex];
            if (_progressionGrid.Columns[e.ColumnIndex].DataPropertyName == nameof(ProgressionRow.Type))
                UpdateInversionCell(row);

            UpdateSummaryAndValidation();
        }

        private void ProgressionGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (_progressionGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                _progressionRows.RemoveAt(e.RowIndex);
                UpdateSummaryAndValidation();
            }
        }

        private void ProgressionGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void UpdateInversionCell(DataGridViewRow row)
        {
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
            if (_currentSpec == null)
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

        private bool ValidateProgression(out string? message)
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

        private void GenerateMidi_Click(object? sender, EventArgs e)
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

        private void BrowseOutputPath_Click(object? sender, EventArgs e)
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

        private static readonly string[] NoteNames =
        {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };

        private static readonly Dictionary<string, string> ModeDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ionian",     "Ionian — major scale" },
            { "dorian",     "Dorian — minor with raised 6" },
            { "phrygian",   "Phrygian — minor with lowered 2" },
            { "lydian",     "Lydian — major with raised 4" },
            { "mixolydian", "Mixolydian — major with lowered 7" },
            { "aeolian",    "Aeolian — natural minor" },
            { "locrian",    "Locrian — diminished tonic" }
        };

        // Control references
        private Label _pathLabel = null!;
        private TextBox _summaryText = null!;
        private TextBox _eventsText = null!;
        private TextBox _logText = null!;
        private Button _generateButton = null!;
        private TextBox _outputPath = null!;
        private DataGridView _progressionGrid = null!;
        private Button _addChordButton = null!;
        private NumericUpDown _minSharedPitchesInput = null!;
        private NumericUpDown _pitchCenterCycleInput = null!;
        private ComboBox _shapeTransformTypeCombo = null!;
        private Label _shapeTransformTypeDescLabel = null!;
        private FlowLayoutPanel _shapeTransformParamRow = null!;
        private Label _shapeTransformRotateLabel = null!;
        private NumericUpDown _shapeTransformRotateInput = null!;
        private Label _shapeTransformReflectLabel = null!;
        private ComboBox _shapeTransformReflectInput = null!;
        private Label _shapeTransformExpandLabel = null!;
        private NumericUpDown _shapeTransformExpandInput = null!;
        private Label _shapeTransformParamDescLabel = null!;
        private string? _lastValidationMessage;

        // Scale editor controls
        private ComboBox _scaleTypeCombo = null!;
        private CheckBox _showDegreesCheck = null!;
        private Label _scaleRootLabel = null!;
        private ComboBox _scaleRootCombo = null!;
        private Label _scaleQualityLabel = null!;
        private ComboBox _scaleQualityCombo = null!;
        private Label _scaleModeLabel = null!;
        private ComboBox _scaleModeCombo = null!;
        private Label _scaleCustomLabel = null!;
        private TextBox _scaleCustomInput = null!;
        private Label _scaleDescLabel = null!;

        private ChromaticCircleControl _chromaticCircle = null!;
        private Label _chordShapeLabel = null!;

        private class ProgressionRow
        {
            public int Time { get; set; }
            public int Degree { get; set; }
            public string Type { get; set; } = "triad";
            public int Inversion { get; set; }
            public string BorrowMode { get; set; } = "none";
        }
    }
}
