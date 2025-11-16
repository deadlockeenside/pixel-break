using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelBreak
{
    public partial class Form1 : Form
    {
        private List<Bitmap> _bitmaps = new List<Bitmap>();
        private Random _random = new Random();

        public Form1()
        {
            InitializeComponent();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) 
            {
                var timer = Stopwatch.StartNew();
                menuStrip1.Enabled = trackBar1.Enabled = false;
                pictureBox1.Image = null;
                _bitmaps.Clear();

                var bitmap = new Bitmap(openFileDialog1.FileName);

                await Task.Run(() => 
                {
                    RunProcessing(bitmap);
                });

                trackBar1.Value = trackBar1.Maximum;
                pictureBox1.Image = _bitmaps[trackBar1.Value - 1];

                menuStrip1.Enabled = trackBar1.Enabled = true;
                timer.Stop();
                Text = $"Заняло времени: {timer.Elapsed.ToString()}";
            }
        }

        // Последняя картинка не обрабатывается, так как если 100% пикселей, то ничего расщиплять не нужно
        // Поэтому она просто добавляется в конце и отсчет от 1, а не 0, чтобы было 99 итераций, а не 100
        // Все варианты состояния изображения задаются заранее
        // Чтобы потом быстро между этими состояниями переключаться, двигая ползунок
        private void RunProcessing(Bitmap bitmap) 
        {
            var pixels = GetPixels(bitmap);

            // Сколько пикселей в 1%
            var pixelsInStep = (bitmap.Width * bitmap.Height) / 100;

            // Нкжно запоминать предыдущее состояние, чтобы расщипление было плавным
            var currentPixelsSet = new List<Pixel>(pixels.Count - pixelsInStep);

            for (int i = 1; i < trackBar1.Maximum; i++) 
            { 
                for (int j = 0; j < pixelsInStep; j++) 
                {
                    var index = _random.Next(pixels.Count);
                    currentPixelsSet.Add(pixels[index]);
                    pixels.RemoveAt(index);
                }

                var currentBitmap = new Bitmap(bitmap.Width, bitmap.Height);

                foreach (var pixel in currentPixelsSet)
                    currentBitmap.SetPixel(pixel.Point.X, pixel.Point.Y, pixel.Color);

                _bitmaps.Add(currentBitmap);

                // Модификация UI из другого потока
                Invoke(new Action(() => 
                {
                    Text = $"Обработка... {i}%";
                }));
            }

            _bitmaps.Add(bitmap);
        }

        private List<Pixel> GetPixels(Bitmap bitmap) 
        { 
            var pixels = new List<Pixel>(bitmap.Width * bitmap.Height);

            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    pixels.Add(new Pixel 
                    { 
                        Color = bitmap.GetPixel(x, y),
                        Point = new Point 
                        {
                            X = x,
                            Y = y
                        }
                    });

            return pixels;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_bitmaps is null || _bitmaps.Count == 0)
                return;

            pictureBox1.Image = _bitmaps[trackBar1.Value - 1];
            Text = $"Состояние: {trackBar1.Value}%";
        }
    }
}
