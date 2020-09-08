using Speedo;
using Speedo.Hook;
using Remoting;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using static NativeMethods;
using System.Runtime.Serialization.Formatters.Binary;

namespace Speedo_Loader
{
    public class UserInterface : Form
    {
        private string baseDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private IniFile ini;
        private bool dragging = false;
        private bool speedoEnabled = false;
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

        private IpcServerChannel speedoLoaderServer;
        private Interface speedoInterface = new Interface();
        private EventProxy eventProxy = new EventProxy();
        private string channelName = "Speedo";
        private bool speedoIsConnected = false;

        private string theme;
        private TrackBar tbOpacity;
        private Label label6;
        private ComboBox cbTheme;
        private Label label7;
        private Button btnAutoDetectRes;
        private Bitmap overlayImage;
        private System.Windows.Forms.Timer regularTaskTimer = new System.Windows.Forms.Timer { Interval = 100 };

        public UserInterface()
        {
            InitializeComponent();
            ini = new IniFile(baseDirectory + "\\settings.ini");
        }

        private void Form1_Load(object sender, EventArgs e)
        {      
            try
            {
                InitInterfaceServer();
            }
            catch(RemotingException)
            {
                // If the interface fails to start then most likely the speedometer is already running.
                Application.Exit();
            }
            IniRead();
            picOverlay.Parent = picScreen;
            picOverlay.BackColor = Color.Transparent;
            cbTheme.Items.AddRange(GetThemeList(baseDirectory + "\\Themes"));
            CbTheme_TextChanged(null, null);

            btnInject.Click += new System.EventHandler(BtnInject_Click);
            cbAlwaysShow.CheckedChanged += new System.EventHandler(CbAlwaysShow_CheckedChanged);
            picOverlay.Paint += new System.Windows.Forms.PaintEventHandler(PicOverlay_Paint);
            picOverlay.MouseDown += new System.Windows.Forms.MouseEventHandler(PicOverlay_MouseDown);
            picOverlay.MouseMove += new System.Windows.Forms.MouseEventHandler(PicOverlay_MouseMove);
            picOverlay.MouseUp += new System.Windows.Forms.MouseEventHandler(PicOverlay_MouseUp);
            cmbResolution.TextChanged += new System.EventHandler(CmbResolution_TextChanged);
            nudPosX.ValueChanged += new System.EventHandler(NudPos_ValueChanged);
            nudPosY.ValueChanged += new System.EventHandler(NudPos_ValueChanged);
            nudScale.ValueChanged += new System.EventHandler(NudScale_ValueChanged);
            tbOpacity.ValueChanged += new System.EventHandler(TbOpacity_ValueChanged);
            cbTheme.TextChanged += new System.EventHandler(CbTheme_TextChanged);
            btnAutoDetectRes.Click += new System.EventHandler(BtnAutoDetectRes_Click);

            regularTaskTimer.Tick += new System.EventHandler(CheckSpeedoConnected);
            regularTaskTimer.Start();
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
            ini.IniWriteValue("General", "Theme", theme);
            ini.IniWriteValue("General", "Opacity", tbOpacity.Value.ToString());
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
                nudScale.Value = 1.5M;
            }
            if (decimal.TryParse(ini.IniReadValue("General", "XPosition"), out decimal x))
            {
                nudPosX.Value = x;
            }
            else
            {
                nudPosX.Value = 1470;
            }
            if (decimal.TryParse(ini.IniReadValue("General", "YPosition"), out decimal y))
            {
                nudPosY.Value = y;
            }
            else
            {
                nudPosY.Value = 686;
            }
            if (bool.TryParse(ini.IniReadValue("General", "AlwaysShow"), out bool alwaysShow))
            {
                cbAlwaysShow.Checked = alwaysShow;
            }
            else
            {
                cbAlwaysShow.Checked = false;
            }
            theme = ini.IniReadValue("General", "Theme");
            if (string.IsNullOrEmpty(cbTheme.Text) || !Directory.Exists(baseDirectory + "\\Themes\\" + cbTheme.Text))
            {
                theme = "Xenn";
            }
            cbTheme.Text = theme;
            if (byte.TryParse(ini.IniReadValue("General", "Opacity"), out byte opacity))
            {
                tbOpacity.Value = opacity;
            }
            else
            {
                tbOpacity.Value = 255;
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
            UpdatePosition(new Point(
                (int)Math.Floor(nudPosX.Value / ResolutionScaler.ResXMultiplier),
                (int)Math.Floor(nudPosY.Value / ResolutionScaler.ResYMultiplier)));

            UpdateConfig();
        }

        private void UpdatePosition(Point point)
        {
            picOverlay.Left = Math.Max(0, Math.Min(picScreen.Width - picOverlay.Width, point.X));
            picOverlay.Top = Math.Max(0, Math.Min(picScreen.Height - picOverlay.Height, point.Y));
        }

        private void CbAlwaysShow_CheckedChanged(object sender, EventArgs e)
        {
            UpdateConfig();
        }

        private void BtnInject_Click(object sender, EventArgs e)
        {
            if (!speedoIsConnected)
            {
                LoadSpeedo();
                return;
            }
            speedoEnabled = !speedoEnabled;
            btnInject.Text = (speedoEnabled ? "Disable" : "Enable") + " Speedometer";
            UpdateConfig();
        }

        private bool LoadSpeedo()
        {
            Process[] processes = Process.GetProcessesByName("ASN_App_PcDx9_Final");
            if (processes.Length > 0)
            {
                WriteMessageToLog(MessageType.Information, "Start loading speedometer");
                bool result = Inject(processes[0].Id, baseDirectory + "\\Bootstrapper.dll", "LoadManagedProject", baseDirectory + "\\Speedo.dll");
                if (!result)
                {
                    WriteMessageToLog(MessageType.Error, "Speedo.dll injection falied.");
                }
                return result;
            }
            else
            {
                WriteMessageToLog(MessageType.Error, "Could not find ASN_App_PcDx9_Final.exe");
                return false;
            }
        }

        private void InitInterfaceServer()
        {
            speedoLoaderServer = new IpcServerChannel(
                new Hashtable
                {
                    ["name"] = channelName,
                    ["portName"] = channelName
                },
                new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });
            ChannelServices.RegisterChannel(speedoLoaderServer, false);
            RemotingServices.Marshal(speedoInterface, channelName);

            speedoInterface.MessageRecievedEventHandler += MessageRecieved;
        }

        public void CheckSpeedoConnected(object sender, EventArgs e)
        {
            bool result = speedoInterface.Ping();
            if (result && !speedoIsConnected)
            {
                WriteMessageToLog("Speedometer connected!");
                speedoIsConnected = true;
                speedoEnabled = true;
                UpdateConfig();
                btnInject.Text = "Disable Speedometer";
            }
            else if (!result && speedoIsConnected)
            {
                WriteMessageToLog("Speedometer disconnected!");
                speedoIsConnected = false;
                btnInject.Text = "Enable Speedometer";
            }
        }

        private void MessageRecieved(string message)
        {
            this.Invoke(new Action(() => WriteMessageToLog(message)));
        }

        private void WriteMessageToLog(string message)
        {
            txtDebugLog.Text = string.Concat(message + "\r\n", txtDebugLog.Text);
        }

        private void WriteMessageToLog(MessageType messageType, string message)
        {
            WriteMessageToLog(string.Format("{0}: {1}", messageType, message));
        }

        private void UpdateConfig()
        {
            SpeedoConfig speedoConfig = new SpeedoConfig()
            {
                PosX = (int)nudPosX.Value,
                PosY = (int)nudPosY.Value,
                Scale = (float)nudScale.Value,
                AlwaysShow = cbAlwaysShow.Checked,
                Opacity = (byte)tbOpacity.Value,
                Theme = theme,
                Enabled = speedoEnabled
            };

            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, speedoConfig);
                stream.Flush();
                speedoInterface.UpdateConfig(stream.ToArray());
            }
        }

        private string[] GetThemeList(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            DirectoryInfo[] subDir = dir.GetDirectories();
            int count = subDir.Length;
            string[] themes = new string[count];
            for (int i = 0; i < count; i++)
            {
                themes[i] = subDir[i].Name;
            }
            return themes;
        }

        private void CmbResolution_TextChanged(object sender, EventArgs e)
        {
            if (ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height))
            {
                UpdateResolution();
            }
        }

        private void UpdateResolution()
        {
            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            picScreen.Height = (int)Math.Ceiling(picScreen.Width / ResolutionScaler.AspectRatio);
            ResolutionScaler.ReadResolutionString(cmbResolution.Text, picScreen.Width, picScreen.Height);
            NudScale_ValueChanged(null, null);
            Height = picScreen.Bottom + txtDebugLog.Height + 50;
        }


        private void BtnAutoDetectRes_Click(object sender, EventArgs e)
        {
            cmbResolution.Text = GetGameResolution();
        }

        private string GetGameResolution()
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SART\\settings.xml");
                return string.Format("{0}x{1}",
                    xmlDocument.SelectSingleNode("/config/screenmode/width").InnerText,
                    xmlDocument.SelectSingleNode("/config/screenmode/height").InnerText);
            }
            catch
            {
                return "1280x720";
            }
        }

        private void NudScale_ValueChanged(object sender, EventArgs e)
        {
            SetOverlaySizeScale(nudScale.Value);
            UpdateConfig();
        }

        private void TbOpacity_ValueChanged(object sender, EventArgs e)
        {
            picOverlay.Image = AdjustAlpha(overlayImage, tbOpacity.Value / 255f);
            UpdateConfig();
        }

        private void CbTheme_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(cbTheme.Text) && Directory.Exists(baseDirectory + "\\Themes\\" + cbTheme.Text))
            {
                theme = cbTheme.Text;
                picScreen.Image = new Bitmap(baseDirectory + "\\Themes\\" + theme + "\\Game_Preview.png");
                overlayImage = new Bitmap(baseDirectory + "\\Themes\\" + theme + "\\Speedo_Preview.png");
                picOverlay.Image = AdjustAlpha(overlayImage, tbOpacity.Value / 255f);
                UpdateResolution();
            }
        }

        private void NudSmoothingFrames_ValueChanged(object sender, EventArgs e)
        {
            UpdateConfig();
        }

        private void CbSpeedType_TextChanged(object sender, EventArgs e)
        {
            UpdateConfig();
        }

        private void SetOverlaySizeScale(decimal scale)
        {
            Point point1 = new Point(picOverlay.Height, picOverlay.Width);
            picOverlay.Width = (int)Math.Ceiling(overlayImage.Width * scale / ResolutionScaler.ResXMultiplier);
            picOverlay.Height = (int)Math.Ceiling(overlayImage.Height * scale / ResolutionScaler.ResYMultiplier);
            NudPos_ValueChanged(null, null);

            nudPosX.Maximum = Math.Max(0, Math.Ceiling(ResolutionScaler.ResX - overlayImage.Width * scale));
            nudPosY.Maximum = Math.Max(0, Math.Ceiling(ResolutionScaler.ResY - overlayImage.Height * scale));
        }

        private void PicOverlay_Paint(object sender, PaintEventArgs e)
        {
            decimal num = nudScale.Value / ResolutionScaler.ResXMultiplier;
            using (Font font = new Font("Arial", (int)Math.Round(new decimal(14) * num), FontStyle.Bold))
            {
                e.Graphics.DrawString("Drag me!", font, Brushes.Red, new Point((int)Math.Round(new decimal(60) * num), (int)Math.Round(new decimal(90) * num)));
            }
        }

        // Adjust an image's translucency.
        private Bitmap AdjustAlpha(Image image, float translucency)
        {
            // Make the ColorMatrix.
            float t = translucency;
            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, t, 0},
                new float[] {0, 0, 0, 0, 1},
            });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(cm);

            // Draw the image onto the new bitmap while
            // applying the new ColorMatrix.
            Point[] points =
            {
                new Point(0, 0),
                new Point(image.Width, 0),
                new Point(0, image.Height),
            };
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            // Make the result bitmap.
            Bitmap bm = new Bitmap(image.Width, image.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.DrawImage(image, points, rect,
                    GraphicsUnit.Pixel, attributes);
            }

            // Return the result.
            return bm;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserInterface));
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
            this.tbOpacity = new System.Windows.Forms.TrackBar();
            this.label6 = new System.Windows.Forms.Label();
            this.cbTheme = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnAutoDetectRes = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPosX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPosY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).BeginInit();
            this.SuspendLayout();
            // 
            // btnInject
            // 
            this.btnInject.Location = new System.Drawing.Point(6, 6);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(176, 46);
            this.btnInject.TabIndex = 0;
            this.btnInject.Text = "Enable Speedometer";
            this.btnInject.UseVisualStyleBackColor = true;
            // 
            // picScreen
            // 
            this.picScreen.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.picScreen.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.picScreen.BackColor = System.Drawing.Color.Black;
            this.picScreen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picScreen.Location = new System.Drawing.Point(190, 53);
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
            this.txtDebugLog.Location = new System.Drawing.Point(4, 514);
            this.txtDebugLog.Multiline = true;
            this.txtDebugLog.Name = "txtDebugLog";
            this.txtDebugLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDebugLog.Size = new System.Drawing.Size(984, 90);
            this.txtDebugLog.TabIndex = 16;
            // 
            // cbAlwaysShow
            // 
            this.cbAlwaysShow.AutoSize = true;
            this.cbAlwaysShow.Checked = true;
            this.cbAlwaysShow.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAlwaysShow.Location = new System.Drawing.Point(45, 464);
            this.cbAlwaysShow.Name = "cbAlwaysShow";
            this.cbAlwaysShow.Size = new System.Drawing.Size(107, 23);
            this.cbAlwaysShow.TabIndex = 26;
            this.cbAlwaysShow.Text = "Always Show";
            this.cbAlwaysShow.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 177);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(123, 19);
            this.label1.TabIndex = 30;
            this.label1.Text = "Horizonal Position:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 211);
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
            this.picOverlay.Size = new System.Drawing.Size(100, 50);
            this.picOverlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picOverlay.TabIndex = 32;
            this.picOverlay.TabStop = false;
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
            this.cmbResolution.Location = new System.Drawing.Point(32, 92);
            this.cmbResolution.Name = "cmbResolution";
            this.cmbResolution.Size = new System.Drawing.Size(128, 25);
            this.cmbResolution.TabIndex = 33;
            this.cmbResolution.Text = "???x???";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(40, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 19);
            this.label3.TabIndex = 34;
            this.label3.Text = "Game resolution:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(50, 252);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 19);
            this.label5.TabIndex = 36;
            this.label5.Text = "Overlay Scale:";
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.label4.Location = new System.Drawing.Point(325, 8);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(539, 38);
            this.label4.TabIndex = 40;
            this.label4.Text = "Drag the speedometer below to the location you want it to appear in game.\nNote: S" +
    "peedometer will not draw in the black areas!";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // nudPosX
            // 
            this.nudPosX.Location = new System.Drawing.Point(131, 176);
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
            // 
            // nudPosY
            // 
            this.nudPosY.Location = new System.Drawing.Point(131, 208);
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
            // 
            // nudScale
            // 
            this.nudScale.DecimalPlaces = 1;
            this.nudScale.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nudScale.Location = new System.Drawing.Point(66, 278);
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
            // 
            // tbOpacity
            // 
            this.tbOpacity.Location = new System.Drawing.Point(34, 348);
            this.tbOpacity.Maximum = 255;
            this.tbOpacity.Name = "tbOpacity";
            this.tbOpacity.Size = new System.Drawing.Size(123, 45);
            this.tbOpacity.TabIndex = 44;
            this.tbOpacity.TickFrequency = 32;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(42, 324);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(110, 19);
            this.label6.TabIndex = 45;
            this.label6.Text = "Overlay Opacity:";
            // 
            // cbTheme
            // 
            this.cbTheme.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbTheme.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbTheme.FormattingEnabled = true;
            this.cbTheme.Location = new System.Drawing.Point(36, 417);
            this.cbTheme.Name = "cbTheme";
            this.cbTheme.Size = new System.Drawing.Size(121, 25);
            this.cbTheme.TabIndex = 46;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(45, 394);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(104, 19);
            this.label7.TabIndex = 47;
            this.label7.Text = "Overlay Theme:";
            // 
            // btnAutoDetectRes
            // 
            this.btnAutoDetectRes.Location = new System.Drawing.Point(32, 122);
            this.btnAutoDetectRes.Name = "btnAutoDetectRes";
            this.btnAutoDetectRes.Size = new System.Drawing.Size(128, 30);
            this.btnAutoDetectRes.TabIndex = 50;
            this.btnAutoDetectRes.Text = "Auto-detect";
            this.btnAutoDetectRes.UseVisualStyleBackColor = true;
            // 
            // UserInterface
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(993, 613);
            this.Controls.Add(this.btnAutoDetectRes);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cbTheme);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tbOpacity);
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
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
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
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
