using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using AimBot.Helpers;
using AimBot.Renderers;

namespace AimBot.Detectors
{
    [Flags]
    public enum Selecting
    {
        Head = 1 << 0,
        Body = 1 << 1,
        Both = Head | Body
    }

    public enum Anchor
    {
        Center,
        TopCenter,
        BottomCenter
    }

    public enum Units
    {
        Pixels,
        Percentage
    }

    public readonly struct Detection
    {
        public readonly Rectangle BoundingBox;
        public readonly Point HeadPosition;
        public readonly Point BodyPosition;
        public readonly double Confidence; // In the interval [0.0, 1.0].

        public Detection(Rectangle boundingBox, Point headPosition, Point bodyPosition, double confidence)
        {
            BoundingBox = boundingBox;
            HeadPosition = headPosition;
            BodyPosition = bodyPosition;
            Confidence = confidence;
        }
    }

    public interface Detector : IDisposable
    {
        List<Detection> Detect(IntPtr image, Rectangle region, Esp esp);
    }

    public abstract class NeuralNetDetector
    {
        private readonly FileFilter configurationFile;
        private readonly List<Detection> detections;

        public FileFilter ConfigurationFile
        {
            get { return configurationFile; }
        }

        public Anchor AnchorPoint { get; set; } = Anchor.Center;
        public Units OffsetUnits { get; set; } = Units.Pixels;
        public int HeadHorizontalOffset { get; set; } = 0;
        public int HeadVerticalOffset { get; set; } = 0;
        public int BodyHorizontalOffset { get; set; } = 0;
        public int BodyVerticalOffset { get; set; } = 0;
        public double DetectionThreshold { get; set; } = 0.05;

        protected int InputWidth { get; private set; } = 512;
        protected int InputHeight { get; private set; } = 512;

        protected NeuralNetDetector()
        {
            configurationFile = new FileFilter("Detectors", "*.cfg", "");
            configurationFile.OnFileChanged += OnConfigurationFileChanged;

            detections = new List<Detection>();
        }

        private void OnConfigurationFileChanged(FileFilter obj)
        {
            var filePath = ConfigurationFile.FilePath;
            if (File.Exists(filePath))
            {
                using (var reader = File.OpenText(filePath))
                {
                    var line = reader.ReadLine();
                    while (line != null)
                    {
                        var parts = line.Split("=");
                        if (parts != null && parts.Length == 2)
                        {
                            switch (parts[0])
                            {
                                case "width":
                                    InputWidth = int.Parse(parts[1]);
                                    break;
                                case "height":
                                    InputHeight = int.Parse(parts[1]);
                                    break;
                            }
                        }

                        line = reader.ReadLine();
                    }
                }
            }
            
            Reload();
        }

        protected abstract bool Reload();

        protected abstract void AddDetections(IntPtr image, Rectangle region, Esp esp, List<Detection> detections);

        public List<Detection> Detect(IntPtr image, Rectangle region, Esp esp)
        {
            detections.Clear();
            AddDetections(image, region, esp, detections);
            return detections;
        }

        protected void AddDetection(Rectangle bounds, double confidence, Esp esp)
        {
            var center = bounds.Center();

            if (esp != null)
            {
                esp.Add(new RectangleShape(new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), Color.Transparent, Color.Red, 2));
            }

            int hho = 0;
            int hvo = 0;
            int bho = 0;
            int bvo = 0;

            switch (OffsetUnits)
            {
                case Units.Pixels:
                    hho = HeadHorizontalOffset;
                    hvo = HeadVerticalOffset;
                    bho = BodyHorizontalOffset;
                    bvo = BodyVerticalOffset;
                    break;
                case Units.Percentage:
                    hho = (int)Math.Round(bounds.Width  * HeadHorizontalOffset * 0.01);
                    hvo = (int)Math.Round(bounds.Height * HeadVerticalOffset   * 0.01);
                    bho = (int)Math.Round(bounds.Width  * BodyHorizontalOffset * 0.01);
                    bvo = (int)Math.Round(bounds.Height * BodyVerticalOffset   * 0.01);
                    break;
            }

            var head = Point.Empty;
            var body = Point.Empty;

            switch (AnchorPoint)
            {
                case Anchor.Center:
                    head = new Point(center.X + hho, center.Y + hvo);
                    body = new Point(center.X + bho, center.Y + bvo);
                    break;
                case Anchor.TopCenter:
                    head = new Point(center.X + hho, bounds.Top + hvo);
                    body = new Point(center.X + bho, bounds.Top + bvo);
                    break;
                case Anchor.BottomCenter:
                    head = new Point(center.X + hho, bounds.Bottom + hvo);
                    body = new Point(center.X + bho, bounds.Bottom + bvo);
                    break;
            }

            detections.Add(new Detection(bounds, head, body, confidence));

            if (esp != null)
            {
                esp.Add(new CircleShape(new Point(head.X, head.Y), 5, Color.Transparent, Color.Cyan, 1));

                if (head != body)
                {
                    esp.Add(new CircleShape(new Point(body.X, body.Y), 5, Color.Transparent, Color.Cyan, 1));
                }
            }
        }
    }
}
