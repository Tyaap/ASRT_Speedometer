using Speedo;
using Speedo.Hook;
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
                    speedoInterface.RemoteMessage += new MessageReceivedEvent(SpeedoInterface_RemoteMessage);
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
            this.btnInject = new System.Windows.Forms.Button();
            this.picScreen = new System.Windows.Forms.PictureBox();
            this.txtDebugLog = new System.Windows.Forms.TextBox();
            this.cbAlwaysShow = new System.Windows.Forms.CheckBox();
            this.txtExeName = new System.Windows.Forms.TextBox();
            this.txtPosX = new System.Windows.Forms.TextBox();
            this.txtPosY = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.picOverlay = new System.Windows.Forms.PictureBox();
            this.cmbResolution = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtScale = new System.Windows.Forms.TextBox();
            this.btnIncreaseScale = new System.Windows.Forms.Button();
            this.btnDecreaseScale = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlay)).BeginInit();
            this.SuspendLayout();
            // 
            // btnInject
            // 
            this.btnInject.Location = new System.Drawing.Point(10, 52);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(172, 23);
            this.btnInject.TabIndex = 0;
            this.btnInject.Text = "Load Speedometer";
            this.btnInject.UseVisualStyleBackColor = true;
            this.btnInject.Click += new System.EventHandler(this.BtnInject_Click);
            // 
            // picScreen
            // 
            this.picScreen.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.picScreen.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.picScreen.BackColor = System.Drawing.Color.Black;
            this.picScreen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picScreen.Location = new System.Drawing.Point(190, 90);
            this.picScreen.Name = "picScreen";
            this.picScreen.Size = new System.Drawing.Size(800, 450);
            this.picScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picScreen.TabIndex = 2;
            this.picScreen.TabStop = false;
            // 
            // txtDebugLog
            // 
            this.txtDebugLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugLog.Location = new System.Drawing.Point(15, 640);
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
            this.cbAlwaysShow.Location = new System.Drawing.Point(10, 195);
            this.cbAlwaysShow.Name = "cbAlwaysShow";
            this.cbAlwaysShow.Size = new System.Drawing.Size(89, 17);
            this.cbAlwaysShow.TabIndex = 26;
            this.cbAlwaysShow.Text = "Always Show";
            this.cbAlwaysShow.UseVisualStyleBackColor = true;
            // 
            // txtExeName
            // 
            this.txtExeName.Location = new System.Drawing.Point(10, 13);
            this.txtExeName.Name = "txtExeName";
            this.txtExeName.Size = new System.Drawing.Size(172, 20);
            this.txtExeName.TabIndex = 27;
            this.txtExeName.Text = "ASN_App_PcDx9_Final";
            // 
            // txtPosX
            // 
            this.txtPosX.Location = new System.Drawing.Point(118, 93);
            this.txtPosX.Name = "txtPosX";
            this.txtPosX.Size = new System.Drawing.Size(41, 20);
            this.txtPosX.TabIndex = 28;
            this.txtPosX.Text = "50";
            this.txtPosX.TextChanged += new System.EventHandler(this.TxtPos_TextChanged);
            // 
            // txtPosY
            // 
            this.txtPosY.Location = new System.Drawing.Point(118, 126);
            this.txtPosY.Name = "txtPosY";
            this.txtPosY.Size = new System.Drawing.Size(40, 20);
            this.txtPosY.TabIndex = 29;
            this.txtPosY.Text = "120";
            this.txtPosY.TextChanged += new System.EventHandler(this.TxtPos_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 96);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 30;
            this.label1.Text = "Horizonal Position";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(21, 129);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 31;
            this.label2.Text = "Vertical Position";
            // 
            // picOverlay
            // 
            this.picOverlay.BackColor = System.Drawing.Color.Black;
            this.picOverlay.Location = new System.Drawing.Point(271, 215);
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
            "720x480",
            "800x600",
            "1024x768",
            "1280x720",
            "1366x768",
            "1680x1050",
            "1600x900",
            "1920x1080",
            "2560x1600"});
            this.cmbResolution.Location = new System.Drawing.Point(15, 285);
            this.cmbResolution.Name = "cmbResolution";
            this.cmbResolution.Size = new System.Drawing.Size(158, 21);
            this.cmbResolution.TabIndex = 33;
            this.cmbResolution.SelectedIndexChanged += new System.EventHandler(this.CmbResolution_SelectedIndexChanged);
            this.cmbResolution.TextChanged += new System.EventHandler(this.CmbResolution_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 266);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(160, 13);
            this.label3.TabIndex = 34;
            this.label3.Text = "Select/type in game\'s resolution:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(50, 322);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(76, 13);
            this.label5.TabIndex = 36;
            this.label5.Text = "Overlay Scale:";
            // 
            // txtScale
            // 
            this.txtScale.Location = new System.Drawing.Point(65, 347);
            this.txtScale.Name = "txtScale";
            this.txtScale.Size = new System.Drawing.Size(46, 20);
            this.txtScale.TabIndex = 37;
            this.txtScale.Text = "1.0";
            this.txtScale.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtScale.TextChanged += new System.EventHandler(this.TxtScale_TextChanged);
            // 
            // btnIncreaseScale
            // 
            this.btnIncreaseScale.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnIncreaseScale.Location = new System.Drawing.Point(121, 347);
            this.btnIncreaseScale.Name = "btnIncreaseScale";
            this.btnIncreaseScale.Size = new System.Drawing.Size(19, 20);
            this.btnIncreaseScale.TabIndex = 38;
            this.btnIncreaseScale.Text = "+";
            this.btnIncreaseScale.UseVisualStyleBackColor = true;
            this.btnIncreaseScale.Click += new System.EventHandler(this.BtnIncreaseScale_Click);
            // 
            // btnDecreaseScale
            // 
            this.btnDecreaseScale.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDecreaseScale.Location = new System.Drawing.Point(36, 347);
            this.btnDecreaseScale.Name = "btnDecreaseScale";
            this.btnDecreaseScale.Size = new System.Drawing.Size(19, 20);
            this.btnDecreaseScale.TabIndex = 39;
            this.btnDecreaseScale.Text = "-";
            this.btnDecreaseScale.UseVisualStyleBackColor = true;
            this.btnDecreaseScale.Click += new System.EventHandler(this.BtnDecreaseScale_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(412, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(378, 38);
            this.label4.TabIndex = 40;
            this.label4.Text = "Drag the speedometer below to the location you want it to appear in game. Note: S" +
    "peedometer will not draw if it is in the black area for some resolutions!";
            // 
            // UserInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1002, 742);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnDecreaseScale);
            this.Controls.Add(this.btnIncreaseScale);
            this.Controls.Add(this.txtScale);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbResolution);
            this.Controls.Add(this.picOverlay);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtPosY);
            this.Controls.Add(this.txtPosX);
            this.Controls.Add(this.txtExeName);
            this.Controls.Add(this.cbAlwaysShow);
            this.Controls.Add(this.txtDebugLog);
            this.Controls.Add(this.picScreen);
            this.Controls.Add(this.btnInject);
            this.Name = "UserInterface";
            this.Text = "Speedo Loader";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnExit);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
