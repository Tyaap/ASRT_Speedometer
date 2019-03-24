using Speedo;
using Speedo.Interface;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Speedo_Loader
{
    public class UserInterface : Form
    {
        private IniFile ini = new IniFile(AppContext.BaseDirectory + "\\settings.ini");
        private bool Dragging = false;
        private int processId = 0;
        private IContainer components = null;
        private int _picOverlayHeight;
        private int _picOverlayWidth;
        private bool ignoreTextChanged;
        private Process _process;
        private SpeedoProcess _speedoProcess;
        private Button btnInject;
        private PictureBox picScreen;
        private TextBox txtDebugLog;
        private CheckBox cbAlwaysShow;
        private TextBox txtExeName;
        private TextBox txtPosX;
        private TextBox txtPosY;
        private Label label1;
        private Label label2;
        private PictureBox picOverlay;
        private ComboBox cmbResolution;
        private Label label3;
        private Label label5;
        private TextBox txtScale;
        private Button btnIncreaseScale;
        private Button btnDecreaseScale;
        private Label label4;

        public UserInterface()
        {
            InitializeComponent();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("en-us");
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _picOverlayHeight = picOverlay.Height;
            _picOverlayWidth = picOverlay.Width;
            picScreen.Image = new Bitmap(AppContext.BaseDirectory + "\\resources\\imagescreen.jpg");
            picOverlay.Parent = picScreen;
            picOverlay.Location = new Point(0, 0);
            IniRead();
            picOverlay.Image = new Bitmap(AppContext.BaseDirectory + "\\resources\\speedorough.png"); ;
            picOverlay.BackColor = Color.Transparent;
            if (!(cmbResolution.Text == ""))
            {
                return;
            }

            cmbResolution.SelectedIndex = 0;
        }

        private void OnExit(object sender, FormClosingEventArgs e)
        {
            IniSave();
        }

        private void IniSave()
        {
            ini.IniWriteValue("General", "Resolution", cmbResolution.Text);
            ini.IniWriteValue("General", "XPosition", txtPosX.Text);
            ini.IniWriteValue("General", "YPosition", txtPosY.Text);
            ini.IniWriteValue("General", "Scale", txtScale.Text);
        }

        private void IniRead()
        {
            cmbResolution.Text = ini.IniReadValue("General", "Resolution");
            txtScale.Text = ini.IniReadValue("General", "Scale");
            ignoreTextChanged = true;
            txtPosX.Text = ini.IniReadValue("General", "XPosition");
            txtPosY.Text = ini.IniReadValue("General", "YPosition");
            ignoreTextChanged = false;
            TxtPos_TextChanged(null, null);
        }

        private void PicOverlay_MouseUp(object sender, MouseEventArgs e)
        {
            Point point = ResolutionScaler.SetResolutionValues(new Point()
            {
                X = picOverlay.Left,
                Y = picOverlay.Top
            });
            if (!ignoreTextChanged)
            {
                txtPosX.Text = point.X.ToString();
                txtPosY.Text = point.Y.ToString();
            }
            Dragging = false;
        }

        private void PicOverlay_MouseDown(object sender, MouseEventArgs e)
        {
            Dragging = true;
        }

        private void PicOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Dragging)
            {
                return;
            }

            Point client = picScreen.PointToClient(Cursor.Position);
            client.X -= picOverlay.Width / 2;
            client.Y -= picOverlay.Height / 2;
            UpdatePosition(client);
        }

        private void TxtPos_TextChanged(object sender, EventArgs e)
        {
            if (Dragging || ignoreTextChanged)
            {
                return;
            }

            string text1 = txtPosX.Text;
            string text2 = txtPosY.Text;
            Point point = new Point();
            try
            {
                Size size;
                int num1;
                if (int.Parse(txtPosX.Text) / ResolutionScaler.ResXMultiplier >= 0.0)
                {
                    double num2 = int.Parse(txtPosX.Text) / ResolutionScaler.ResXMultiplier;
                    size = picScreen.Size;
                    double width = size.Width;
                    num1 = num2 > width ? 1 : 0;
                }
                else
                {
                    num1 = 1;
                }

                if (num1 == 0)
                {
                    point.X = Convert.ToInt32(Math.Round(int.Parse(txtPosX.Text) / ResolutionScaler.ResXMultiplier, 0));
                }
                else
                {
                    txtPosX.Text = text1;
                }

                int num3;
                if (int.Parse(txtPosY.Text) / ResolutionScaler.ResYMultiplier >= 0.0)
                {
                    double num2 = int.Parse(txtPosY.Text) / ResolutionScaler.ResYMultiplier;
                    size = picScreen.Size;
                    double height = size.Height;
                    num3 = num2 > height ? 1 : 0;
                }
                else
                {
                    num3 = 1;
                }

                if (num3 == 0)
                {
                    point.Y = Convert.ToInt32(Math.Round(int.Parse(txtPosY.Text) / ResolutionScaler.ResYMultiplier, 0));
                }
                else
                {
                    txtPosY.Text = text2;
                }

                if (point.X >= 0 && point.Y >= 0)
                {
                    UpdatePosition(point);
                }
            }
            catch
            {
                txtPosX.Text = "0";
                txtPosY.Text = "0";
            }
        }

        private static double GCD(int a, int b)
        {
            int num;
            for (; b != 0; b = num)
            {
                num = a % b;
                a = b;
            }
            return a;
        }

        private void UpdatePosition(Point point)
        {
            if (point.X < 0 || point.X > picScreen.Width - picOverlay.Width || point.Y < 0 || point.Y > picScreen.Height - picOverlay.Height)
            {
                return;
            }

            picOverlay.Left = point.X;
            picOverlay.Top = point.Y;
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
            string text = txtExeName.Text;
            foreach (Process process in Process.GetProcessesByName(text))
            {
                if (!(process.MainWindowHandle == IntPtr.Zero))
                {
                    SpeedoConfig config = new SpeedoConfig()
                    {
                        PosX = int.Parse(txtPosX.Text),
                        PosY = int.Parse(txtPosY.Text),
                        Scale = double.Parse(txtScale.Text),
                        AlwaysShow = cbAlwaysShow.Checked
                    };
                    processId = process.Id;
                    _process = process;
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

            int num = (int)MessageBox.Show("No executable found matching: '" + text + "'");
        }

        private void SpeedoInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            txtDebugLog.Invoke(new Action(() => txtDebugLog.Text = string.Format("{0}\r\n{1}", message, txtDebugLog.Text)));
        }

        private void ScreenshotManager_OnScreenshotDebugMessage(int clientPID, string message)
        {
            txtDebugLog.Invoke(new Action(() => txtDebugLog.Text = string.Format("{0}:{1}\r\n{2}", clientPID, message, txtDebugLog.Text)));
        }

        private void CmbResolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            int height = picScreen.Height;
            picScreen.Height = (int)Math.Round(picScreen.Width / ResolutionScaler.AspectRatio, 0);
            picScreen.Top += (height - picScreen.Height) / 2;
            if (picOverlay.Top < picScreen.Top)
            {
                picOverlay.Top = picScreen.Top;
            }

            Point res = new Point
            {
                X = picOverlay.Left - picScreen.Left,
                Y = picOverlay.Top - picScreen.Top
            };
            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            Point point = ResolutionScaler.SetResolutionValues(res);
            TextBox txtPosX = this.txtPosX;
            int num = point.X;
            string str1 = num.ToString();
            txtPosX.Text = str1;
            TextBox txtPosY = this.txtPosY;
            num = point.Y;
            string str2 = num.ToString();
            txtPosY.Text = str2;
            TxtScale_TextChanged(null, null);
        }

        private void CmbResolution_TextChanged(object sender, EventArgs e)
        {
            string[] strArray = cmbResolution.Text.Split('x', 'X', '*');
            if (cmbResolution.Text.Length == 0)
            {
                return;
            }

            foreach (string s in strArray)
            {
                if (s.Length > 0 && (!int.TryParse(s, out int result) || strArray.Length > 2))
                {
                    cmbResolution.Text = cmbResolution.Text.Remove(cmbResolution.SelectionStart - 1);
                    cmbResolution.SelectionStart = cmbResolution.Text.Length;
                    return;
                }
            }
            if (strArray.Length != 2 || strArray[1].Length <= 2 || int.Parse(strArray[0]) < 640 || int.Parse(strArray[1]) < 480)
            {
                return;
            }

            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            CmbResolution_SelectedIndexChanged(sender, e);
        }

        private void TxtScale_TextChanged(object sender, EventArgs e)
        {
            if (txtScale.Text.Length > 0)
            {
                if (double.TryParse(txtScale.Text, out double result) && (result < 3.1 && result > 0.1))
                {
                    SetOverlaySizeScale(result);
                }
                else
                {
                    try
                    {
                        txtScale.Text = txtScale.Text.Remove(txtScale.SelectionStart - 1);
                        txtScale.SelectionStart = txtScale.Text.Length;
                    }
                    catch
                    {
                        txtScale.Text = "0.2";
                    }
                }
            }
            else
            {
                txtScale.Text = "0.2";
            }
        }

        private void SetOverlaySizeScale(double scale)
        {
            Point point1 = new Point(picOverlay.Height, picOverlay.Width);
            picOverlay.Width = (int)Math.Round(_picOverlayWidth * scale / ResolutionScaler.ResXMultiplier, 0);
            picOverlay.Height = (int)Math.Round(_picOverlayHeight * scale / ResolutionScaler.ResYMultiplier, 0);
            if (picOverlay.Right > picScreen.Width)
            {
                picOverlay.Left = picScreen.Width - picOverlay.Width - 1;
            }

            if (picOverlay.Bottom > picScreen.Height)
            {
                picOverlay.Top = picScreen.Height - picOverlay.Height - 1;
            }

            if (picOverlay.Left < 0)
            {
                picOverlay.Left = 0;
            }

            if (picOverlay.Top < 0)
            {
                picOverlay.Top = 0;
            }

            Point point2 = ResolutionScaler.SetResolutionValues(new Point(picOverlay.Left, picOverlay.Top));
            TextBox txtPosX = this.txtPosX;
            int num = point2.X;
            string str1 = num.ToString();
            txtPosX.Text = str1;
            TextBox txtPosY = this.txtPosY;
            num = point2.Y;
            string str2 = num.ToString();
            txtPosY.Text = str2;
        }

        private void BtnIncreaseScale_Click(object sender, EventArgs e)
        {
            double num = double.Parse(txtScale.Text);
            if (num >= 3.0)
            {
                return;
            }

            txtScale.Text = (num + 0.1).ToString();
        }

        private void BtnDecreaseScale_Click(object sender, EventArgs e)
        {
            double num = double.Parse(txtScale.Text);
            if (num <= 0.2)
            {
                return;
            }

            txtScale.Text = (num - 0.1).ToString();
        }

        private void PicOverlay_Paint(object sender, PaintEventArgs e)
        {
            decimal num = decimal.Parse(txtScale.Text) / (decimal)ResolutionScaler.ResXMultiplier;
            using (Font font = new Font("Arial", (int)Math.Round(new decimal(14) * num)))
            {
                e.Graphics.DrawString("Drag me!", font, Brushes.Red, new Point((int)Math.Round(new decimal(85) * num), (int)Math.Round(new decimal(120) * num)));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnInject = new System.Windows.Forms.Button();
            picScreen = new System.Windows.Forms.PictureBox();
            txtDebugLog = new System.Windows.Forms.TextBox();
            cbAlwaysShow = new System.Windows.Forms.CheckBox();
            txtExeName = new System.Windows.Forms.TextBox();
            txtPosX = new System.Windows.Forms.TextBox();
            txtPosY = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            picOverlay = new System.Windows.Forms.PictureBox();
            cmbResolution = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            txtScale = new System.Windows.Forms.TextBox();
            btnIncreaseScale = new System.Windows.Forms.Button();
            btnDecreaseScale = new System.Windows.Forms.Button();
            label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(picScreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(picOverlay)).BeginInit();
            SuspendLayout();
            // 
            // btnInject
            // 
            btnInject.Location = new System.Drawing.Point(10, 52);
            btnInject.Name = "btnInject";
            btnInject.Size = new System.Drawing.Size(172, 23);
            btnInject.TabIndex = 0;
            btnInject.Text = "Load Speedometer";
            btnInject.UseVisualStyleBackColor = true;
            btnInject.Click += new System.EventHandler(BtnInject_Click);
            // 
            // picScreen
            // 
            picScreen.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            picScreen.Anchor = System.Windows.Forms.AnchorStyles.None;
            picScreen.BackColor = System.Drawing.Color.Black;
            picScreen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picScreen.Location = new System.Drawing.Point(190, 90);
            picScreen.Name = "picScreen";
            picScreen.Size = new System.Drawing.Size(800, 450);
            picScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            picScreen.TabIndex = 2;
            picScreen.TabStop = false;
            // 
            // txtDebugLog
            // 
            txtDebugLog.Anchor = ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right);
            txtDebugLog.Location = new System.Drawing.Point(15, 640);
            txtDebugLog.Multiline = true;
            txtDebugLog.Name = "txtDebugLog";
            txtDebugLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txtDebugLog.Size = new System.Drawing.Size(985, 90);
            txtDebugLog.TabIndex = 16;
            // 
            // cbAlwaysShow
            // 
            cbAlwaysShow.AutoSize = true;
            cbAlwaysShow.Checked = true;
            cbAlwaysShow.CheckState = System.Windows.Forms.CheckState.Checked;
            cbAlwaysShow.Location = new System.Drawing.Point(10, 195);
            cbAlwaysShow.Name = "cbAlwaysShow";
            cbAlwaysShow.Size = new System.Drawing.Size(89, 17);
            cbAlwaysShow.TabIndex = 26;
            cbAlwaysShow.Text = "Always Show";
            cbAlwaysShow.UseVisualStyleBackColor = true;
            // 
            // txtExeName
            // 
            txtExeName.Location = new System.Drawing.Point(10, 13);
            txtExeName.Name = "txtExeName";
            txtExeName.Size = new System.Drawing.Size(172, 20);
            txtExeName.TabIndex = 27;
            txtExeName.Text = "ASN_App_PcDx9_Final";
            // 
            // txtPosX
            // 
            txtPosX.Location = new System.Drawing.Point(118, 93);
            txtPosX.Name = "txtPosX";
            txtPosX.Size = new System.Drawing.Size(41, 20);
            txtPosX.TabIndex = 28;
            txtPosX.Text = "50";
            txtPosX.TextChanged += new System.EventHandler(TxtPos_TextChanged);
            // 
            // txtPosY
            // 
            txtPosY.Location = new System.Drawing.Point(118, 126);
            txtPosY.Name = "txtPosY";
            txtPosY.Size = new System.Drawing.Size(40, 20);
            txtPosY.TabIndex = 29;
            txtPosY.Text = "120";
            txtPosY.TextChanged += new System.EventHandler(TxtPos_TextChanged);
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 96);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(91, 13);
            label1.TabIndex = 30;
            label1.Text = "Horizonal Position";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(21, 129);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(82, 13);
            label2.TabIndex = 31;
            label2.Text = "Vertical Position";
            // 
            // picOverlay
            // 
            picOverlay.BackColor = System.Drawing.Color.Black;
            picOverlay.Location = new System.Drawing.Point(271, 215);
            picOverlay.Name = "picOverlay";
            picOverlay.Size = new System.Drawing.Size(256, 256);
            picOverlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            picOverlay.TabIndex = 32;
            picOverlay.TabStop = false;
            picOverlay.Paint += new System.Windows.Forms.PaintEventHandler(PicOverlay_Paint);
            picOverlay.MouseDown += new System.Windows.Forms.MouseEventHandler(PicOverlay_MouseDown);
            picOverlay.MouseMove += new System.Windows.Forms.MouseEventHandler(PicOverlay_MouseMove);
            picOverlay.MouseUp += new System.Windows.Forms.MouseEventHandler(PicOverlay_MouseUp);
            // 
            // cmbResolution
            // 
            cmbResolution.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            cmbResolution.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            cmbResolution.FormattingEnabled = true;
            cmbResolution.Items.AddRange(new object[] {
            "640x480",
            "720x480",
            "800x600",
            "1024x768",
            "1280x720",
            "1366x768",
            "1680x1050",
            "1600x900",
            "1920x1080",
            "2560x1600"});
            cmbResolution.Location = new System.Drawing.Point(15, 285);
            cmbResolution.Name = "cmbResolution";
            cmbResolution.Size = new System.Drawing.Size(158, 21);
            cmbResolution.TabIndex = 33;
            cmbResolution.SelectedIndexChanged += new System.EventHandler(CmbResolution_SelectedIndexChanged);
            cmbResolution.TextChanged += new System.EventHandler(CmbResolution_TextChanged);
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(13, 266);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(160, 13);
            label3.TabIndex = 34;
            label3.Text = "Select/type in game\'s resolution:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(50, 322);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(76, 13);
            label5.TabIndex = 36;
            label5.Text = "Overlay Scale:";
            // 
            // txtScale
            // 
            txtScale.Location = new System.Drawing.Point(65, 347);
            txtScale.Name = "txtScale";
            txtScale.Size = new System.Drawing.Size(46, 20);
            txtScale.TabIndex = 37;
            txtScale.Text = "1.0";
            txtScale.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            txtScale.TextChanged += new System.EventHandler(TxtScale_TextChanged);
            // 
            // btnIncreaseScale
            // 
            btnIncreaseScale.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnIncreaseScale.Location = new System.Drawing.Point(121, 347);
            btnIncreaseScale.Name = "btnIncreaseScale";
            btnIncreaseScale.Size = new System.Drawing.Size(19, 20);
            btnIncreaseScale.TabIndex = 38;
            btnIncreaseScale.Text = "+";
            btnIncreaseScale.UseVisualStyleBackColor = true;
            btnIncreaseScale.Click += new System.EventHandler(BtnIncreaseScale_Click);
            // 
            // btnDecreaseScale
            // 
            btnDecreaseScale.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            btnDecreaseScale.Location = new System.Drawing.Point(36, 347);
            btnDecreaseScale.Name = "btnDecreaseScale";
            btnDecreaseScale.Size = new System.Drawing.Size(19, 20);
            btnDecreaseScale.TabIndex = 39;
            btnDecreaseScale.Text = "-";
            btnDecreaseScale.UseVisualStyleBackColor = true;
            btnDecreaseScale.Click += new System.EventHandler(BtnDecreaseScale_Click);
            // 
            // label4
            // 
            label4.Location = new System.Drawing.Point(412, 9);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(378, 38);
            label4.TabIndex = 40;
            label4.Text = "Drag the speedometer below to the location you want it to appear in game. Note: S" +
    "peedometer will not draw if it is in the black area for some resolutions!";
            // 
            // UserInterface
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1002, 742);
            Controls.Add(label4);
            Controls.Add(btnDecreaseScale);
            Controls.Add(btnIncreaseScale);
            Controls.Add(txtScale);
            Controls.Add(label5);
            Controls.Add(label3);
            Controls.Add(cmbResolution);
            Controls.Add(picOverlay);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtPosY);
            Controls.Add(txtPosX);
            Controls.Add(txtExeName);
            Controls.Add(cbAlwaysShow);
            Controls.Add(txtDebugLog);
            Controls.Add(picScreen);
            Controls.Add(btnInject);
            Name = "UserInterface";
            Text = "Speedo Loader";
            FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnExit);
            Load += new System.EventHandler(Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(picScreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(picOverlay)).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }
    }
}
