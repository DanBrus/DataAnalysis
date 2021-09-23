using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;
using MOAED_Cch;

namespace MOAED_Cch
{
    public partial class Form1 : Form
    {
        Core calc_core;
        int active_graph;
        public Form1()
        {
            active_graph = 0;
            calc_core = new Core();
            InitializeComponent();
        }

        Color[] _colors = new Color[] {Color.Black,
            Color.Blue,
            Color.Brown,
            Color.Gray,
            Color.Green,
            Color.Indigo,
            Color.Orange,
            Color.Red,
            Color.YellowGreen};

        private void PrintButton_Click(object sender, EventArgs e)
        {
            GraphPane pane = zedGraphControl1.GraphPane;
            
            switch (active_graph)
            {
                case 0:
                    pane = zedGraphControl1.GraphPane;
                    break;
                case 1:
                    pane = zedGraphControl2.GraphPane;
                    break;
                case 2:
                    pane = zedGraphControl3.GraphPane;
                    break;
                case 3:
                    pane = zedGraphControl4.GraphPane;
                    break;
            }

            Random rnd = new Random();

            PointPairList list = new PointPairList();
            if (modelsList.SelectedIndex == -1)
                return;

            Dictionary<string, double> param = calc_core.get_params(modelsList.SelectedItem.ToString());
            double[] result = calc_core.get_result(modelsList.SelectedItem.ToString());

            for(int i = 0; i < param["step_ctr"]; i++)
            {
                list.Add(i * param["step"] + param["start"], result[i]);
            }

            // Выберем случайный цвет для графика
            Color curveColor = _colors[rnd.Next(_colors.Length)];
            LineItem myCurve = pane.AddCurve(modelsList.SelectedItem.ToString(), list, curveColor, SymbolType.None);

            // Включим сглаживание
            //myCurve.Line.IsSmooth = true;

            // Обновим график
            switch (active_graph)
            {
                case 0:
                    zedGraphControl1.AxisChange();
                    zedGraphControl1.Invalidate();
                    break;
                case 1:
                    zedGraphControl2.AxisChange();
                    zedGraphControl2.Invalidate();
                    break;
                case 2:
                    zedGraphControl3.AxisChange();
                    zedGraphControl3.Invalidate();
                    break;
                case 3:
                    zedGraphControl4.AxisChange();
                    zedGraphControl4.Invalidate();
                    break;
            }
        }

        private void ModelTypeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ModelTypeCB.SelectedIndex)
            {
                case 0:
                    LinGB.BringToFront();
                    break;

                case 1:
                    ExpGB.BringToFront();
                    break;

                case 2:
                    randGB.BringToFront();
                    break;

                case 3:
                    HarmGB.BringToFront();
                    break;

                case 4:
                    pulseGB.BringToFront();
                    break;

                case 5:
                    FilterGB.BringToFront();
                    break;

                default:
                    break;
            }

            return;
        }

        private void NewModelButton_Click(object sender, EventArgs e)
        {

            Dictionary<string, double> param = new Dictionary<string, double>();

            switch (ModelTypeCB.SelectedIndex)
            {
                case 0:
                    param.Add("type", 0);
                    param.Add("c", Convert.ToDouble(LinearC.Value));
                    param.Add("d", Convert.ToDouble(LinearD.Value));
                    param.Add("start", Convert.ToDouble(LinearStart.Value));
                    param.Add("step", Convert.ToDouble(LinearStep.Value));
                    param.Add("step_ctr", Convert.ToDouble(LinearStepCtr.Value));
                    modelsList.Items.Add(calc_core.new_model(param, "linear"));
                    break;

                case 1:
                    param.Add("type", 1);
                    param.Add("a", Convert.ToDouble(ExpA.Value));
                    param.Add("b", Convert.ToDouble(ExpB.Value));
                    param.Add("start", Convert.ToDouble(ExpStart.Value));
                    param.Add("step", Convert.ToDouble(ExpStep.Value));
                    param.Add("step_ctr", Convert.ToDouble(ExpStepCtr.Value));
                    modelsList.Items.Add(calc_core.new_model(param, "exponential"));
                    break;

                case 2:
                    param.Add("type", 2);
                    param.Add("start", Convert.ToDouble(RandStart.Value));
                    param.Add("step", Convert.ToDouble(RandStep.Value));
                    param.Add("min", Convert.ToDouble(randMin.Value));
                    param.Add("max", Convert.ToDouble(randMax.Value));
                    param.Add("step_ctr", Convert.ToDouble(randSteps.Value));
                    param.Add("rand_type", Convert.ToDouble(RandTypeCB.SelectedIndex));
                    modelsList.Items.Add(calc_core.new_model(param, "random"));
                    break;

                case 3:
                    param.Add("type", 5);
                    param.Add("start", Convert.ToDouble(HarmStart.Value));
                    param.Add("step", Convert.ToDouble(HarmStep.Value));
                    param.Add("step_ctr", Convert.ToDouble(HarmSteps.Value));
                    param.Add("phase", Convert.ToDouble(HarmPhase.Value));
                    param.Add("A", Convert.ToDouble(HarmAmp.Value));
                    param.Add("f", Convert.ToDouble(HarmFreq.Value));
                    modelsList.Items.Add(calc_core.new_model(param, "Harmonical"));
                    break;

                case 4:
                    param.Add("type", 7);
                    param.Add("start", 0.0);
                    param.Add("step", Convert.ToDouble(pulseStepNUD.Value));
                    param.Add("step_ctr", Convert.ToDouble(pulseStepsNUD.Value));
                    param.Add("T", Convert.ToDouble(pulseTNUD.Value));
                    param.Add("len", Convert.ToDouble(pulseLenNUD.Value));
                    param.Add("max", Convert.ToDouble(pulseMaxNUD.Value));
                    param.Add("min", Convert.ToDouble(pulseMinNUD.Value));
                    modelsList.Items.Add(calc_core.new_model(param, "Pulse"));
                    break;

                case 5:
                    param.Add("type", 8);
                    param.Add("start", 0.0);
                    param.Add("step", Convert.ToDouble(filterStepNUD.Value));
                    param.Add("step_ctr", Convert.ToDouble(filterStepsNUD.Value));
                    param.Add("filterType", Convert.ToDouble(filterTypeGB.SelectedIndex));
                    param.Add("S", Convert.ToDouble(filterSNUD.Value));
                    param.Add("f", Convert.ToDouble(filterFreqNUD.Value));
                    modelsList.Items.Add(calc_core.new_model(param, "Filter" + filterTypeGB.SelectedIndex));
                    break;
            }
        }

        private void ExpA_ValueChanged(object sender, EventArgs e)
        {

        }

        private void DelModelButton_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.del_model(modelsList.SelectedItem.ToString());
            modelsList.Items.Remove(modelsList.SelectedItem);
        }

        private void PlaneRB1_CheckedChanged(object sender, EventArgs e)
        {
            if (PlaneRB1.Checked)
                active_graph = 0;
        }

        private void PlaneRB2_CheckedChanged(object sender, EventArgs e)
        {
            if (PlaneRB2.Checked)
                active_graph = 1;

        }

        private void PlaneRB3_CheckedChanged(object sender, EventArgs e)
        {
            if (PlaneRB3.Checked)
                active_graph = 2;
        }

        private void PlaneRB4_CheckedChanged(object sender, EventArgs e)
        {
            if (PlaneRB4.Checked)
                active_graph = 3;
        }

        void change_list()
        {
            modelsList.Items.Clear();
            foreach (string name in calc_core.get_names())
                modelsList.Items.Add(name);
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            switch (active_graph)
            {
                case 0:
                    zedGraphControl1.GraphPane.CurveList.Clear();
                    zedGraphControl1.AxisChange();
                    zedGraphControl1.Invalidate();
                    break;
                case 1:
                    zedGraphControl2.GraphPane.CurveList.Clear();
                    zedGraphControl2.AxisChange();
                    zedGraphControl2.Invalidate();
                    break;
                case 2:
                    zedGraphControl3.GraphPane.CurveList.Clear();
                    zedGraphControl3.AxisChange();
                    zedGraphControl3.Invalidate();
                    break;
                case 3:
                    zedGraphControl4.GraphPane.CurveList.Clear();
                    zedGraphControl4.AxisChange();
                    zedGraphControl4.Invalidate();
                    break;
            }
        }

        private void station_btn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            Dictionary<string, double> res = calc_core.statioanarity(modelsList.SelectedItem.ToString());

            string result;
            if ((res["d_average"] > Convert.ToDouble(StatNUD.Value)) || (res["d_D"] > Convert.ToDouble(StatNUD.Value)))
                result = "Not stationary";
            else
                result = "Stationary";
            
            state_label.Text = result + "\n Average value scatter = " + res["d_average"] + " %\n Dispersy value scatter = " + res["d_D"] + "%";
        }

        private void Distribution_Btn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            modelsList.Items.Add(calc_core.distrb(modelsList.SelectedItem.ToString(), Convert.ToInt32(DistribNUD.Value)));
        }

        private void AnalActCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (AnalActCB.SelectedIndex)
            {
                case 0:
                    StatGB.BringToFront();
                    break;

                case 1:
                    DistribGB.BringToFront();
                    break;

                case 2:
                    AutocorGP.BringToFront();
                    break;

                case 3:
                    InterCorrGB.BringToFront();
                    break;

                case 4:
                    FurieGB.BringToFront();
                    break;

                case 5:
                    SNRGB.BringToFront();
                    break;

                case 6:
                    ampModulationGB.BringToFront();
                    break;

                case 7:
                    geneticalGB.BringToFront();
                    break;

                default:
                    break;
            }
        }

        private void AutocorBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            modelsList.Items.Add(calc_core.autocorr(modelsList.SelectedItem.ToString()));
        }

        private void HarmT_ValueChanged(object sender, EventArgs e)
        {
            decimal tmp = 1 / HarmT.Value;

            if (HarmFreq.Value != tmp)
                HarmFreq.Value = tmp;

            return;
        }

        private void HarmFreq_ValueChanged(object sender, EventArgs e)
        {
            decimal tmp = 1 / HarmFreq.Value;

            if (HarmT.Value != tmp)
                HarmT.Value = tmp;

            return;
        }

        private void FirstCB_Click(object sender, EventArgs e)
        {
            FirstCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            FirstCB.Items.AddRange(tmp);

            return;
        }

        private void SumBtn_Click(object sender, EventArgs e)
        {
            if (!modelsList.Items.Contains(FirstCB.SelectedItem) || !modelsList.Items.Contains(SecondCB.SelectedItem))
                return;

            calc_core.sum_models(FirstCB.SelectedItem.ToString(), SecondCB.SelectedItem.ToString(), EditName.Text);

            change_list();
            return;
        }

        private void SecondCB_Click(object sender, EventArgs e)
        {
            SecondCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            SecondCB.Items.AddRange(tmp);

            return;
        }

        private void FirstCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((FirstCB.SelectedItem == null) || (SecondCB.SelectedItem == null))
                return;

            EditName.Text = FirstCB.SelectedItem.ToString() + "UNITED" + SecondCB.SelectedItem.ToString();
            return;
        }

        private void SecondCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((FirstCB.SelectedItem == null) || (SecondCB.SelectedItem == null))
                return;

            EditName.Text = FirstCB.SelectedItem.ToString() + "UNITED" + SecondCB.SelectedItem.ToString();
            return;
        }

        private void EditTupeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (EditTypeCB.SelectedIndex)
            {
                case 0:
                    UnionGB.BringToFront();
                    break;
                case 1:
                    LinearisationGB.BringToFront();
                    break;
                case 2:
                    SmoothGB.BringToFront();
                    break;
                case 3:
                    convolutionGB.BringToFront();
                    break;
                case 4:
                    suppressGB.BringToFront();
                    break;
                case 5:
                    editSimpleGB.BringToFront();
                    break;
                case 6:
                    cuttingGB.BringToFront();
                    break;
                default:
                    break;
            }
        }

        private void BlowBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.make_blowout(modelsList.SelectedItem.ToString());

            change_list();
            return;
        }

        private void UnBlowBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.del_blowout(modelsList.SelectedItem.ToString());

            change_list();
            return;
        }

        private void FurieBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.get_furie(modelsList.SelectedItem.ToString());

            change_list();
            return;
        }

        private void IntercorrFirstCB_Click(object sender, EventArgs e)
        {
            IntercorrFirstCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            IntercorrFirstCB.Items.AddRange(tmp);

            return;
        }

        private void IntercorrSecondCB_Click(object sender, EventArgs e)
        {
            IntercorrSecondCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            IntercorrSecondCB.Items.AddRange(tmp);

            return;
        }

        private void IntercorrBtn_Click(object sender, EventArgs e)
        {
            calc_core.intercorr(IntercorrFirstCB.SelectedItem.ToString(), IntercorrSecondCB.SelectedItem.ToString(), IntercorrName.Text);

            change_list();
        }

        private void IntercorrFirstCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((IntercorrFirstCB.SelectedItem == null) || (IntercorrSecondCB.SelectedItem == null))
                return;

            IntercorrName.Text = IntercorrFirstCB.SelectedItem.ToString() + "INTERCORR" + IntercorrSecondCB.SelectedItem.ToString();
            return;
        }

        private void IntercorrSecondCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((IntercorrFirstCB.SelectedItem == null) || (IntercorrSecondCB.SelectedItem == null))
                return;

            IntercorrName.Text = IntercorrFirstCB.SelectedItem.ToString() + "INTERCORR" + IntercorrSecondCB.SelectedItem.ToString();
            return;
        }

        private void ToNullBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.to_null(modelsList.SelectedItem.ToString());

            change_list();
            return;
        }

        private void AverSmoothBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.aver_smooth(modelsList.SelectedItem.ToString(), Convert.ToInt32(SmoothWindowUD.Value), EditName.Text);
            change_list();
        }

        private void UniteBtn_Click(object sender, EventArgs e)
        {
            if (!modelsList.Items.Contains(FirstCB.SelectedItem) || !modelsList.Items.Contains(SecondCB.SelectedItem))
                return;

            calc_core.unite_models(FirstCB.SelectedItem.ToString(), SecondCB.SelectedItem.ToString(), EditName.Text);

            change_list();
            return;
        }

        private void convFirstCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((convFirstCB.SelectedItem == null) || (convSecondCB.SelectedItem == null))
                return;

            EditName.Text = convFirstCB.SelectedItem.ToString() + "CONVOLUTED" + convSecondCB.SelectedItem.ToString();
            return;
        }

        private void convFirstCB_Click(object sender, EventArgs e)
        {
            convFirstCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            convFirstCB.Items.AddRange(tmp);

            return;
        }

        private void convSecondCB_Click(object sender, EventArgs e)
        {
            convSecondCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            convSecondCB.Items.AddRange(tmp);

            return;
        }

        private void convSecondCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((convFirstCB.SelectedItem == null) || (convSecondCB.SelectedItem == null))
                return;

            EditName.Text = convFirstCB.SelectedItem.ToString() + "CONVOLUTED" + convSecondCB.SelectedItem.ToString();
            return;
        }

        private void convolutionBtn_Click(object sender, EventArgs e)
        {
            if (!modelsList.Items.Contains(convFirstCB.SelectedItem) || !modelsList.Items.Contains(convSecondCB.SelectedItem))
                return;

            calc_core.convolute(convFirstCB.SelectedItem.ToString(), convSecondCB.SelectedItem.ToString(), EditName.Text);

            change_list();
            return;
        }

        private void editMultBtn_Click(object sender, EventArgs e)
        {
            if (!modelsList.Items.Contains(FirstCB.SelectedItem) || !modelsList.Items.Contains(SecondCB.SelectedItem))
                return;

            calc_core.mult_models(FirstCB.SelectedItem.ToString(), SecondCB.SelectedItem.ToString(), EditName.Text);

            change_list();
            return;
        }

        private void suppressInterBtn_Click(object sender, EventArgs e)
        {

        }

        private void suppressWindowBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.suppressSlideWindow(modelsList.SelectedItem.ToString(), Convert.ToInt32(suppressWindowNUD.Value));

            change_list();
            return;
        }
        
        private void paleLoadBtn_Click(object sender, EventArgs e)
        {
            if (fileLoadTB.Text == "")
            {
                MessageBox.Show("Please, enter model name");
                return;
            }

            openFileDialog1.Filter = "data files (*.dat) | *.dat";
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            

            BinaryReader reader = new BinaryReader(File.OpenRead(openFileDialog1.FileName));
            double[] data = new double[reader.BaseStream.Length / 4];
            double step = reader.ReadDouble();
            for (int i = 0; i < (reader.BaseStream.Length - 8) / 4; i++)
            {
                data[i] = reader.ReadSingle();
            }

            reader.Close();

            calc_core.load_model(data, fileLoadTB.Text, step);

            change_list();
        }

        private void fileSaveBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            saveFileDialog1.Filter = "data files (*.dat) | *.dat";
            saveFileDialog1.FileName = modelsList.SelectedItem.ToString() + "_dt" + calc_core.get_params(modelsList.SelectedItem.ToString())["step"];
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            BinaryWriter writer = new BinaryWriter(File.OpenWrite(saveFileDialog1.FileName));

            double[] res = calc_core.get_result(modelsList.SelectedItem.ToString());

            writer.Write(calc_core.get_params(modelsList.SelectedItem.ToString())["step"]);

            for (int i = 0; i < res.Length; i++)
                writer.Write(Convert.ToSingle(res[i]));

            writer.Close();
        }
        
        private void SnrCalcBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            double[] tmp = calc_core.snr_calculate(modelsList.SelectedItem.ToString(), Convert.ToInt32(SnrStepsNUD.Value));
            change_list();

            SnrRasultLabel.Text = "f(" + SnrControl0NUD.Value + ") = " + tmp[Convert.ToInt32(SnrControl0NUD.Value)] + ";\n" +
                "f(" + SnrControl1NUD.Value + ") = " + Convert.ToInt32(tmp[Convert.ToInt32(SnrControl1NUD.Value - 1)]) + ";\n" +
                "f(" + SnrControl2NUD.Value + ") = " + Convert.ToInt32(tmp[Convert.ToInt32(SnrControl2NUD.Value - 1)]) + ";\n" +
                "f(" + SnrControl3NUD.Value + ") = " + Convert.ToInt32(tmp[Convert.ToInt32(SnrControl3NUD.Value - 1)]) + ";\n";
        }

        private void fileWavLoad_Click(object sender, EventArgs e)
        {
            if (fileLoadTB.Text == "")
            {
                MessageBox.Show("Please, enter model name");
                return;
            }

            openFileDialog1.Filter = "wav files (*.wav) | *.wav";
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            BinaryReader reader = new BinaryReader(File.OpenRead(openFileDialog1.FileName));

            /*if ((readLEBE(ref reader, 4) != 0x52494646) ||
                (readLEBE(ref reader, 4, true) != reader.BaseStream.Length - 8) ||
                (readLEBE(ref reader, 4) != 0x57415645) ||
                (readLEBE(ref reader, 4) != 0x666d7420) ||
                (readLEBE(ref reader, 2, true) != 1))
            {
                MessageBox.Show("Currupted or compressed file");
                reader.Close();
                return;
            }*/

            bool control0 = readLEBE(ref reader, 4) != 0x52494646;
            bool control1 = readLEBE(ref reader, 4, true) != reader.BaseStream.Length - 8;
            bool control2 = readLEBE(ref reader, 4) != 0x57415645;
            bool control3 = readLEBE(ref reader, 4) != 0x666d7420;
            bool control4 = readLEBE(ref reader, 4, true) != 20;
            bool control5 = readLEBE(ref reader, 2, true) != 1;

            int channelsNum = readLEBE(ref reader, 2, true);
            int sampleRate = readLEBE(ref reader, 4, true);
            int byteRate = readLEBE(ref reader, 4, true);
            int blockAlign = readLEBE(ref reader, 2, true);
            int bitPerSample = readLEBE(ref reader, 2, true);

            if (readLEBE(ref reader, 4) != 0x64617461)
            {
                MessageBox.Show("Currupted file");
                reader.Close();
                return;
            }
            int dataSize = readLEBE(ref reader, 4, true);
            double[][] tracks = new double[channelsNum][];

            for (int i = 0; i < channelsNum; i++)
                tracks[i] = new double[dataSize / (bitPerSample / 8) / channelsNum];

            for(int i = 0; i < dataSize / (bitPerSample / 8); i++)
            {
                if (bitPerSample <= 16)
                    tracks[i % channelsNum][i / channelsNum] = reader.ReadInt16();
                else
                    tracks[i % channelsNum][i / channelsNum] = reader.ReadDouble();

            }

            calc_core.load_model(tracks[0], fileLoadTB.Text + "_track0", 1 / Convert.ToDouble(sampleRate));

            if(channelsNum == 2)
                calc_core.load_model(tracks[0], fileLoadTB.Text + "_track1", 1 / Convert.ToDouble(sampleRate));

            reader.Close();

            change_list();
        }

        int readLEBE(ref BinaryReader reader, int size, bool LEmode = false)
        {
            int result = 0;
            for (int i = 0; i < size; i++)
            {
                int offs = (LEmode) ? i : size - i - 1;
                result += reader.ReadByte() << (offs * 8);
            }
            return result;
        }
        
        private void simpleRenameBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            if (EditName.Text == "")
            {
                MessageBox.Show("Please, enter model new name");
                return;
            }

            calc_core.renameModel(modelsList.SelectedItem.ToString(), EditName.Text);

            change_list();
        }

        private void simpleSumBtn_Click(object sender, EventArgs e)
        {

            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.sumNumToModel(modelsList.SelectedItem.ToString(), Convert.ToDouble(simpleEditNUD.Value), EditName.Text);
            change_list();
        }

        private void simpleMultBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.multModelToNum(modelsList.SelectedItem.ToString(), Convert.ToDouble(simpleEditNUD.Value), EditName.Text);
            change_list();
        }

        private void propRefreshBtn_Click(object sender, EventArgs e)
        {
            modelPropView.Rows.Clear();

            if (modelsList.SelectedIndex == -1)
                return;

            Dictionary<string, double> param = calc_core.get_params(modelsList.SelectedItem.ToString());

            foreach(KeyValuePair<string, double> cur in param)
                modelPropView.Rows.Add(cur.Key, cur.Value);

        }

        private void propCancelBtn_Click(object sender, EventArgs e)
        {
            propRefreshBtn_Click(sender, e);
        }

        private void propAcceptBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            Dictionary<string, double> param = new Dictionary<string, double>();

            for (int i = 0; i < modelPropView.Rows.Count - 1; i++)
            {
                string paramName = modelPropView[0, i].EditedFormattedValue.ToString();
                string tmp = modelPropView[1, i].Value.ToString();
                double paramVal = Convert.ToDouble(tmp);
                param.Add(paramName, paramVal);
            }

            calc_core.setParams(modelsList.SelectedItem.ToString(), param);
        }

        private void modulOriginalCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((modulOriginalCB.SelectedItem == null) || (modulModulatingCB.SelectedItem == null))
                return;

            modulNameTB.Text = modulOriginalCB.SelectedItem.ToString() + "CONVOLUTED" + modulModulatingCB.SelectedItem.ToString();
            return;
        }

        private void modulModulatingCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((modulOriginalCB.SelectedItem == null) || (modulModulatingCB.SelectedItem == null))
                return;

            modulNameTB.Text = modulOriginalCB.SelectedItem.ToString() + "MODULED_BY" + modulModulatingCB.SelectedItem.ToString();
            return;
        }

        private void modulOriginalCB_Click(object sender, EventArgs e)
        {
            modulOriginalCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            modulOriginalCB.Items.AddRange(tmp);

        }

        private void modulModulatingCB_Click(object sender, EventArgs e)
        {
            modulModulatingCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            modulModulatingCB.Items.AddRange(tmp);
        }

        private void modulModulationBtn_Click(object sender, EventArgs e)
        {
            if (!modelsList.Items.Contains(modulModulatingCB.SelectedItem) || !modelsList.Items.Contains(modulOriginalCB.SelectedItem))
                return;

            calc_core.modulation(modulOriginalCB.SelectedItem.ToString(), modulModulatingCB.SelectedItem.ToString(), modulNameTB.Text, Convert.ToDouble(modulCoefficientNUD.Value));

            //calc_core.snrModulation(modulOriginalCB.SelectedItem.ToString(), modulModulatingCB.SelectedItem.ToString());

            change_list();
            return;
        }

        private void modulDemodulationBtn_Click(object sender, EventArgs e)
        {
            if (!modelsList.Items.Contains(modulModulatingCB.SelectedItem) || !modelsList.Items.Contains(modulOriginalCB.SelectedItem))
                return;

            calc_core.demodulation(modulOriginalCB.SelectedItem.ToString(), modulModulatingCB.SelectedItem.ToString(), modulNameTB.Text, Convert.ToDouble(modulCoefficientNUD.Value));

            change_list();
            return;
        }

        private void absBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.absModel(modelsList.SelectedItem.ToString(), EditName.Text);
            change_list();
        }
        

        private void cutBtn_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            calc_core.cutModel(modelsList.SelectedItem.ToString(), EditName.Text, Convert.ToDouble(cutFromNUD.Value), Convert.ToDouble(cutToNUD.Value));
            change_list();
        }

        private void EtalonModelCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((geneticalEtalonModelCB.SelectedItem == null) || (geneticalModelCB.SelectedItem == null))
                return;

            geneticalNameTB.Text = geneticalModelCB.SelectedItem.ToString() + "EVOLUTED_TO" + geneticalEtalonModelCB.SelectedItem.ToString();
            return;
        }

        private void GeneticalModelCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((geneticalEtalonModelCB.SelectedItem == null) || (geneticalModelCB.SelectedItem == null))
                return;

            geneticalNameTB.Text = geneticalModelCB.SelectedItem.ToString() + "EVOLUTED_TO" + geneticalEtalonModelCB.SelectedItem.ToString();
            return;
        }

        private void EtalonModelCB_Click(object sender, EventArgs e)
        {
            geneticalEtalonModelCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            geneticalEtalonModelCB.Items.AddRange(tmp);
        }

        private void GeneticalModelCB_Click(object sender, EventArgs e)
        {
            geneticalModelCB.Items.Clear();
            string[] tmp = new string[modelsList.Items.Count];
            modelsList.Items.CopyTo(tmp, 0);

            geneticalModelCB.Items.AddRange(tmp);

        }

        private void geneticalBtn_Click(object sender, EventArgs e)
        {
            calc_core.evolution(geneticalModelCB.SelectedItem.ToString(), geneticalEtalonModelCB.SelectedItem.ToString(), geneticalNameTB.Text, Convert.ToInt32(geneticalStepsNUD.Value));
            change_list();
        }

        private void fileWavSave_Click(object sender, EventArgs e)
        {
            if (modelsList.SelectedIndex == -1)
                return;

            saveFileDialog1.Filter = "WAV files (*.wav) | *.wav";
            saveFileDialog1.FileName = modelsList.SelectedItem.ToString() + "_dt" + calc_core.get_params(modelsList.SelectedItem.ToString())["step"];
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            BinaryWriter writer = new BinaryWriter(File.OpenWrite(saveFileDialog1.FileName));

            double[] res = calc_core.get_result(modelsList.SelectedItem.ToString());


            //bool control0 = readLEBE(ref reader, 4) != 0x52494646;
            

            int tmp = 0x52494646;
            byte[] bTmp = BitConverter.GetBytes(tmp);
            Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);

            //bool control1 = readLEBE(ref reader, 4, true) != reader.BaseStream.Length - 8;

            tmp = res.Length * 2 + 36;
            bTmp = BitConverter.GetBytes(tmp);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);

            //bool control2 = readLEBE(ref reader, 4) != 0x57415645;

            tmp = 0x57415645;
            bTmp = BitConverter.GetBytes(tmp);
            Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);
            
            //bool control3 = readLEBE(ref reader, 4) != 0x666d7420;

            tmp = 0x666d7420; 
            bTmp = BitConverter.GetBytes(tmp);
            Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);

            //bool control4 = readLEBE(ref reader, 4, true) != 16;

            tmp = 16;
            bTmp = BitConverter.GetBytes(tmp);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);

            //bool control5 = readLEBE(ref reader, 2, true) != 1;

            Int16 tmp16 = 1;
            bTmp = BitConverter.GetBytes(tmp16);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            
            //int channelsNum = readLEBE(ref reader, 2, true);

            tmp16 = 1;
            bTmp = BitConverter.GetBytes(tmp16);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);

            //int sampleRate = readLEBE(ref reader, 4, true);

            tmp = Convert.ToInt32(1.0 / calc_core.get_params(modelsList.SelectedItem.ToString())["step"]);
            bTmp = BitConverter.GetBytes(tmp);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);

            //int byteRate = readLEBE(ref reader, 4, true);

            tmp = Convert.ToInt32(2.0 / calc_core.get_params(modelsList.SelectedItem.ToString())["step"]);
            bTmp = BitConverter.GetBytes(tmp);
           // Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);

            //int blockAlign = readLEBE(ref reader, 2, true);

            tmp16 = 2;
            bTmp = BitConverter.GetBytes(tmp16);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);

            //int bitPerSample = readLEBE(ref reader, 2, true);

            tmp16 = 16;
            bTmp = BitConverter.GetBytes(tmp16);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            
            //if (readLEBE(ref reader, 4) != 0x64617461)

            tmp = 0x64617461;
            bTmp = BitConverter.GetBytes(tmp);
            Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);
            
            //int dataSize = readLEBE(ref reader, 4, true);

            tmp = res.Length * 2;
            bTmp = BitConverter.GetBytes(tmp);
            //Array.Reverse(bTmp);
            writer.Write(bTmp[0]);
            writer.Write(bTmp[1]);
            writer.Write(bTmp[2]);
            writer.Write(bTmp[3]);
            
            for (int i = 0; i < res.Length; i++)
                writer.Write(Convert.ToInt16(res[i]));

            writer.Close();
        }
        
    }
}
