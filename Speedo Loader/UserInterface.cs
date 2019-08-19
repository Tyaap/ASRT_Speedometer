using Speedo;
using Speedo.Interface;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Speedo_Loader
{
    public class UserInterface : Form
    {
        private IniFile ini = new IniFile(AppContext.BaseDirectory + "\\settings.ini");
        private bool dragging = false;
        private int _picOverlayHeight;
        private int _picOverlayWidth;
        private SpeedoProcess _speedoProcess;
        private Button btnInject;
        private PictureBox picScreen;
        private TextBox txtDebugLog;
        private CheckBox cbAlwaysShow;
        private Label label1;
        private Label label2;
        private PictureBox picOverlay;
        private ComboBox cmbResolution;
        private Label label3;
        private Label label5;
        private NumericUpDown nudPosX;
        private NumericUpDown nudPosY;
        private NumericUpDown nudScale;
        private Label label4;

        public UserInterface()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _picOverlayHeight = picOverlay.Height;
            _picOverlayWidth = picOverlay.Width;
            picScreen.Image = new Bitmap(AppContext.BaseDirectory + "\\resources\\imagescreen.png");
            picOverlay.Parent = picScreen;
            picOverlay.Location = new Point(0, 0);
            picOverlay.Image = new Bitmap(AppContext.BaseDirectory + "\\resources\\speedocar.png");
            picOverlay.BackColor = Color.Transparent;
            IniRead();
        }

        private void OnExit(object sender, FormClosingEventArgs e)
        {
            IniSave();
        }

        private void IniSave()
        {
            ini.IniWriteValue("General", "Resolution", cmbResolution.Text);
            ini.IniWriteValue("General", "XPosition", nudPosX.Value.ToString());
            ini.IniWriteValue("General", "YPosition", nudPosY.Value.ToString());
            ini.IniWriteValue("General", "Scale", nudScale.Value.ToString());
            ini.IniWriteValue("General", "AlwaysShow", cbAlwaysShow.Checked.ToString());
        }

        private void IniRead()
        {
            string resolution = ini.IniReadValue("General", "Resolution");
            if (!ResolutionScaler.ReadResolutionString(resolution, picScreen.Width, picScreen.Height))
            {
                cmbResolution.SelectedIndex = 7;
            }
            else
            {
                cmbResolution.Text = resolution;
            }
            if (decimal.TryParse(ini.IniReadValue("General", "Scale"), out decimal scale))
            {
                nudScale.Value = scale;
            }
            else
            {
                nudScale.Value = 1.2M;
            }
            if(decimal.TryParse(ini.IniReadValue("General", "XPosition"), out decimal x))
            {
                nudPosX.Value = x;
            }
            else
            {
                nudPosX.Value = 1488;
            }
            if (decimal.TryParse(ini.IniReadValue("General", "YPosition"), out decimal y))
            {
                nudPosY.Value = y;
            }
            else
            {
                nudPosY.Value = 688;
            }
            if (bool.TryParse(ini.IniReadValue("General", "AlwaysShow"), out bool alwaysShow))
            {
                cbAlwaysShow.Checked = alwaysShow;
            }
            else
            {
                cbAlwaysShow.Checked = false;
            }
        }

        private void PicOverlay_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void PicOverlay_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
        }

        private void PicOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging)
            {
                return;
            }

            Point client = picScreen.PointToClient(Cursor.Position);
            client.X -= picOverlay.Width / 2;
            client.Y -= picOverlay.Height / 2;
            UpdatePosition(client);
            Point point = ResolutionScaler.SetResolutionValues(new Point(picOverlay.Left, picOverlay.Top));

            nudPosX.Value = point.X;
            nudPosY.Value = point.Y;
        }

        private void NudPos_ValueChanged(object sender, EventArgs e)
        {
            Point point = new Point(
                (int)Math.Floor(nudPosX.Value / ResolutionScaler.ResXMultiplier),
                (int)Math.Floor(nudPosY.Value / ResolutionScaler.ResYMultiplier));

            UpdatePosition(point);
        }

        private void UpdatePosition(Point point)
        {
            picOverlay.Left = Math.Max(0, Math.Min(picScreen.Width - picOverlay.Width, point.X));
            picOverlay.Top = Math.Max(0, Math.Min(picScreen.Height - picOverlay.Height, point.Y));
        }

        private void BtnInject_Click(object sender, EventArgs e)
        {
            if (_speedoProcess == null)
            {
                btnInject.Enabled = false;
                AttachProcess();
            }
            else
            {
                _speedoProcess.SpeedoInterface.Disconnect();
                _speedoProcess = null;
            }
            if (_speedoProcess != null)
            {
                btnInject.Text = "Unload Speedometer";
                btnInject.Enabled = true;
            }
            else
            {
                btnInject.Text = "Load Speedometer";
                btnInject.Enabled = true;
            }
        }

        private void AttachProcess()
        {
            foreach (Process process in Process.GetProcessesByName("ASN_App_PcDx9_Final"))
            {
                if (!(process.MainWindowHandle == IntPtr.Zero))
                {
                    SpeedoConfig config = new SpeedoConfig()
                    {
                        PosX = (int)nudPosX.Value,
                        PosY = (int)nudPosY.Value,
                        Scale = (double)nudScale.Value,
                        AlwaysShow = cbAlwaysShow.Checked
                    };
                    SpeedoInterface speedoInterface = new SpeedoInterface();
                    speedoInterface.RemoteMessageEventHandler += new MessageReceivedEvent(SpeedoInterface_RemoteMessage);
                    _speedoProcess = new SpeedoProcess(process, config, speedoInterface);
                    _speedoProcess.SpeedoInterface.Message(MessageType.Debug, "Hook successful!");
                    break;
                }
            }
            Thread.Sleep(10);
            if (_speedoProcess != null)
            {
                return;
            }

            int num = (int)MessageBox.Show("No executable found matching: 'ASN_App_PcDx9_Final'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SpeedoInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            txtDebugLog.Invoke(new Action(() => txtDebugLog.Text = string.Format("{0}\r\n{1}", message, txtDebugLog.Text)));
        }

        private void CmbResolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            picScreen.Height = (int)Math.Ceiling(picScreen.Width / ResolutionScaler.AspectRatio);
            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            NudScale_ValueChanged(null, null);
            Height = picScreen.Bottom + txtDebugLog.Height + 60;
        }

        private void CmbResolution_TextChanged(object sender, EventArgs e)
        {
            if (ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height))
            {
                CmbResolution_SelectedIndexChanged(sender, e);
            }
        }

        private void NudScale_ValueChanged(object sender, EventArgs e)
        {
            SetOverlaySizeScale(nudScale.Value);
        }

        private void SetOverlaySizeScale(decimal scale)
        {
            Point point1 = new Point(picOverlay.Height, picOverlay.Width);
            picOverlay.Width = (int)Math.Ceiling(_picOverlayWidth * scale / ResolutionScaler.ResXMultiplier);
            picOverlay.Height = (int)Math.Ceiling(_picOverlayHeight * scale / ResolutionScaler.ResYMultiplier);
            NudPos_ValueChanged(null, null);

            nudPosX.Maximum = Math.Max(0, Math.Ceiling(ResolutionScaler.ResX - 256 * scale));
            nudPosY.Maximum = Math.Max(0, Math.Ceiling(ResolutionScaler.ResY - 256 * scale));
        }

        private void PicOverlay_Paint(object sender, PaintEventArgs e)
        {
            decimal num = nudScale.Value / ResolutionScaler.ResXMultiplier;
            using (Font font = new Font("Arial", (int)Math.Round(new decimal(14) * num)))
            {
                e.Graphics.DrawString("Drag me!", font, Brushes.Red, new Point((int)Math.Round(new decimal(85) * num), (int)Math.Round(new decimal(120) * num)));
            }
        }

        private void InitializeComponent()
        {
            this.btnInject = new System.Windows.Forms.Button();
            this.picScreen = new System.Windows.Forms.PictureBox();
            this.txtDebugLog = new System.Windows.Forms.TextBox();
            this.cbAlwaysShow = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.picOverlay = new System.Windows.Forms.PictureBox();
            this.cmbResolution = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nudPosX = new System.Windows.Forms.NumericUpDown();
            this.nudPosY = new System.Windows.Forms.NumericUpDown();
            this.nudScale = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPosX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPosY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudScale)).BeginInit();
            this.SuspendLayout();
            // 
            // btnInject
            // 
            this.btnInject.Location = new System.Drawing.Point(10, 59);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(172, 35);
            this.btnInject.TabIndex = 0;
            this.btnInject.Text = "Load Speedometer";
            this.btnInject.UseVisualStyleBackColor = true;
            this.btnInject.Click += new System.EventHandler(this.BtnInject_Click);
            // 
            // picScreen
            // 
            this.picScreen.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.picScreen.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.picScreen.BackColor = System.Drawing.Color.Black;
            this.picScreen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picScreen.Location = new System.Drawing.Point(192, 53);
            this.picScreen.Name = "picScreen";
            this.picScreen.Size = new System.Drawing.Size(800, 600);
            this.picScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picScreen.TabIndex = 2;
            this.picScreen.TabStop = false;
            // 
            // txtDebugLog
            // 
            this.txtDebugLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugLog.Location = new System.Drawing.Point(15, 655);
            this.txtDebugLog.Multiline = true;
            this.txtDebugLog.Name = "txtDebugLog";
            this.txtDebugLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDebugLog.Size = new System.Drawing.Size(985, 90);
            this.txtDebugLog.TabIndex = 16;
            // 
            // cbAlwaysShow
            // 
            this.cbAlwaysShow.AutoSize = true;
            this.cbAlwaysShow.Checked = true;
            this.cbAlwaysShow.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAlwaysShow.Location = new System.Drawing.Point(40, 410);
            this.cbAlwaysShow.Name = "cbAlwaysShow";
            this.cbAlwaysShow.Size = new System.Drawing.Size(107, 23);
            this.cbAlwaysShow.TabIndex = 26;
            this.cbAlwaysShow.Text = "Always Show";
            this.cbAlwaysShow.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 221);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(123, 19);
            this.label1.TabIndex = 30;
            this.label1.Text = "Horizonal Position:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 255);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(110, 19);
            this.label2.TabIndex = 31;
            this.label2.Text = "Vertical Position:";
            // 
            // picOverlay
            // 
            this.picOverlay.BackColor = System.Drawing.Color.Black;
            this.picOverlay.Location = new System.Drawing.Point(443, 117);
            this.picOverlay.Name = "picOverlay";
            this.picOverlay.Size = new System.Drawing.Size(256, 256);
            this.picOverlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOverlay.TabIndex = 32;
            this.picOverlay.TabStop = false;
            this.picOverlay.Paint += new System.Windows.Forms.PaintEventHandler(this.PicOverlay_Paint);
            this.picOverlay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PicOverlay_MouseDown);
            this.picOverlay.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PicOverlay_MouseMove);
            this.picOverlay.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PicOverlay_MouseUp);
            // 
            // cmbResolution
            // 
            this.cmbResolution.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cmbResolution.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbResolution.FormattingEnabled = true;
            this.cmbResolution.Items.AddRange(new object[] {
            "640x480",
            "800x600",
            "1024x768",
            "1280x720",
            "1366x768",
            "1440x900",
            "1600x900",
            "1920x1080",
            "1920x1200",
            "2560x1440",
            "2560x1600",
            "3840x2160"});
            this.cmbResolution.Location = new System.Drawing.Point(30, 149);
            this.cmbResolution.Name = "cmbResolution";
            this.cmbResolution.Size = new System.Drawing.Size(133, 25);
            this.cmbResolution.TabIndex = 33;
            this.cmbResolution.Text = "???x???";
            this.cmbResolution.SelectedIndexChanged += new System.EventHandler(this.CmbResolution_SelectedIndexChanged);
            this.cmbResolution.TextChanged += new System.EventHandler(this.CmbResolution_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 127);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(182, 19);
            this.label3.TabIndex = 34;
            this.label3.Text = "Select/type game resolution:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(50, 314);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 19);
            this.label5.TabIndex = 36;
            this.label5.Text = "Overlay Scale:";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.label4.Location = new System.Drawing.Point(328, 8);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(539, 38);
            this.label4.TabIndex = 40;
            this.label4.Text = "Drag the speedometer below to the location you want it to appear in game. Note: S" +
    "peedometer will not draw if it is in the black area for some resolutions!";
            // 
            // nudPosX
            // 
            this.nudPosX.Location = new System.Drawing.Point(135, 220);
            this.nudPosX.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudPosX.Name = "nudPosX";
            this.nudPosX.Size = new System.Drawing.Size(52, 25);
            this.nudPosX.TabIndex = 41;
            this.nudPosX.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudPosX.ValueChanged += new System.EventHandler(this.NudPos_ValueChanged);
            // 
            // nudPosY
            // 
            this.nudPosY.Location = new System.Drawing.Point(135, 254);
            this.nudPosY.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudPosY.Name = "nudPosY";
            this.nudPosY.Size = new System.Drawing.Size(52, 25);
            this.nudPosY.TabIndex = 42;
            this.nudPosY.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudPosY.ValueChanged += new System.EventHandler(this.NudPos_ValueChanged);
            // 
            // nudScale
            // 
            this.nudScale.DecimalPlaces = 1;
            this.nudScale.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nudScale.Location = new System.Drawing.Point(66, 340);
            this.nudScale.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudScale.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            this.nudScale.Name = "nudScale";
            this.nudScale.Size = new System.Drawing.Size(59, 25);
            this.nudScale.TabIndex = 43;
            this.nudScale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudScale.ValueChanged += new System.EventHandler(this.NudScale_ValueChanged);
            // 
            // UserInterface
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1002, 757);
            this.Controls.Add(this.nudScale);
            this.Controls.Add(this.nudPosY);
            this.Controls.Add(this.nudPosX);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbResolution);
            this.Controls.Add(this.picOverlay);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbAlwaysShow);
            this.Controls.Add(this.txtDebugLog);
            this.Controls.Add(this.picScreen);
            this.Controls.Add(this.btnInject);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "UserInterface";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Speedometer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnExit);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPosX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPosY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudScale)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
