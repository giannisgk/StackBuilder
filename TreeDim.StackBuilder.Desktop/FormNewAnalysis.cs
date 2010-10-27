﻿#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using TreeDim.StackBuilder.Basics;
using TreeDim.StackBuilder.Graphics;
using Sharp3D.Math.Core;
using log4net;

using TreeDim.StackBuilder.Desktop.Properties;
#endregion

namespace TreeDim.StackBuilder.Desktop
{
    public partial class FormNewAnalysis : Form
    {
        #region Data members
        private BProperties[] _boxes;
        private PalletProperties[] _palletProperties;
        private InterlayerProperties[] _interlayerProperties;
        private Document _document;
        private Analysis _analysis;
        protected static readonly ILog _log = LogManager.GetLogger(typeof(FormNewAnalysis));
        #endregion

        #region Combo box item private classes
        private class BoxItem
        {
            private BProperties _boxProperties;

            public BoxItem(BProperties boxProperties)
            {
                _boxProperties = boxProperties;
            }

            public BProperties Item
            {
                get { return _boxProperties; }
            }

            public override string ToString()
            {
                return _boxProperties.Name;
            }
        }
        private class PalletItem
        { 
            private PalletProperties _palletProperties;

            public PalletItem(PalletProperties palletProperties)
            {
                _palletProperties = palletProperties;
            }

            public PalletProperties Item
            {
                get { return _palletProperties; }
            }

            public override string ToString()
            {
                return _palletProperties.Name;
            }       
        }
        private class InterlayerItem
        {
            private InterlayerProperties _interlayerProperties;

            public InterlayerItem(InterlayerProperties interlayerProperties)
            {
                _interlayerProperties = interlayerProperties;
            }

            public InterlayerProperties Item
            {
                get { return _interlayerProperties; }
            }

            public override string ToString()
            {
                return _interlayerProperties.Name;
            }
        }
        #endregion

        #region Constructor
        public FormNewAnalysis(Document document)
        {
            InitializeComponent();
            // save document reference
            _document = document;
            onInterlayerChecked(this, null);
        }
        public FormNewAnalysis(Document document, Analysis analysis)
        {
            InitializeComponent();
            // save document reference
            _document = document;
            _analysis = analysis;
            // set caption text
            Text = string.Format("Edit {0}...", _analysis.Name);

            onInterlayerChecked(this, null);
        }
        #endregion

        #region Load / FormClosing event
        private void FormNewAnalysis_Load(object sender, EventArgs e)
        {
            try
            {
                // fill boxes combo
                foreach (BProperties box in _boxes)
                    cbBox.Items.Add(new BoxItem(box));
                if (cbBox.Items.Count > 0)
                    cbBox.SelectedIndex = 0;
                // fill pallet combo
                foreach (PalletProperties pallet in _palletProperties)
                    cbPallet.Items.Add(new PalletItem(pallet));
                if (cbPallet.Items.Count > 0)
                    cbPallet.SelectedIndex = 0;
                // fill interlayer combo
                foreach (InterlayerProperties interlayer in _interlayerProperties)
                    cbInterlayer.Items.Add(new InterlayerItem(interlayer));
                if (cbInterlayer.Items.Count > 0)
                    cbInterlayer.SelectedIndex = 0;
                else
                {
                    checkBoxInterlayer.Checked = false;
                    checkBoxInterlayer.Enabled = false;
                }

                // allowed position box
                AllowVerticalX = Settings.Default.AllowVerticalX;
                AllowVerticalY = Settings.Default.AllowVerticalY;
                AllowVerticalZ = Settings.Default.AllowVerticalZ;

                // alternate / aligned layers
                AllowAlignedLayers = Settings.Default.AllowAlignedLayer;
                AllowAlternateLayers = Settings.Default.AllowAlternateLayer;

                // stop stacking criterion
                UseMaximumNumberOfBoxes = false;
                UseMaximumPalletHeight = true;
                UseMaximumPalletWeight = true;
                UseMaximumLoadOnBox = false;
                MaximumNumberOfBoxes = 500;
                MaximumPalletHeight = 1500.0;
                MaximumPalletWeight = 1000.0;
                MaximumLoadOnBox = 100.0;

                // check all patterns
                string allowedPatterns = Settings.Default.AllowedPatterns;
                int iCountAllowedPatterns = 0;
                for (int i = 0; i < checkedListBoxPatterns.Items.Count; ++i)
                {
                    string patternName = checkedListBoxPatterns.GetItemText(checkedListBoxPatterns.Items[i]);
                    bool patternAllowed = allowedPatterns.Contains(patternName);
                    checkedListBoxPatterns.SetItemChecked(i, patternAllowed);
                    if (patternAllowed)
                        ++iCountAllowedPatterns;
                }
                if (iCountAllowedPatterns == 0)
                    for (int i = 0; i < checkedListBoxPatterns.Items.Count; ++i)
                        checkedListBoxPatterns.SetItemChecked(i, true);

                // keep best solutions
                UseNumberOfSolutionsKept = Settings.Default.KeepBestSolutions;
                NumberOfSolutionsKept = Settings.Default.NoSolutionsToKeep;                

                UpdateSolutionsToKeep();
                UpdateCriterionFields();
                UpdateButtonOkStatus();


                // windows settings
                if (null != Settings.Default.FormNewAnalysisPosition)
                    Settings.Default.FormNewAnalysisPosition.Restore(this);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }

        private void FormNewAnalysis_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // allowed position box
                Settings.Default.AllowVerticalX = AllowVerticalX;
                Settings.Default.AllowVerticalY = AllowVerticalY;
                Settings.Default.AllowVerticalZ = AllowVerticalZ;
                // alternate / aligned layers
                Settings.Default.AllowAlignedLayer = AllowAlignedLayers;
                Settings.Default.AllowAlternateLayer = AllowAlternateLayers;
                // allowed patterns
                string allowedPatterns = string.Empty;
                for (int i = 0; i < checkedListBoxPatterns.Items.Count; ++i)
                    if (checkedListBoxPatterns.GetItemChecked(i))
                        allowedPatterns += checkedListBoxPatterns.GetItemText(checkedListBoxPatterns.Items[i]) + ";";
                Settings.Default.AllowedPatterns = allowedPatterns;
                // keep best solutions
                Settings.Default.KeepBestSolutions = UseNumberOfSolutionsKept;
                Settings.Default.NoSolutionsToKeep = NumberOfSolutionsKept;                
                // window position
                if (null == Settings.Default.FormNewAnalysisPosition)
                    Settings.Default.FormNewAnalysisPosition = new WindowSettings();
                Settings.Default.FormNewAnalysisPosition.Record(this);
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
            }
        }
        #endregion

        #region Public properties
        public string AnalysisName
        {
            get { return tbName.Text; }
        }
        public string AnalysisDescription
        {
            get { return tbDescription.Text; }
        }
        public BProperties[] Boxes
        {
            get { return _boxes;}
            set { _boxes = value; }
        }
        public PalletProperties[] Pallets
        {
            get { return _palletProperties; }
            set { _palletProperties = value; }
        }
        public InterlayerProperties[] Interlayers
        {
            get { return _interlayerProperties; }
            set { _interlayerProperties = value; }
        }
        public BProperties SelectedBox
        {
            get { return _boxes[cbBox.SelectedIndex]; }
        }
        public PalletProperties SelectedPallet
        {
            get { return _palletProperties[cbPallet.SelectedIndex]; }
        }
        public InterlayerProperties SelectedInterlayer
        {
            get
            {
                if (null == _interlayerProperties
                    || _interlayerProperties.Length == 0
                    || !checkBoxInterlayer.Checked
                    || -1 == cbInterlayer.SelectedIndex)
                    return null;
                else
                    return _interlayerProperties[cbInterlayer.SelectedIndex]; 
            }
        }
        public bool UseMaximumNumberOfBoxes
        {
            get { return checkBoxMaximumNumberOfBoxes.Checked; }
            set { checkBoxMaximumNumberOfBoxes.Checked = value; }
        }
        public int MaximumNumberOfBoxes
        {
            get { return (int)nudMaximumNumberOfBoxes.Value; }
            set { nudMaximumNumberOfBoxes.Value = (decimal)value; }
        }
        public bool UseMaximumPalletHeight
        {
            get { return checkBoxMaximumPalletHeight.Checked; }
            set { checkBoxMaximumPalletHeight.Checked = value; }
        }
        public double MaximumPalletHeight
        {
            get { return (double)nudMaximumPalletHeight.Value; }
            set { nudMaximumPalletHeight.Value = (decimal)value; }
        }
        public bool UseMaximumPalletWeight
        {
            get { return checkBoxMaximumPalletWeight.Checked; }
            set { checkBoxMaximumPalletWeight.Checked = value; }
        }
        public double MaximumPalletWeight
        {
            get { return (double)nudMaximumPalletWeight.Value; }
            set { nudMaximumPalletWeight.Value = (decimal)value; }
        }
        public bool UseMaximumLoadOnBox
        {
            get { return checkBoxMaximumLoadOnBox.Checked; }
            set { checkBoxMaximumLoadOnBox.Checked = value; }
        }
        public double MaximumLoadOnBox
        {
            get { return (double)nudMaximumLoadOnBox.Value; }
            set { nudMaximumLoadOnBox.Value = (decimal) value; }
        }
        public bool AllowVerticalX
        {
            get { return checkBoxPositionX.Checked; }
            set { checkBoxPositionX.Checked = value; }
        }
        public bool AllowVerticalY
        {
            get { return checkBoxPositionY.Checked; }
            set { checkBoxPositionY.Checked = value; }
        }
        public bool AllowVerticalZ
        {
            get { return checkBoxPositionZ.Checked; }
            set { checkBoxPositionZ.Checked = value; }
        }
        public List<string> AllowedPatterns
        {
            get
            {
                List<string> listAllowedPatterns = new List<string>();
                foreach (object itemChecked in checkedListBoxPatterns.CheckedItems)
                {
                    // use the IndexOf method to get the index of an item
                    if (checkedListBoxPatterns.GetItemCheckState(checkedListBoxPatterns.Items.IndexOf(itemChecked)) == CheckState.Checked)
                        listAllowedPatterns.Add(itemChecked.ToString());
                }
                return listAllowedPatterns;
            }
        }
        public bool HasInterlayers
        {
            get { return checkBoxInterlayer.Checked; }
            set { checkBoxInterlayer.Checked = value; }
        }
        public int InterlayerPeriod
        {
            get { return (int)nudInterlayerFreq.Value; }
            set { nudInterlayerFreq.Value = (decimal)value; }
        }
        public bool AllowAlternateLayers
        {
            get { return checkBoxAllowAlternateLayer.Checked; }
            set { checkBoxAllowAlternateLayer.Checked = value; }
        }
        public bool AllowAlignedLayers
        {
            get { return checkBoxAllowAlignedLayer.Checked; }
            set { checkBoxAllowAlignedLayer.Checked = value; }
        }

        public double OverhangX
        {
            get { return (double)nudPalletOverhangX.Value; }
        }
        public double OverhangY
        {
            get { return (double)nudPalletOverhangY.Value; }
        }
        public bool UseNumberOfSolutionsKept
        {
            get { return checkBoxKeepSolutions.Checked; }
            set { checkBoxKeepSolutions.Checked = value; }
        }
        public int NumberOfSolutionsKept
        {
            get { return (int)nudSolutions.Value; }
            set { nudSolutions.Value = (decimal)value; }
        }
        #endregion

        #region Handlers
        private void onBoxChanged(object sender, EventArgs e)
        {
            DrawBoxPositions();
        }

        private void UpdateButtonOkStatus()
        {
            bnAccept.Enabled =
                tbName.Text.Length > 0
                && tbDescription.Text.Length > 0
                && _document.IsValidNewTypeName(tbName.Text, _analysis);
        }
        private void onNameDescriptionChanged(object sender, EventArgs e)
        {
            UpdateButtonOkStatus();
        }
        private void UpdateCriterionFields()
        { 
            nudMaximumNumberOfBoxes.Enabled = checkBoxMaximumNumberOfBoxes.Checked;
            nudMaximumPalletHeight.Enabled = checkBoxMaximumPalletHeight.Checked;
            nudMaximumPalletWeight.Enabled = checkBoxMaximumPalletWeight.Checked;
            nudMaximumLoadOnBox.Enabled = checkBoxMaximumLoadOnBox.Checked;
        }
        private void onCriterionCheckChanged(object sender, EventArgs e)
        {
            UpdateCriterionFields();
        }
        private void UpdateSolutionsToKeep()
        { 
            nudSolutions.Enabled = checkBoxKeepSolutions.Checked;
        }
        private void onCheckedChangedKeepSolutions(object sender, EventArgs e)
        {
            UpdateSolutionsToKeep();
        }
        private void onInterlayerChecked(object sender, EventArgs e)
        {
            cbInterlayer.Enabled = checkBoxInterlayer.Checked;
            lbInterlayerFreq1.Enabled = checkBoxInterlayer.Checked;
            lbInterlayerFreq2.Enabled = checkBoxInterlayer.Checked;
            nudInterlayerFreq.Enabled = checkBoxInterlayer.Checked;
        }
        private void onCheckedChangedAlignedLayer(object sender, EventArgs e)
        {
            if (!checkBoxAllowAlignedLayer.Checked && !checkBoxAllowAlternateLayer.Checked)
                checkBoxAllowAlternateLayer.Checked = true;
        }
        private void onCheckedChangedAlternateLayer(object sender, EventArgs e)
        {
            if (!checkBoxAllowAlignedLayer.Checked && !checkBoxAllowAlternateLayer.Checked)
                checkBoxAllowAlignedLayer.Checked = true;
        }
        #endregion

        #region Box position drawings
        private void DrawBoxPositions()
        { 
            // get current boxProperties
            BProperties selectedBox = SelectedBox;
            DrawBoxPosition(selectedBox, HalfAxis.HAxis.AXIS_X_P, pictureBoxPositionX);
            DrawBoxPosition(selectedBox, HalfAxis.HAxis.AXIS_Y_P, pictureBoxPositionY);
            DrawBoxPosition(selectedBox, HalfAxis.HAxis.AXIS_Z_P, pictureBoxPositionZ);
        }
        private void DrawBoxPosition(BProperties boxProperties, HalfAxis.HAxis axis, PictureBox pictureBox)
        {
            // get horizontal angle
            double angle = 45;
            // instantiate graphics
            Graphics3DImage graphics = new Graphics3DImage(pictureBox.Size);
            graphics.CameraPosition = new Vector3D(
                Math.Cos(angle * Math.PI / 180.0) * Math.Sqrt(2.0) * 10000.0
                , Math.Sin(angle * Math.PI / 180.0) * Math.Sqrt(2.0) * 10000.0
                , 10000.0);
            graphics.Target = Vector3D.Zero;
            graphics.LightDirection = new Vector3D(-0.75, -0.5, 1.0);
            graphics.SetViewport(-500.0f, -500.0f, 500.0f, 500.0f);
            // draw
            Box box = new Box(0, boxProperties);

            // set axes
            HalfAxis.HAxis lengthAxis = HalfAxis.HAxis.AXIS_X_P;
            HalfAxis.HAxis widthAxis = HalfAxis.HAxis.AXIS_Y_P;
            switch (axis)
            {
                case HalfAxis.HAxis.AXIS_X_P: lengthAxis = HalfAxis.HAxis.AXIS_Z_P; widthAxis = HalfAxis.HAxis.AXIS_X_P; break;
                case HalfAxis.HAxis.AXIS_Y_P: lengthAxis = HalfAxis.HAxis.AXIS_X_P; widthAxis = HalfAxis.HAxis.AXIS_Z_N; break;
                case HalfAxis.HAxis.AXIS_Z_P: lengthAxis = HalfAxis.HAxis.AXIS_X_P; widthAxis = HalfAxis.HAxis.AXIS_Y_P; break;
                default: break;
            }
            box.LengthAxis = TreeDim.StackBuilder.Basics.HalfAxis.ToVector3D(lengthAxis);
            box.WidthAxis = TreeDim.StackBuilder.Basics.HalfAxis.ToVector3D(widthAxis);

            // draw box
            graphics.AddBox(box);
            graphics.Flush();
            // set to picture box
            pictureBox.Image = graphics.Bitmap;
        }
        #endregion
    }
}