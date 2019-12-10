using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using SimilarImageSearch.Engine;
using SimilarImageSearch.Engine.Arcs;
using AForge;

namespace SimilarImageSearch.Test
{
    public partial class ArcsView : Form
    {
        ArcCollection Arcs;
        readonly System.Drawing.SizeF Shift = new System.Drawing.SizeF(-5, -5);
        public bool ShowLabels { get; set; }
        public ArcsView(ArcCollection arcs, System.Drawing.Image background)
        {
            this.Arcs = arcs;
            InitializeComponent();
            //panel1.BackgroundImage = background;
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            System.Drawing.Graphics g = panel1.CreateGraphics();
            Random rand = new Random();
            int i = 0;
            foreach (Arc a in Arcs)
            {
                try
                {
                    System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255 * i / Arcs.Count, 255 * (Arcs.Count - i) / Arcs.Count, 0));
                    //Pen pen = new Pen(Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255)));
                    //Pen pen = new Pen(Color.Black);

                    Point center;
                    if (a.IsStrightLine)
                    {
                        g.DrawLine(pen, a.StartPoint.X, a.StartPoint.Y, a.StartPoint.X + (int)(a.ArcLength * Math.Cos(a.TangentAngle)), a.StartPoint.Y + (int)(a.ArcLength * Math.Sin(a.TangentAngle)));
                        center = GeometryTools.CenterPoint(a.StartPoint, a.EndPoint);
                    }
                    else
                    {
                        g.DrawArc(pen, new System.Drawing.RectangleF((float)(a.CenterPoint.X - a.Radius), (float)(a.CenterPoint.Y - a.Radius), (float)(a.Radius * 2), (float)(a.Radius * 2)),
                        (float)((a.TangentAngle - Math.PI / 2) * 180 / Math.PI),
                        (float)(a.Angle * 180 / Math.PI));
                        center = new Point(a.CenterPoint.X + (float)Math.Cos(a.StartAngle + a.Angle / 2) * a.Radius,
                            a.CenterPoint.Y + (float)Math.Sin(a.StartAngle + a.Angle / 2) * a.Radius);
                    }
                    if (ShowLabels)
                        g.DrawString(i.ToString(), this.Font, System.Drawing.Brushes.Black, System.Drawing.PointF.Add(new System.Drawing.PointF(center.X, center.Y), Shift));
                }
                catch { throw; }
                i++;
            }
        }
    }
}
