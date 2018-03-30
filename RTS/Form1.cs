using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Graph;
using StochasticValues;

namespace RTS
{
    public partial class frmMain : Form
    {
        TSModel model = new TSModel(Application.StartupPath + "\\graphpzd.accdb");
        
        public frmMain()
        {
            InitializeComponent();
            // Перевод ординат в координаты экрана ("переворачивание картинки вверх ногами")
            double maxY = double.NegativeInfinity;
            foreach (Node node in model.Nodes) if (node.Y > maxY) maxY = node.Y;
            foreach (Node node in model.Nodes) node.Y = maxY - node.Y;

        }

        private void DrawGraph()
        {

            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;

            // font size definition
            float fs = (pnlGraph.Width < pnlGraph.Height) ? 0.02f * pnlGraph.Width : 0.02f * pnlGraph.Height;
            if (fs > 12) fs = 12f;

            foreach (Node node in model.Nodes)
            {
                if (node.X > maxX) maxX = node.X;
                if (node.Y > maxY) maxY = node.Y;
                if (node.X < minX) minX = node.X;
                if (node.Y < minY) minY = node.Y;
            }

            double scaleX = (pnlGraph.Width - 2 * fs - 100) / (maxX - minX);
            double scaleY = (pnlGraph.Height - 2 * fs - 3) / (maxY - minY);

            double maxLoad = double.NegativeInfinity;
            foreach (Link link in model.Links) if (link.Load > maxLoad) maxLoad = link.Load;
            
            Graphics g = pnlGraph.CreateGraphics();
                        
            foreach (Link link in model.Links)
            {
                double backLoad = model.GetLink(link.InNode, link.OutNode).Load;
                double load = (link.Load > backLoad) ? link.Load : backLoad;
                g.DrawLine(new Pen(Color.FromArgb((int)(255 * load / maxLoad),
                                                    (int)(255 - 255 * load / maxLoad), 100),
                                                    (float)(1 + 1.5 * fs * load / maxLoad)),
                            (float)(fs + scaleX * (link.OutNode.X - minX)),
                            (float)(fs + scaleY * (link.OutNode.Y - minY)),
                            (float)(fs + scaleX * (link.InNode.X - minX)),
                            (float)(fs + scaleY * (link.InNode.Y - minY)));
            }

            int dx, dy; // text shift in a rectangle
            foreach (Node node in model.Nodes)
            {
                g.FillRectangle(Brushes.FloralWhite,
                                new Rectangle((int)(scaleX * (node.X - minX)),
                                                (int)(scaleY * (node.Y - minY)),
                                                (int)(fs * 2.2), (int)fs * 2));

                g.DrawRectangle(new Pen(Color.Black, 1),
                                new Rectangle((int)(scaleX * (node.X - minX)),
                                                (int)(scaleY * (node.Y - minY)),
                                                (int)(fs * 2.2), (int)fs * 2));

                if (node.Code > 9) { dx = 0; dy = (int)(fs / 4); } else { dx = (int)(fs / 2); dy = (int)(fs / 4); }
                g.DrawString(node.Code.ToString(), new Font(new FontFamily("Courier New"), fs), Brushes.Black,
                             new RectangleF((int)(dx + scaleX * (node.X - minX)),
                                            (int)(dy + scaleY * (node.Y - minY)),
                                            (int)(fs * 2.2), (int)fs * 2));
                
                if (this.chbShowNames.Checked)
                    if (this.chbFacilities.Checked)
                        g.DrawString(node.Name + "(" + model.LocoNum[node].ToString() + ", " + model.GearNum[node].ToString() + ")",
                                        new Font(new FontFamily("Courier New"), 0.8f * fs),
                                        Brushes.DarkBlue,
                                        (int)(scaleX * (node.X - minX)) + (int)(fs * 2.2 + 2),
                                        (int)(scaleY * (node.Y - minY)));
                    else
                        g.DrawString(node.Name,
                                        new Font(new FontFamily("Courier New"), 0.8f * fs),
                                        Brushes.DarkBlue,
                                        (int)(scaleX * (node.X - minX)) + (int)(fs * 2.2 + 2),
                                        (int)(scaleY * (node.Y - minY)));
                else
                    if (this.chbFacilities.Checked)
                        g.DrawString("(" + model.LocoNum[node].ToString() + ", " + model.GearNum[node].ToString() + ")",
                                        new Font(new FontFamily("Courier New"), 0.8f * fs),
                                        Brushes.DarkBlue,
                                        (int)(scaleX * (node.X - minX)) + (int)(fs * 2.2 + 2),
                                        (int)(scaleY * (node.Y - minY)));
            }

            g.Dispose();
        }

        private void pnlGraph_Paint(object sender, PaintEventArgs e)
        {
            DrawGraph();
        }

        private void pnlGraph_Resize(object sender, EventArgs e)
        {
            pnlGraph.Refresh();
        }

        private void btnSimulate_Click(object sender, EventArgs e)
        {
            //model.SimulateTS();
            model.CalcExper();
            pnlGraph.Refresh();
        }

        private void chbShowNames_CheckedChanged(object sender, EventArgs e)
        {
            pnlGraph.Refresh();
        }

        private void chbFacilities_CheckedChanged(object sender, EventArgs e)
        {
            pnlGraph.Refresh();
        }
              
    }
}
