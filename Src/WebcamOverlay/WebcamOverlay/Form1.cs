using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using NAudio.Wave;
using CSCore.Streams;
using CSCore.SoundIn;
using CSCore;
using CSCore.DSP;
using WinformsVisualization.Visualization;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using NAudio.Extras;
using System.Data;
using System.Text;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;
using Brush = System.Drawing.Brush;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using RectangleF = System.Drawing.RectangleF;

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
        public int numBars = 20;
        public float[] barData = new float[20];
        public int minFreq = 1;
        public int maxFreq = 20000;
        public int barSpacing = 0;
        public bool logScale = true;
        public bool isAverage = false;
        public float highScaleAverage = 1.0f;
        public float highScaleNotAverage = 2.0f;
        public LineSpectrum lineSpectrum;
        public WasapiCapture capture;
        public FftSize fftSize;
        public float[] fftBuffer;
        public BasicSpectrumProvider spectrumProvider;
        public IWaveSource finalSource;
        public static Brush brush = (Brush)Brushes.Red;
        public static Image img;
        private FilterInfoCollection CaptureDevice;
        private VideoCaptureDevice FinalFrame;
        private VideoCapabilities[] videoCapabilities;
        private static int height = 300, width, imgheight, imgwidth;
        private static bool getstate = false;
        private static double ratio, border;
        private static WindowRenderTarget target;
        private static SharpDX.Direct2D1.Factory1 fact = new SharpDX.Direct2D1.Factory1();
        private static RenderTargetProperties renderProp;
        private static HwndRenderTargetProperties winProp;
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
                GetAudioByteArray();
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
                if (System.IO.File.Exists("bckg.gif"))
                {
                    this.pictureBox1.Image = new Bitmap("bckg.gif");
                    border = 0.96f;
                }
                else
                {
                    border = 1.00f;
                }
                this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox1.Size = new Size(width, height);
                this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                this.pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
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
                InitDisplayCapture(this.pictureBox2.Handle);
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
                DisplayCapture(EditableImg);
            }
            catch { }
        }
        private static void InitDisplayCapture(IntPtr handle)
        {
            renderProp = new RenderTargetProperties()
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_DEFAULT,
                PixelFormat = new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                Type = RenderTargetType.Hardware,
                Usage = RenderTargetUsage.None
            };
            winProp = new HwndRenderTargetProperties()
            {
                Hwnd = handle,
                PixelSize = new Size2(imgwidth, imgheight),
                PresentOptions = PresentOptions.Immediately
            };
            target = new WindowRenderTarget(fact, renderProp, winProp);
        }
        private static void DisplayCapture(Bitmap image1)
        {
            using (var bmp = image1)
            {
                System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                SharpDX.DataStream stream = new SharpDX.DataStream(bmpData.Scan0, bmpData.Stride * bmpData.Height, true, false);
                SharpDX.Direct2D1.PixelFormat pFormat = new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
                SharpDX.Direct2D1.BitmapProperties bmpProps = new SharpDX.Direct2D1.BitmapProperties(pFormat);
                SharpDX.Direct2D1.Bitmap result = new SharpDX.Direct2D1.Bitmap(target, new SharpDX.Size2(imgwidth, imgheight), stream, bmpData.Stride, bmpProps);
                bmp.UnlockBits(bmpData);
                stream.Dispose();
                bmp.Dispose();
                target.BeginDraw();
                target.Clear(new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 1f));
                target.DrawBitmap(result, 1.0f, BitmapInterpolationMode.NearestNeighbor);
                target.EndDraw();
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
        void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            img = (Bitmap)eventArgs.Frame.Clone();
            try
            {
                valchanged(1, GetAsyncKeyState(Keys.NumPad9));
                if (wd[1] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, 10);
                }
                valchanged(2, GetAsyncKeyState(Keys.NumPad8));
                if (wd[2] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - width / 2, 10);
                }
                valchanged(3, GetAsyncKeyState(Keys.NumPad7));
                if (wd[3] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(10, 10);
                }
                valchanged(4, GetAsyncKeyState(Keys.NumPad4));
                if (wd[4] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(10, Screen.PrimaryScreen.Bounds.Height / 2 - height / 2);
                }
                valchanged(5, GetAsyncKeyState(Keys.NumPad1));
                if (wd[5] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(10, Screen.PrimaryScreen.Bounds.Height - height - 10);
                }
                valchanged(6, GetAsyncKeyState(Keys.NumPad2));
                if (wd[6] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - width / 2, Screen.PrimaryScreen.Bounds.Height - height - 10);
                }
                valchanged(7, GetAsyncKeyState(Keys.NumPad3));
                if (wd[7] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, Screen.PrimaryScreen.Bounds.Height - height - 10);
                }
                valchanged(8, GetAsyncKeyState(Keys.NumPad6));
                if (wd[8] == 1)
                {
                    this.pictureBox1.Size = new Size(width, height);
                    this.pictureBox1.Location = new Point((width / 2) - (this.pictureBox1.Width / 2), (height / 2) - (this.pictureBox1.Height / 2));
                    this.pictureBox2.Size = new Size((int)(height * border * ratio), (int)(height * border));
                    this.pictureBox2.Location = new Point((width / 2) - (this.pictureBox2.Width / 2), (height / 2) - (this.pictureBox2.Height / 2));
                    this.Size = new Size(width, height);
                    this.ClientSize = new Size(width, height);
                    this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, (int)(double)Screen.PrimaryScreen.Bounds.Height / 2 - height / 2);
                }
                valchanged(9, GetAsyncKeyState(Keys.NumPad5));
                if (wd[9] == 1)
                {
                    this.pictureBox1.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Height * ratio), Screen.PrimaryScreen.Bounds.Height);
                    this.pictureBox1.Location = new Point(0, 0);
                    this.pictureBox2.Size = new Size((int)(Screen.PrimaryScreen.Bounds.Height * border * ratio), (int)(Screen.PrimaryScreen.Bounds.Height * border));
                    this.pictureBox2.Location = new Point((int)(Screen.PrimaryScreen.Bounds.Height * ratio / 2) - this.pictureBox2.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 - this.pictureBox2.Height / 2);
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
                ComputeData();
                Bitmap bmp = new Bitmap(img);
                Graphics graphics = Graphics.FromImage(bmp as Image);
                Random rnd = new Random();
                int bar1 = Convert.ToInt32(barData[0] * 100f);
                int bar2 = Convert.ToInt32(barData[1] * 100f);
                int bar3 = Convert.ToInt32(barData[2] * 100f);
                int bar4 = Convert.ToInt32(barData[3] * 100f);
                int bar5 = Convert.ToInt32(barData[4] * 100f);
                int bar6 = Convert.ToInt32(barData[5] * 100f);
                int bar7 = Convert.ToInt32(barData[6] * 100f);
                int bar8 = Convert.ToInt32(barData[7] * 100f);
                int bar9 = Convert.ToInt32(barData[8] * 100f);
                int bar10 = Convert.ToInt32(barData[9] * 100f);
                int bar11 = Convert.ToInt32(barData[10] * 100f);
                int bar12 = Convert.ToInt32(barData[11] * 100f);
                int bar13 = Convert.ToInt32(barData[12] * 100f);
                int bar14 = Convert.ToInt32(barData[13] * 100f);
                int bar15 = Convert.ToInt32(barData[14] * 100f);
                int bar16 = Convert.ToInt32(barData[15] * 100f);
                int bar17 = Convert.ToInt32(barData[16] * 100f);
                int bar18 = Convert.ToInt32(barData[17] * 100f);
                int bar19 = Convert.ToInt32(barData[18] * 100f);
                int bar20 = Convert.ToInt32(barData[19] * 100f);
                graphics.FillRectangle(brush, 0 * img.Width / 20f + 0.5f, img.Height - bar1, img.Width / 20 - 1, bar1);
                graphics.FillRectangle(brush, 1 * img.Width / 20f + 0.5f, img.Height - bar2, img.Width / 20 - 1, bar2);
                graphics.FillRectangle(brush, 2 * img.Width / 20f + 0.5f, img.Height - bar3, img.Width / 20 - 1, bar3);
                graphics.FillRectangle(brush, 3 * img.Width / 20f + 0.5f, img.Height - bar4, img.Width / 20 - 1, bar4);
                graphics.FillRectangle(brush, 4 * img.Width / 20f + 0.5f, img.Height - bar5, img.Width / 20 - 1, bar5);
                graphics.FillRectangle(brush, 5 * img.Width / 20f + 0.5f, img.Height - bar6, img.Width / 20 - 1, bar6);
                graphics.FillRectangle(brush, 6 * img.Width / 20f + 0.5f, img.Height - bar7, img.Width / 20 - 1, bar7);
                graphics.FillRectangle(brush, 7 * img.Width / 20f + 0.5f, img.Height - bar8, img.Width / 20 - 1, bar8);
                graphics.FillRectangle(brush, 8 * img.Width / 20f + 0.5f, img.Height - bar9, img.Width / 20 - 1, bar9);
                graphics.FillRectangle(brush, 9 * img.Width / 20f + 0.5f, img.Height - bar10, img.Width / 20 - 1, bar10);
                graphics.FillRectangle(brush, 10 * img.Width / 20f + 0.5f, img.Height - bar11, img.Width / 20 - 1, bar11);
                graphics.FillRectangle(brush, 11 * img.Width / 20f + 0.5f, img.Height - bar12, img.Width / 20 - 1, bar12);
                graphics.FillRectangle(brush, 12 * img.Width / 20f + 0.5f, img.Height - bar13, img.Width / 20 - 1, bar13);
                graphics.FillRectangle(brush, 13 * img.Width / 20f + 0.5f, img.Height - bar14, img.Width / 20 - 1, bar14);
                graphics.FillRectangle(brush, 14 * img.Width / 20f + 0.5f, img.Height - bar15, img.Width / 20 - 1, bar15);
                graphics.FillRectangle(brush, 15 * img.Width / 20f + 0.5f, img.Height - bar16, img.Width / 20 - 1, bar16);
                graphics.FillRectangle(brush, 16 * img.Width / 20f + 0.5f, img.Height - bar17, img.Width / 20 - 1, bar17);
                graphics.FillRectangle(brush, 17 * img.Width / 20f + 0.5f, img.Height - bar18, img.Width / 20 - 1, bar18);
                graphics.FillRectangle(brush, 18 * img.Width / 20f + 0.5f, img.Height - bar19, img.Width / 20 - 1, bar19);
                graphics.FillRectangle(brush, 19 * img.Width / 20f + 0.5f, img.Height - bar20, img.Width / 20 - 1, bar20);
                SetWebcamInputs(bmp);
                graphics.Dispose();
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
            Process.GetCurrentProcess().Kill();
        }
        public void GetAudioByteArray()
        {
            capture = new CSCore.SoundIn.WasapiLoopbackCapture();
            capture.Initialize();
            IWaveSource source = new SoundInSource(capture);
            fftSize = FftSize.Fft4096;
            fftBuffer = new float[(int)fftSize];
            spectrumProvider = new BasicSpectrumProvider(capture.WaveFormat.Channels, capture.WaveFormat.SampleRate, fftSize);
            lineSpectrum = new LineSpectrum(fftSize)
            {
                SpectrumProvider = spectrumProvider,
                UseAverage = true,
                BarCount = numBars,
                BarSpacing = 2,
                IsXLogScale = false,
                ScalingStrategy = ScalingStrategy.Sqrt
            };
            var notificationSource = new SingleBlockNotificationStream(source.ToSampleSource());
            notificationSource.SingleBlockRead += NotificationSource_SingleBlockRead;
            finalSource = notificationSource.ToWaveSource();
            capture.DataAvailable += Capture_DataAvailable;
            capture.Start();
        }
        public void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            finalSource.Read(e.Data, e.Offset, e.ByteCount);
        }
        public void NotificationSource_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            spectrumProvider.Add(e.Left, e.Right);
        }
        public float[] GetFFtData()
        {
            lock (barData)
            {
                lineSpectrum.BarCount = numBars;
                if (numBars != barData.Length)
                {
                    barData = new float[numBars];
                }
            }
            if (spectrumProvider.IsNewDataAvailable)
            {
                lineSpectrum.MinimumFrequency = minFreq;
                lineSpectrum.MaximumFrequency = maxFreq;
                lineSpectrum.IsXLogScale = logScale;
                lineSpectrum.BarSpacing = barSpacing;
                lineSpectrum.SpectrumProvider.GetFftData(fftBuffer, this);
                return lineSpectrum.GetSpectrumPoints(100.0f, fftBuffer);
            }
            else
            {
                return null;
            }
        }
        public void ComputeData()
        {
            float[] resData = GetFFtData();
            int numBars = barData.Length;
            if (resData == null)
            {
                return;
            }
            lock (barData)
            {
                for (int i = 0; i < numBars && i < resData.Length; i++)
                {
                    barData[i] = resData[i] / 100.0f;
                }
                for (int i = 0; i < numBars && i < resData.Length; i++)
                {
                    if (lineSpectrum.UseAverage)
                    {
                        barData[i] = barData[i] + highScaleAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barData[i];
                    }
                    else
                    {
                        barData[i] = barData[i] + highScaleNotAverage * (float)Math.Sqrt(i / (numBars + 0.0f)) * barData[i];
                    }
                }
            }
        }
    }
}
namespace WinformsVisualization.Visualization
{
    /// <summary>
    ///     BasicSpectrumProvider
    /// </summary>
    public class BasicSpectrumProvider : FftProvider, ISpectrumProvider
    {
        public readonly int _sampleRate;
        public readonly List<object> _contexts = new List<object>();

        public BasicSpectrumProvider(int channels, int sampleRate, FftSize fftSize)
            : base(channels, fftSize)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException("sampleRate");
            _sampleRate = sampleRate;
        }

        public int GetFftBandIndex(float frequency)
        {
            int fftSize = (int)FftSize;
            double f = _sampleRate / 2.0;
            // ReSharper disable once PossibleLossOfFraction
            return (int)((frequency / f) * (fftSize / 2));
        }

        public bool GetFftData(float[] fftResultBuffer, object context)
        {
            if (_contexts.Contains(context))
                return false;

            _contexts.Add(context);
            GetFftData(fftResultBuffer);
            return true;
        }

        public override void Add(float[] samples, int count)
        {
            base.Add(samples, count);
            if (count > 0)
                _contexts.Clear();
        }

        public override void Add(float left, float right)
        {
            base.Add(left, right);
            _contexts.Clear();
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public interface ISpectrumProvider
    {
        bool GetFftData(float[] fftBuffer, object context);
        int GetFftBandIndex(float frequency);
    }
}
namespace WinformsVisualization.Visualization
{
    internal class GradientCalculator
    {
        public Color[] _colors;

        public GradientCalculator()
        {
        }

        public GradientCalculator(params Color[] colors)
        {
            _colors = colors;
        }

        public Color[] Colors
        {
            get { return _colors ?? (_colors = new Color[] { }); }
            set { _colors = value; }
        }

        public Color GetColor(float perc)
        {
            if (_colors.Length > 1)
            {
                int index = Convert.ToInt32((_colors.Length - 1) * perc - 0.5f);
                float upperIntensity = (perc % (1f / (_colors.Length - 1))) * (_colors.Length - 1);
                if (index + 1 >= Colors.Length)
                    index = Colors.Length - 2;

                return Color.FromArgb(
                    255,
                    (byte)(_colors[index + 1].R * upperIntensity + _colors[index].R * (1f - upperIntensity)),
                    (byte)(_colors[index + 1].G * upperIntensity + _colors[index].G * (1f - upperIntensity)),
                    (byte)(_colors[index + 1].B * upperIntensity + _colors[index].B * (1f - upperIntensity)));
            }
            return _colors.FirstOrDefault();
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public class LineSpectrum : SpectrumBase
    {
        public int _barCount;
        public double _barSpacing;
        public double _barWidth;
        public Size _currentSize;

        public LineSpectrum(FftSize fftSize)
        {
            FftSize = fftSize;
        }

        [Browsable(false)]
        public double BarWidth
        {
            get { return _barWidth; }
        }

        public double BarSpacing
        {
            get { return _barSpacing; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _barSpacing = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("BarSpacing");
                RaisePropertyChanged("BarWidth");
            }
        }

        public int BarCount
        {
            get { return _barCount; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");
                _barCount = value;
                SpectrumResolution = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("BarCount");
                RaisePropertyChanged("BarWidth");
            }
        }

        [BrowsableAttribute(false)]
        public Size CurrentSize
        {
            get { return _currentSize; }
            set
            {
                _currentSize = value;
                RaisePropertyChanged("CurrentSize");
            }
        }

        public Bitmap CreateSpectrumLine(Size size, Brush brush, Color background, bool highQuality)
        {
            if (!UpdateFrequencyMappingIfNessesary(size))
                return null;

            var fftBuffer = new float[(int)FftSize];

            //get the fft result from the spectrum provider
            if (SpectrumProvider.GetFftData(fftBuffer, this))
            {
                using (var pen = new Pen(brush, (float)_barWidth))
                {
                    var bitmap = new Bitmap(size.Width, size.Height);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        PrepareGraphics(graphics, highQuality);
                        graphics.Clear(background);

                        CreateSpectrumLineInternal(graphics, pen, fftBuffer, size);
                    }

                    return bitmap;
                }
            }
            return null;
        }

        public Bitmap CreateSpectrumLine(Size size, Color color1, Color color2, Color background, bool highQuality)
        {
            if (!UpdateFrequencyMappingIfNessesary(size))
                return null;

            using (
                Brush brush = new LinearGradientBrush(new RectangleF(0, 0, (float)_barWidth, size.Height), color2,
                    color1, LinearGradientMode.Vertical))
            {
                return CreateSpectrumLine(size, brush, background, highQuality);
            }
        }

        public void CreateSpectrumLineInternal(Graphics graphics, Pen pen, float[] fftBuffer, Size size)
        {
            int height = size.Height;
            //prepare the fft result for rendering 
            SpectrumPointData[] spectrumPoints = CalculateSpectrumPoints(height, fftBuffer);

            //connect the calculated points with lines
            for (int i = 0; i < spectrumPoints.Length; i++)
            {
                SpectrumPointData p = spectrumPoints[i];
                int barIndex = p.SpectrumPointIndex;
                double xCoord = BarSpacing * (barIndex + 1) + (_barWidth * barIndex) + _barWidth / 2;

                var p1 = new PointF((float)xCoord, height);
                var p2 = new PointF((float)xCoord, height - (float)p.Value - 1);

                graphics.DrawLine(pen, p1, p2);
            }
        }

        public override void UpdateFrequencyMapping()
        {
            _barWidth = Math.Max(((_currentSize.Width - (BarSpacing * (BarCount + 1))) / BarCount), 0.00001);
            base.UpdateFrequencyMapping();
        }

        public bool UpdateFrequencyMappingIfNessesary(Size newSize)
        {
            if (newSize != CurrentSize)
            {
                CurrentSize = newSize;
                UpdateFrequencyMapping();
            }

            return newSize.Width > 0 && newSize.Height > 0;
        }

        public void PrepareGraphics(Graphics graphics, bool highQuality)
        {
            if (highQuality)
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Default;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            }
            else
            {
                graphics.SmoothingMode = SmoothingMode.HighSpeed;
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.PixelOffsetMode = PixelOffsetMode.None;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            }
        }
        public float[] GetSpectrumPoints(float height, float[] fftBuffer)
        {
            SpectrumPointData[] dats = CalculateSpectrumPoints(height, fftBuffer);
            float[] res = new float[dats.Length];
            for (int i = 0; i < dats.Length; i++)
            {
                res[i] = (float)dats[i].Value;
            }

            return res;
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public class SpectrumBase : INotifyPropertyChanged
    {
        public const int ScaleFactorLinear = 9;
        public const int ScaleFactorSqr = 2;
        public const double MinDbValue = -90;
        public const double MaxDbValue = 0;
        public const double DbScale = (MaxDbValue - MinDbValue);

        public int _fftSize;
        public bool _isXLogScale;
        public int _maxFftIndex;
        public int _maximumFrequency = 20000;
        public int _maximumFrequencyIndex;
        public int _minimumFrequency = 20; //Default spectrum from 20Hz to 20kHz
        public int _minimumFrequencyIndex;
        public ScalingStrategy _scalingStrategy;
        public int[] _spectrumIndexMax;
        public int[] _spectrumLogScaleIndexMax;
        public ISpectrumProvider _spectrumProvider;

        public int SpectrumResolution;
        public bool _useAverage;

        public int MaximumFrequency
        {
            get { return _maximumFrequency; }
            set
            {
                if (value <= MinimumFrequency)
                {
                    throw new ArgumentOutOfRangeException("value",
                        "Value must not be less or equal the MinimumFrequency.");
                }
                _maximumFrequency = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("MaximumFrequency");
            }
        }

        public int MinimumFrequency
        {
            get { return _minimumFrequency; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                _minimumFrequency = value;
                UpdateFrequencyMapping();

                RaisePropertyChanged("MinimumFrequency");
            }
        }

        [BrowsableAttribute(false)]
        public ISpectrumProvider SpectrumProvider
        {
            get { return _spectrumProvider; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _spectrumProvider = value;

                RaisePropertyChanged("SpectrumProvider");
            }
        }

        public bool IsXLogScale
        {
            get { return _isXLogScale; }
            set
            {
                _isXLogScale = value;
                UpdateFrequencyMapping();
                RaisePropertyChanged("IsXLogScale");
            }
        }

        public ScalingStrategy ScalingStrategy
        {
            get { return _scalingStrategy; }
            set
            {
                _scalingStrategy = value;
                RaisePropertyChanged("ScalingStrategy");
            }
        }

        public bool UseAverage
        {
            get { return _useAverage; }
            set
            {
                _useAverage = value;
                RaisePropertyChanged("UseAverage");
            }
        }

        [BrowsableAttribute(false)]
        public FftSize FftSize
        {
            get { return (FftSize)_fftSize; }
            set
            {
                if ((int)Math.Log((int)value, 2) % 1 != 0)
                    throw new ArgumentOutOfRangeException("value");

                _fftSize = (int)value;
                _maxFftIndex = _fftSize / 2 - 1;

                RaisePropertyChanged("FFTSize");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void UpdateFrequencyMapping()
        {
            _maximumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MaximumFrequency) + 1, _maxFftIndex);
            _minimumFrequencyIndex = Math.Min(_spectrumProvider.GetFftBandIndex(MinimumFrequency), _maxFftIndex);

            int actualResolution = SpectrumResolution;

            int indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
            double linearIndexBucketSize = Math.Round(indexCount / (double)actualResolution, 3);

            _spectrumIndexMax = _spectrumIndexMax.CheckBuffer(actualResolution, true);
            _spectrumLogScaleIndexMax = _spectrumLogScaleIndexMax.CheckBuffer(actualResolution, true);

            double maxLog = Math.Log(actualResolution, actualResolution);
            for (int i = 1; i < actualResolution; i++)
            {
                int logIndex =
                    (int)((maxLog - Math.Log((actualResolution + 1) - i, (actualResolution + 1))) * indexCount) +
                    _minimumFrequencyIndex;

                _spectrumIndexMax[i - 1] = _minimumFrequencyIndex + (int)(i * linearIndexBucketSize);
                _spectrumLogScaleIndexMax[i - 1] = logIndex;
            }

            if (actualResolution > 0)
            {
                _spectrumIndexMax[_spectrumIndexMax.Length - 1] =
                    _spectrumLogScaleIndexMax[_spectrumLogScaleIndexMax.Length - 1] = _maximumFrequencyIndex;
            }
        }

        public virtual SpectrumPointData[] CalculateSpectrumPoints(double maxValue, float[] fftBuffer)
        {
            var dataPoints = new List<SpectrumPointData>();

            double value0 = 0, value = 0;
            double lastValue = 0;
            double actualMaxValue = maxValue;
            int spectrumPointIndex = 0;

            for (int i = _minimumFrequencyIndex; i <= _maximumFrequencyIndex; i++)
            {
                switch (ScalingStrategy)
                {
                    case ScalingStrategy.Decibel:
                        value0 = (((20 * Math.Log10(fftBuffer[i])) - MinDbValue) / DbScale) * actualMaxValue;
                        break;
                    case ScalingStrategy.Linear:
                        value0 = (fftBuffer[i] * ScaleFactorLinear) * actualMaxValue;
                        break;
                    case ScalingStrategy.Sqrt:
                        value0 = ((Math.Sqrt(fftBuffer[i])) * ScaleFactorSqr) * actualMaxValue;
                        break;
                }

                bool recalc = true;

                value = Math.Max(0, Math.Max(value0, value));

                while (spectrumPointIndex <= _spectrumIndexMax.Length - 1 &&
                       i ==
                       (IsXLogScale
                           ? _spectrumLogScaleIndexMax[spectrumPointIndex]
                           : _spectrumIndexMax[spectrumPointIndex]))
                {
                    if (!recalc)
                        value = lastValue;

                    if (value > maxValue)
                        value = maxValue;

                    if (_useAverage && spectrumPointIndex > 0)
                        value = (lastValue + value) / 2.0;

                    dataPoints.Add(new SpectrumPointData { SpectrumPointIndex = spectrumPointIndex, Value = value });

                    lastValue = value;
                    value = 0.0;
                    spectrumPointIndex++;
                    recalc = false;
                }

                //value = 0;
            }

            return dataPoints.ToArray();
        }

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !String.IsNullOrEmpty(propertyName))
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [DebuggerDisplay("{Value}")]
        public struct SpectrumPointData
        {
            public int SpectrumPointIndex;
            public double Value;
        }
    }
}
namespace WinformsVisualization.Visualization
{
    public enum ScalingStrategy
    {
        Decibel,
        Linear,
        Sqrt
    }
}