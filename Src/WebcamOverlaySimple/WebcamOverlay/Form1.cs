using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using Point = System.Drawing.Point;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

namespace WebcamOverlay
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll")]
        static extern bool DrawMenuBar(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        public static Bitmap img;
        private FilterInfoCollection CaptureDevice;
        private VideoCaptureDevice FinalFrame;
        private VideoCapabilities[] videoCapabilities;
        private static int height = 300, width, imgheight, imgwidth;
        private static double initratio, ratio, border;
        private Region rg;
        private GraphicsPath gp;
        private static bool getstateminus, getstateplus;
        private const int GWL_STYLE = -16;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_TABSTOP = 0x00010000;
        private const uint WS_VISIBLE = 0x10000000;
        private static int[] wd = { 2, 2, 2, 2 };
        private static int[] wu = { 2, 2, 2, 2 };
        public static void valchanged(int n, bool val)
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
                Task.Run(() => Start());
                AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(AppDomain_UnhandledException);
                System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
                this.TopMost = true;
                CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                FinalFrame = new VideoCaptureDevice(CaptureDevice[0].MonikerString);
                FinalFrame.DesiredFrameRate = 10;
                FinalFrame.DesiredFrameSize = new Size(300, 300);
                videoCapabilities = FinalFrame.VideoCapabilities;
                FinalFrame.VideoResolution = videoCapabilities[1];
                initratio = Convert.ToDouble(FinalFrame.VideoResolution.FrameSize.Width) / Convert.ToDouble(FinalFrame.VideoResolution.FrameSize.Height);
                ratio = 1;
                border = 10f;
                height = 300;
                width = (int)(height * ratio);
                this.Size = new Size(width, height);
                this.ClientSize = new Size(width, height);
                this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - (int)border, (int)border);
                this.pictureBox1.Size = new Size(width, height);
                this.pictureBox1.Location = new Point(0, 0);
                FinalFrame.DesiredFrameSize = new Size((int)(height * ratio), height);
                FinalFrame.SetCameraProperty(CameraControlProperty.Zoom, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Focus, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Exposure, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Iris, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Pan, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Tilt, 0, CameraControlFlags.Manual);
                FinalFrame.SetCameraProperty(CameraControlProperty.Roll, 0, CameraControlFlags.Manual);
                FinalFrame.NewFrame += FinalFrame_NewFrame;
                FinalFrame.Start();
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                System.Drawing.Rectangle r = new System.Drawing.Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
                GraphicsPath gp = new GraphicsPath();
                int d = 50;
                gp.AddArc(r.X, r.Y, d, d, 180, 90);
                gp.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
                gp.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
                gp.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
                this.Region = new Region(gp);
            }
            catch
            {
                this.Close();
            }
        }
        private void Start()
        {
            while (true)
            {
                valchanged(0, GetAsyncKeyState(Keys.PageDown));
                if (wu[0] == 1)
                {
                    int width = Screen.PrimaryScreen.Bounds.Width;
                    int height = Screen.PrimaryScreen.Bounds.Height;
                    IntPtr window = GetForegroundWindow();
                    SetWindowLong(window, GWL_STYLE, WS_SYSMENU);
                    SetWindowPos(window, -2, 0, 0, width, height, 0x0040);
                    DrawMenuBar(window);
                }
                valchanged(1, GetAsyncKeyState(Keys.PageUp));
                if (wu[1] == 1)
                {
                    IntPtr window = GetForegroundWindow();
                    SetWindowLong(window, GWL_STYLE, WS_CAPTION | WS_POPUP | WS_BORDER | WS_SYSMENU | WS_TABSTOP | WS_VISIBLE | WS_OVERLAPPED | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                    DrawMenuBar(window);
                }
                valchanged(2, GetAsyncKeyState(Keys.Subtract));
                if (wu[2] == 1 & !getstateminus)
                {
                    getstateminus = true;
                    this.Opacity = 1;
                }
                else if (wu[2] == 1 & getstateminus)
                {
                    getstateminus = false;
                    this.Opacity = 0.75D;
                }
                valchanged(3, GetAsyncKeyState(Keys.Add));
                if (wu[3] == 1 & !getstateplus)
                {
                    getstateplus = true; 
                    gp = new GraphicsPath();
                    gp.AddEllipse(pictureBox1.DisplayRectangle);
                    rg = new Region(gp);
                    pictureBox1.Region = rg;
                }
                else if (wu[3] == 1 & getstateplus)
                {
                    getstateplus = false;
                    System.Drawing.Rectangle r = new System.Drawing.Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
                    GraphicsPath gp = new GraphicsPath();
                    int d = 50;
                    gp.AddArc(r.X, r.Y, d, d, 180, 90);
                    gp.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
                    gp.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
                    gp.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
                    pictureBox1.Region = new Region(gp);
                }
                System.Threading.Thread.Sleep(100);
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
        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap capture = eventArgs.Frame.Clone() as Bitmap;
                this.pictureBox1.Image = cropImage(capture);
                capture.Dispose();
            }
            catch { }
        }
        private Bitmap cropImage(Bitmap bmp)
        {
            int oldwidth = (int)(initratio * bmp.Height);
            int oldheight = bmp.Height;
            int newWidth = oldheight;
            int newHeight = oldheight;
            Rectangle CropArea = new Rectangle((oldwidth - oldheight) / 2, 0, newWidth, newHeight);
            Bitmap bmpCrop = bmp.Clone(CropArea, bmp.PixelFormat);
            return bmpCrop;
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