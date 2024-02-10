using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Runtime.InteropServices;

namespace WebcamOverlay
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        public static extern bool GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        public static Image img;
        private FilterInfoCollection CaptureDevice;
        private VideoCaptureDevice FinalFrame;
        private VideoCapabilities[] videoCapabilities;
        private static int height = 300, width, imgheight, imgwidth;
        private static bool getstate = false;
        private static double ratio, border;
        private static int[] wd = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static int[] wu = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }

        [Obsolete]
        private void Form1_Shown(object sender, EventArgs e)
        {
            try
            {
                TimeBeginPeriod(1);
                NtSetTimerResolution(1, true, ref CurrentResolution);
                AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(AppDomain_UnhandledException);
                Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
                this.TopMost = true;
                CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                FinalFrame = new VideoCaptureDevice(CaptureDevice[0].MonikerString);
                videoCapabilities = FinalFrame.VideoCapabilities;
                FinalFrame.VideoResolution = videoCapabilities[videoCapabilities.Length - 1];
                FinalFrame.DesiredFrameRate = 15;
                ratio = Convert.ToDouble(FinalFrame.VideoResolution.FrameSize.Width) / Convert.ToDouble(FinalFrame.VideoResolution.FrameSize.Height);
                height = 300;
                width = (int)(height * ratio);
                this.Size = new Size(width, height);
                this.ClientSize = new Size(width, height);
                this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, 10);
                border = 1.00f;
                this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                FinalFrame.DesiredFrameSize = new Size((int)(height * border * ratio), (int)(height * border));
                FinalFrame.SetCameraProperty(CameraControlProperty.Zoom, 600, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Focus, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Exposure, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Iris, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Pan, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Tilt, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Roll, 0, CameraControlFlags.Manual);
                FinalFrame.NewFrame += FinalFrame_NewFrame;
                FinalFrame.Start();
                System.Threading.Thread.Sleep(1000);
                imgheight = img.Height;
                imgwidth = img.Width;
            }
            catch
            {
                this.Close();
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        private void OnKeyDown(Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                const string message = "• Author: Michaël André Franiatte.\n\r\n\r• Contact: michael.franiatte@gmail.com.\n\r\n\r• Publisher: https://github.com/michaelandrefraniatte.\n\r\n\r• Copyrights: All rights reserved, no permissions granted.\n\r\n\r• License: Not open source, not free of charge to use.";
                const string caption = "About";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        public void SetWebcamInputs(Bitmap EditableImg)
        {
            try
            {
                pictureBox1.Image = EditableImg;
            }
            catch { }
        }
        public void AppDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            FormClose();
        }
        public void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            FormClose();
        }
        private void Form1_Activated(object sender, EventArgs e)
        {
            if (this.FormBorderStyle == FormBorderStyle.FixedToolWindow)
                return;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        }
        private void Form1_Deactivate(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;
        }
        void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            img = (Bitmap)eventArgs.Frame.Clone();
            try
            {
                valchanged(1, GetAsyncKeyState(Keys.NumPad9));
                if (wd[1] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, 10);
                }
                valchanged(2, GetAsyncKeyState(Keys.NumPad8));
                if (wd[2] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - width / 2, 10);
                }
                valchanged(3, GetAsyncKeyState(Keys.NumPad7));
                if (wd[3] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(10, 10);
                }
                valchanged(4, GetAsyncKeyState(Keys.NumPad4));
                if (wd[4] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(10, Screen.PrimaryScreen.Bounds.Height / 2 - height / 2);
                }
                valchanged(5, GetAsyncKeyState(Keys.NumPad1));
                if (wd[5] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(10, Screen.PrimaryScreen.Bounds.Height - height - 10);
                }
                valchanged(6, GetAsyncKeyState(Keys.NumPad2));
                if (wd[6] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - width / 2, Screen.PrimaryScreen.Bounds.Height - height - 10);
                }
                valchanged(7, GetAsyncKeyState(Keys.NumPad3));
                if (wd[7] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, Screen.PrimaryScreen.Bounds.Height - height - 10);
                }
                valchanged(8, GetAsyncKeyState(Keys.NumPad6));
                if (wd[8] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, (int)(double)Screen.PrimaryScreen.Bounds.Height / 2 - height / 2);
                }
                valchanged(9, GetAsyncKeyState(Keys.NumPad5));
                if (wd[9] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Height * border * ratio), (int)(Screen.PrimaryScreen.Bounds.Height * border));
                    this.pictureBox1.Location = new Point((int)(Screen.PrimaryScreen.Bounds.Height * ratio / 2) - this.pictureBox1.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 - this.pictureBox1.Height / 2);
                    this.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Height * ratio), Screen.PrimaryScreen.Bounds.Height);
                    this.ClientSize = new Size((int)(Screen.PrimaryScreen.Bounds.Height * ratio), Screen.PrimaryScreen.Bounds.Height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - (int)(Screen.PrimaryScreen.Bounds.Height * ratio / 2), 0);
                }
                valchanged(10, GetAsyncKeyState(Keys.Multiply));
                if (wd[10] == 1)
                {
                    this.Size = new Size(0, 0);
                    this.ClientSize = new Size(0, 0);
                    this.Location = new Point(0, 0);
                }
                valchanged(11, GetAsyncKeyState(Keys.Subtract));
                if (wd[11] == 1)
                {
                    if (!getstate)
                    {
                        getstate = true;
                        this.Opacity = 1;
                    }
                    else
                    {
                        getstate = false;
                        this.Opacity = 0.5D;
                    }
                }
            }
            catch { }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                Bitmap bmp = new Bitmap(img);
                SetWebcamInputs(bmp);
            }
            catch { }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            FormClose();
        }
        private void FormClose()
        {
            try
            {
                FinalFrame.NewFrame -= FinalFrame_NewFrame;
                System.Threading.Thread.Sleep(1000);
                if (FinalFrame.IsRunning)
                    FinalFrame.Stop();
            }
            catch { }
        }
    }
}