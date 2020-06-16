using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RichTextBoxTargetsTest
{
    public partial class MainForm : Form
    {
        private readonly NLog.ILogger LogHelper = NLog.LogManager.GetCurrentClassLogger();
        public MainForm()
        {
            InitializeComponent();
            NLog.Windows.Forms.RichTextBoxTarget.ReInitializeAllTextboxes(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LogHelper.Debug("Debug");
            LogHelper.Info("Info");
            LogHelper.Warn("Warn");
            LogHelper.Error("Error");
            LogHelper.Fatal("Fatal");
        }
    }
}