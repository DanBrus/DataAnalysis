using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace MOAED_2_sem
{
    public partial class MOAED_2_sem : Form
    {
        private List<string> printedGraphs;
        private Core calc_core;

        Color[] _colors = new Color[] {
            Color.Black,
            Color.Blue,
            Color.Brown,
            Color.Gray,
            Color.Green,
            Color.Indigo,
            Color.Orange,
            Color.Red,
            Color.YellowGreen,
            Color.AliceBlue,
            Color.Bisque,
            Color.Chocolate
        };

        public MOAED_2_sem()
        {
            printedGraphs = new List<string>();
            calc_core = new Core();
            InitializeComponent();
        }

        private void updateGraph()
        {
            // Обновим график
            zedGraphControl.AxisChange();
            zedGraphControl.Invalidate();
        }

        private void graphChanged(string modelName)
        {
            Predicate<CurveItem> isAlreadyPrintedRed = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString() + "_red") return true; return false; };
            Predicate<CurveItem> isAlreadyPrintedGre = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString() + "_gregn") return true; return false; };
            Predicate<CurveItem> isAlreadyPrintedBlu = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString() + "_blue") return true; return false; };

            GraphPane pane = zedGraphControl.GraphPane;

            CurveItem redtem = pane.CurveList.Find(isAlreadyPrintedRed);
            CurveItem gretem = pane.CurveList.Find(isAlreadyPrintedGre);
            CurveItem blutem = pane.CurveList.Find(isAlreadyPrintedBlu);

            if (redtem == null || gretem == null || blutem == null)
                return;

            PointPairList redList = new PointPairList();
            PointPairList greList = new PointPairList();
            PointPairList bluList = new PointPairList();
            Dictionary<string, double> param = calc_core.get_params(modelName);
            double[] redTmp = calc_core.get_red(modelName);
            double[] greTmp = calc_core.get_green(modelName);
            double[] bluTmp = calc_core.get_blue(modelName);

            for (int i = 0; i < param["step_ctr"]; i++)
            {
                redList.Add(i * param["step"] + param["start"], redTmp[i]);
                greList.Add(i * param["step"] + param["start"], greTmp[i]);
                bluList.Add(i * param["step"] + param["start"], bluTmp[i]);
            }

            redtem.Points = redList;
            gretem.Points = greList;
            blutem.Points = bluList;

            updateGraph();
        }

        private void printGraphBtn_Click(object sender, EventArgs e)
        {
            Predicate<CurveItem> isAlreadyPrinted = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString()) return true; return false; };

            GraphPane pane = zedGraphControl.GraphPane;

            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }
            if (pane.CurveList.Find(isAlreadyPrinted) != null)
            {
                MessageBox.Show("Model is already printed.");
                return;
            }

            printedGraphs.Add(modelsLB.SelectedItem.ToString());

            string modelName = modelsLB.SelectedItem.ToString();


            PointPairList redList = new PointPairList();
            PointPairList greList = new PointPairList();
            PointPairList bluList = new PointPairList();
            Dictionary<string, double> param = calc_core.get_params(modelName);
            double[] redRes = calc_core.get_red(modelName);
            double[] greRes = calc_core.get_green(modelName);
            double[] bluRes = calc_core.get_blue(modelName);

            for (int i = 0; i < param["step_ctr"]; i++)
            {
                redList.Add(i * param["step"] + param["start"], redRes[i]);
                greList.Add(i * param["step"] + param["start"], greRes[i]);
                bluList.Add(i * param["step"] + param["start"], bluRes[i]);
            }

            // Выберем цвет для графика
            Color curveColor = _colors[pane.CurveList.Count % _colors.Length];
            LineItem myRedCurve = pane.AddCurve(modelName + "_red", redList, Color.Red, SymbolType.None); ;
            LineItem myGreCurve = pane.AddCurve(modelName + "_green", greList, Color.Green, SymbolType.None);
            LineItem myBluCurve = pane.AddCurve(modelName + "_blue", bluList, Color.Blue, SymbolType.None);

            updateGraph();

        }

        private void cleanGraphBtn_Click(object sender, EventArgs e)
        {
            Predicate<CurveItem> isAlreadyPrintedRed = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString() + "_red") return true; return false; };
            Predicate<CurveItem> isAlreadyPrintedGre = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString() + "_green") return true; return false; };
            Predicate<CurveItem> isAlreadyPrintedBlu = delegate (CurveItem obj) { if (obj.Label.Text == modelsLB.SelectedItem.ToString() + "_blue") return true; return false; };

            GraphPane pane = zedGraphControl.GraphPane;

            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }
            if (pane.CurveList.Find(isAlreadyPrintedRed) == null)
            {
                MessageBox.Show("Model is not printed.");
                return;
            }

            pane.CurveList.Remove(pane.CurveList.Find(isAlreadyPrintedRed));
            pane.CurveList.Remove(pane.CurveList.Find(isAlreadyPrintedGre));
            pane.CurveList.Remove(pane.CurveList.Find(isAlreadyPrintedBlu));
            updateGraph();
        }

        private void DatLoadBtn_Click(object sender, EventArgs e)
        {
            if (loadModelNameTB.Text == "")
            {
                MessageBox.Show("Please, enter model name");
                return;
            }

            openFileDialog1.Filter = "data files (*.dat) | *.dat";
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;


            BinaryReader reader = new BinaryReader(File.OpenRead(openFileDialog1.FileName));
            double[] data = new double[(reader.BaseStream.Length - 8) / 4];
            double step = reader.ReadDouble();
            for (int i = 0; i < (reader.BaseStream.Length - 8) / 4; i++)
            {
                data[i] = reader.ReadSingle();
            }

            reader.Close();

            calc_core.load_model(data, loadModelNameTB.Text, step);

            change_list();
        }

        void change_list()
        {
            int i = modelsLB.SelectedIndex;

            modelsLB.Items.Clear();
            foreach (string name in calc_core.get_names())
                modelsLB.Items.Add(name);

            modelsLB.SelectedIndex = (modelsLB.Items.Count - 1 < i) ? modelsLB.Items.Count - 1 : i;
        }

        private void modelDeleteBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
                return;

            cleanGraphBtn_Click(sender, e);

            calc_core.del_model(modelsLB.SelectedItem.ToString());
            modelsLB.Items.Remove(modelsLB.SelectedItem);
        }

        /*
        private List<string> findChannels(string channelName)
        {
            if (!channelName.EndsWith("Channel"))
            {
                MessageBox.Show("Choosen model is not a channel.");
                return null;
            }
            string tmp = channelName.Substring(0, modelsLB.SelectedItem.ToString().Length - 10);
            string redChan = tmp,
                grnChan = tmp + "GrnChannel",
                bluChan = tmp + "BluChannel";
            if(!modelsLB.Items.Contains(redChan) || !modelsLB.Items.Contains(grnChan) || !modelsLB.Items.Contains(bluChan))
            {
                MessageBox.Show("One or more image channels is not found");
                return null;
            }

            List<string> tmpList = new List<string>();
            tmpList.Add(redChan);
            tmpList.Add(grnChan);
            tmpList.Add(bluChan);
            return tmpList;
        }*/

        private void pivturePrintBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose a model.");
                return;
            }
            string tmp = modelsLB.SelectedItem.ToString();
            calc_core.normalizePicture(tmp);
            double[] redChan, grnChan, bluChan;
            try
            {
                redChan = calc_core.get_red(tmp);
                grnChan = calc_core.get_green(tmp);
                bluChan = calc_core.get_blue(tmp);
            }
            catch (Exception)
            {
                MessageBox.Show("One or more image channels is not found");
                return;
            }

            Bitmap bmp = new Bitmap(Convert.ToInt32(calc_core.get_params(tmp)["width"]),
                                    Convert.ToInt32(calc_core.get_params(tmp)["height"]));


            int h = Convert.ToInt32(calc_core.get_params(tmp)["height"]);
            int w = Convert.ToInt32(calc_core.get_params(tmp)["width"]);
            for (int i = 0; i < redChan.Length; i++)
            {
                Color clr = Color.FromArgb(Convert.ToInt32(redChan[i]), Convert.ToInt32(redChan[i]), Convert.ToInt32(redChan[i]));
                bmp.SetPixel(i / h, i % h, clr);
            }

            pictureBox.Image = bmp;
            pictureBox.Width = bmp.Width;
            pictureBox.Height = bmp.Height;
        }

        private void pictureCleanBtn_Click(object sender, EventArgs e)
        {
            pictureBox.Image = null;
        }

        private void jpegLoadBtn_Click(object sender, EventArgs e)
        {
            if (loadModelNameTB.Text == "")
            {
                MessageBox.Show("Please, enter model name");
                return;
            }

            openFileDialog1.Filter = "jpeg files (*.jpg) | *.jpg";
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;


            Image img = Image.FromFile(openFileDialog1.FileName);
            Bitmap b = new Bitmap(img);//note this has several overloads, including a path to an image

            double[] rChan = new double[b.Width * b.Height];
            double[] gChan = new double[b.Width * b.Height];
            double[] bChan = new double[b.Width * b.Height];

            for (int i = 0; i < b.Width; ++i)
            {
                for (int j = 0; j < b.Height; ++j)
                {
                    rChan[i * b.Height + j] = Convert.ToDouble(b.GetPixel(i, j).R);
                    gChan[i * b.Height + j] = Convert.ToDouble(b.GetPixel(i, j).G);
                    bChan[i * b.Height + j] = Convert.ToDouble(b.GetPixel(i, j).B);
                }
            }

            Dictionary<string, double> param = new Dictionary<string, double>();

            param.Add("width", b.Width);
            param.Add("height", b.Height);

            calc_core.load_picture(rChan, gChan, bChan, loadModelNameTB.Text, 1, param);

            change_list();
        }

        private void jpegSaveBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
                return;

            saveFileDialog1.Filter = "JPEG files (*.jpg) | *.jpg";

            string tmp = modelsLB.SelectedItem.ToString();
            calc_core.normalizePicture(tmp);
            double[] redChan, grnChan, bluChan;
            try
            {
                redChan = calc_core.get_red(tmp);
                grnChan = calc_core.get_green(tmp);
                bluChan = calc_core.get_blue(tmp);
            }
            catch (Exception)
            {
                MessageBox.Show("One or more image channels is not found");
                return;
            }

            saveFileDialog1.FileName = modelsLB.SelectedItem.ToString();
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;


            Bitmap bmp = new Bitmap(Convert.ToInt32(calc_core.get_params(tmp)["width"]),
                                    Convert.ToInt32(calc_core.get_params(tmp)["height"]));

            int h = Convert.ToInt32(calc_core.get_params(tmp)["height"]);
            int w = Convert.ToInt32(calc_core.get_params(tmp)["width"]);
            for (int i = 0; i < redChan.Length; i++)
            {
                Color clr = Color.FromArgb(Convert.ToInt32(redChan[i]), Convert.ToInt32(grnChan[i]), Convert.ToInt32(bluChan[i]));
                bmp.SetPixel(i / h, i % h, clr);
            }

            bmp.Save(saveFileDialog1.FileName);

            return;
        }

        private void shiftBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.sumNumToModel(modelsLB.SelectedItem.ToString(), Convert.ToDouble(shiftNUD.Value), simpleNewNameTB.Text);
            //if (ChannelCB.Checked)
            //    calc_core.normalizePicture(modelsLB.SelectedItem.ToString());
            graphChanged(modelsLB.SelectedItem.ToString());

            change_list();
        }

        private void multBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.multModelToNum(modelsLB.SelectedItem.ToString(), Convert.ToDouble(multNUD.Value), simpleNewNameTB.Text);
            if (ChannelCB.Checked)
                calc_core.normalizePicture(modelsLB.SelectedItem.ToString());
            graphChanged(modelsLB.SelectedItem.ToString());

            change_list();
            updateGraph();
        }

        private void inverseBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.inversePicture(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text);
            graphChanged(modelsLB.SelectedItem.ToString());

            change_list();
            updateGraph();
        }

        private void EditTypeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (EditTypeCB.SelectedIndex)
            {
                case 0:
                    shiftGB.BringToFront();
                    break;
                case 1:
                    intensChangePage.BringToFront();
                    break;
                case 2:
                    sizePage.BringToFront();
                    break;
                case 3:
                    addNoizeGB.BringToFront();
                    break;
                case 4:
                    filtrarionGB.BringToFront();
                    break;
                case 5:
                    convoluteGB.BringToFront();
                    break;
                case 6:
                    linesDelGB.BringToFront();
                    break;
                case 7:
                    GreyGB.BringToFront();
                    break;
                case 8:
                    filtrationFurieGB.BringToFront();
                    break;
                default:
                    break;
            }
        }

        private void intensTransformBtn_Click(object sender, EventArgs e)
        {
            if (IntensTransformPicCB.SelectedIndex == -1 || IntensTransformModelCB.SelectedIndex == -1)
                return;
            if (!TransformRedCB.Checked && !TransformBlueCB.Checked && !TransformGreenCB.Checked)
                return;

            calc_core.intensTransform(IntensTransformPicCB.SelectedItem.ToString(),
                IntensTransformModelCB.SelectedItem.ToString(),
                simpleNewNameTB.Text,
                TransformRedCB.Checked,
                TransformGreenCB.Checked,
                TransformBlueCB.Checked);

            change_list();
        }

        private void IntensTransformPicCB_Click(object sender, EventArgs e)
        {
            IntensTransformPicCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            IntensTransformPicCB.Items.AddRange(tmp);
        }

        private void IntensTransformModelCB_Click(object sender, EventArgs e)
        {
            IntensTransformModelCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            IntensTransformModelCB.Items.AddRange(tmp);
        }

        private void resizeNeightborBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.resizeImage(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToDouble(resizeScaleXNUD.Value), Convert.ToDouble(resizeScaleYNUD.Value), false);

            change_list();
        }

        private void ResizeBilinearBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.resizeImage(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToDouble(resizeScaleXNUD.Value), Convert.ToDouble(resizeScaleYNUD.Value), true);

            change_list();
        }

        private void hystoMakeBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.makeHysto(modelsLB.SelectedItem.ToString());

            change_list();
        }

        private void distribMakeBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.makeDistrib(modelsLB.SelectedItem.ToString());

            change_list();
        }

        private void noizeSnPBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.noizeSnP(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToDouble(noizeIntenseNUD.Value));

            change_list();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.noizeRnd(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToDouble(noizeIntenseNUD.Value));

            change_list();
        }

        private void rotateBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.rotateImage(modelsLB.SelectedItem.ToString());

        }

        private void analPagesCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (analPagesCB.SelectedIndex)
            {
                case 0:
                    hystoGB.BringToFront();
                    break;
                case 1:
                    subtractGB.BringToFront();
                    break;
                case 2:
                    furieGB.BringToFront();
                    break;
                default:
                    break;
            }
        }

        private void subtractCB_Click(object sender, EventArgs e)
        {
            subtractCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            subtractCB.Items.AddRange(tmp);
        }

        private void subFromCB_Click(object sender, EventArgs e)
        {

            subFromCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            subFromCB.Items.AddRange(tmp);
        }

        private void subtractImagesBtn_Click(object sender, EventArgs e)
        {
            if (subFromCB.SelectedIndex == -1 || subtractCB.SelectedIndex == -1)
                return;

            calc_core.substractImages(subFromCB.SelectedItem.ToString(),
                subtractCB.SelectedItem.ToString(),
                analNewNameTB.Text);

            change_list();
        }

        private void filterMedBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.medianFilterImage(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToInt32(filterWindowNUD.Value));

            change_list();
        }

        private void filterAverBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.averageFilterImage(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToInt32(filterWindowNUD.Value));

            change_list();

        }

        private void greyLvlBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.greyScale(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToInt32(greyLvlsNUD.Value));

            change_list();

        }

        private void DatSaveBtn_Click_1(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
                return;

            saveFileDialog1.Filter = "data files (*.dat) | *.dat";
            saveFileDialog1.FileName = modelsLB.SelectedItem.ToString() + "_dt" + calc_core.get_params(modelsLB.SelectedItem.ToString())["step"];
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            BinaryWriter writer = new BinaryWriter(File.OpenWrite(saveFileDialog1.FileName));

            double[] res = calc_core.get_red(modelsLB.SelectedItem.ToString());

            writer.Write(calc_core.get_params(modelsLB.SelectedItem.ToString())["step"]);

            for (int i = 0; i < res.Length; i++)
                writer.Write(Convert.ToSingle(res[i]));

            writer.Close();
        }

        private void xcrLoadBtn_Click(object sender, EventArgs e)
        {
            if (loadModelNameTB.Text == "")
            {
                MessageBox.Show("Please, enter model name");
                return;
            }

            openFileDialog1.Filter = "XCR files (*.xcr) | *.xcr";
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            BinaryReader reader = new BinaryReader(File.OpenRead(openFileDialog1.FileName));
            double heigth = Convert.ToDouble(xcrHeighNUD.Value);
            double[] data = new double[(reader.BaseStream.Length - 0x800) / 2 - 4 * Convert.ToInt32(heigth)];
            double width = data.Length / heigth;
            double step = 1.0;

            byte[] header = reader.ReadBytes(0x800);

            for (int i = 0; i < data.Length; i++)
            {
                byte[] bytes = reader.ReadBytes(2);
                Array.Reverse(bytes);
                data[i] = BitConverter.ToUInt16(bytes, 0);
            }

            reader.Close();

            Dictionary<string, double> param = new Dictionary<string, double>();
            param.Add("height", heigth);
            param.Add("width", width);

            calc_core.load_picture(data, data, data, loadModelNameTB.Text, step, param);

            change_list();
        }

        private void furieTransBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.furieTransform(modelsLB.SelectedItem.ToString(), analNewNameTB.Text);

            change_list();
        }

        private void deleteLinesBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.linesDelete(modelsLB.SelectedItem.ToString(), analNewNameTB.Text, Convert.ToInt32(linesDeleteStepNUD.Value));

            change_list();

        }

        private void backFurieTransBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.furieTransform(modelsLB.SelectedItem.ToString(), analNewNameTB.Text, true);

            change_list();
        }

        private void convMaskCB_Click(object sender, EventArgs e)
        {
            convMaskCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            convMaskCB.Items.AddRange(tmp);
        }

        private void convModelCB_Click(object sender, EventArgs e)
        {
            convModelCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            convModelCB.Items.AddRange(tmp);

        }

        private void convBtn_Click(object sender, EventArgs e)
        {
            if (convModelCB.SelectedIndex == -1 || convMaskCB.SelectedIndex == -1)
                return;

            calc_core.convolute(convModelCB.SelectedItem.ToString(),
                convMaskCB.SelectedItem.ToString(),
                simpleNewNameTB.Text);

            change_list();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.resizeImageRediscret(modelsLB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToDouble(resizeScaleXNUD.Value), Convert.ToDouble(resizeScaleYNUD.Value));

            change_list();
        }

        private void filtrationBtn_Click(object sender, EventArgs e)
        {
            if (modelsLB.SelectedIndex == -1)
            {
                MessageBox.Show("Please, choose model.");
                return;
            }

            calc_core.filtreFurie(filtrationPictureCB.SelectedItem.ToString(), filtrationModelCB.SelectedItem.ToString(), simpleNewNameTB.Text, Convert.ToDouble(filtrationAlphaNUD.Value));

            change_list();

        }

        private void filtrationPictureCB_Click(object sender, EventArgs e)
        {
            filtrationPictureCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            filtrationPictureCB.Items.AddRange(tmp);
        }

        private void filtrationModelCB_Click(object sender, EventArgs e)
        {
            filtrationModelCB.Items.Clear();
            string[] tmp = new string[modelsLB.Items.Count];
            modelsLB.Items.CopyTo(tmp, 0);

            filtrationModelCB.Items.AddRange(tmp);
        }
    }
}
