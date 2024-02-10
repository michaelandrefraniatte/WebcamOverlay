using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

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
        private static int height = 300, width;
        private static bool getstate = false;
        private static double ratio, border;
        private static string base64image;
        public WebView2 webView21 = new WebView2();
        private ImageCodecInfo jpegEncoder;
        private EncoderParameters encoderParameters;
        private async void Form1_Load(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-web-security --allow-file-access-from-files --allow-file-access", "en");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment);
            webView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", "assets", CoreWebView2HostResourceAccessKind.DenyCors);
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView21.Source = new Uri("https://appassets/index.html");
            webView21.Dock = DockStyle.Fill;
            webView21.DefaultBackgroundColor = Color.Transparent;
            webView21.KeyDown += WebView21_KeyDown;
            this.Controls.Add(webView21);
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e.KeyData);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            OnKeyDown(keyData);
            return true;
        }
        private void WebView21_KeyDown(object sender, KeyEventArgs e)
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
        private void Form1_Shown(object sender, EventArgs e)
        {
            jpegEncoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, 255);
            AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(AppDomain_UnhandledException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            this.TopMost = true;
            this.Opacity = 0.80f;
            CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            FinalFrame = new VideoCaptureDevice(CaptureDevice[0].MonikerString);
            videoCapabilities = FinalFrame.VideoCapabilities;
            FinalFrame.VideoResolution = videoCapabilities[videoCapabilities.Length - 1];
            ratio = Convert.ToDouble(FinalFrame.VideoResolution.FrameSize.Width) / Convert.ToDouble(FinalFrame.VideoResolution.FrameSize.Height);
            height = 300;
            width = (int)(height * ratio);
            this.Size = new Size(width, height);
            this.ClientSize = new Size(width, height);
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width - width - 10, 10);
            border = 1.00f;
            FinalFrame.NewFrame += FinalFrame_NewFrame;
            FinalFrame.Start();
        }
        public static Bitmap ImageToGrayScale(Bitmap Bmp)
        {
            Bitmap newBitmap = new Bitmap(Bmp.Width, Bmp.Height);
            Graphics g = Graphics.FromImage(newBitmap);
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(Bmp, new Rectangle(0, 0, Bmp.Width, Bmp.Height), 0, 0, Bmp.Width, Bmp.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }
        public byte[] ImageToByteArray(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, jpegEncoder, encoderParameters);
                return ms.ToArray();
            }
        }
        private async Task<String> execScriptHelper(String script)
        {
            var x = await webView21.ExecuteScriptAsync(script).ConfigureAwait(false);
            return x;
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
        }
        private async void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                Bitmap bmp = new Bitmap(img);
                bmp = new Bitmap(bmp, new Size(bmp.Width / 2, bmp.Height / 2));
                bmp = ImageToGrayScale(bmp);
                byte[] imageArray = ImageToByteArray(bmp);
                base64image = Convert.ToBase64String(imageArray);
                await execScriptHelper($"setBackground('{base64image.ToString()}');");
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