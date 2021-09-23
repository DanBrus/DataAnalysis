﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;


namespace MOAED_2_sem
{
    class Core
    {
        private List<IModel> models;
        private List<string> models_names;

        public Core()
        {
            AppDomain.MonitoringIsEnabled = true;
            models = new List<IModel>();
            models_names = new List<string>();
            
        }

        interface IModel
        {
            double[] red { get; set; }
            double[] green { get; set; }
            double[] blue { get; set; }
            Dictionary<string, double> param { get; set; }
        }

        class IOModule
        {

        }

        class Analysis
        {
            public static void hystohramm(IModel cur)
            {
                Process.normalizePicture(ref cur);

                double[] hystoR = new double[256];
                double[] hystoG = new double[256];
                double[] hystoB = new double[256];

                for (int i = 0; i < cur.param["step_ctr"]; i++)
                {
                    hystoR[Convert.ToInt32(cur.red[i])]++;
                    hystoG[Convert.ToInt32(cur.green[i])]++;
                    hystoB[Convert.ToInt32(cur.blue[i])]++;
                }

                cur.red = hystoR;
                cur.green = hystoG;
                cur.blue = hystoB;

                cur.param["step_ctr"] = 256;
                cur.param["start"] = 0;
                cur.param["step"] = 1;
                cur.param.Remove("width");
                cur.param.Remove("height");
            }

            public static void distribution(IModel cur)
            {
                double[] distribR = new double[Convert.ToInt32(cur.param["step_ctr"])];
                double[] distribG = new double[Convert.ToInt32(cur.param["step_ctr"])];
                double[] distribB = new double[Convert.ToInt32(cur.param["step_ctr"])];

                distribR[0] = cur.red[0];
                distribG[0] = cur.green[0];
                distribB[0] = cur.blue[0];

                for (int i = 1; i < Convert.ToInt32(cur.param["step_ctr"]); i++)
                {
                    distribR[i] = distribR[i - 1] + cur.red[i];
                    distribG[i] = distribG[i - 1] + cur.green[i];
                    distribB[i] = distribB[i - 1] + cur.blue[i];
                }

                cur.red = distribR;
                cur.green = distribG;
                cur.blue = distribB;
            }


            public static double average(double[] realisation)
            {
                double sum = 0;
                for (int i = 0; i < realisation.Length; i++)
                    sum += realisation[i];
                return sum / realisation.Length;
            }

            public static IModel smoothing(IModel cur_mod, int type, int param = 0)
            {
                switch (type)
                {
                    case (0):   //Скользящее среднее
                        {
                            cur_mod.red = Process.aver_smooth(cur_mod.red, param);
                            break;
                        }
                }
                return cur_mod;
            }

            public static double[] intercorr(double[] first, double[] second)
            {
                double[] tmp = new double[first.Length];

                for (int i = 0; i < first.Length; i++)
                {
                    int j = 0;
                    for (j = 0; (j < first.Length) && (j + i < second.Length); j++)
                        tmp[i] += first[j] * second[j + i];

                    tmp[i] /= j;
                }

                return tmp;
            }
        }

        class Process
        {
            //public static void IdealFilter(IModel input, IModel kern)
            //{
            //    if (kern.Count < input.Count)
            //    {
            //        for (int i = kern.Count; i < input.Count; i++)
            //        {
            //            kern.Add(i, 0);
            //        }
            //    }
            //
            //    List<Complex> kernSpectre = GetComplexSpectre(GetComplexes(kern), false);
            //    List<Complex> inputSpectre = GetComplexSpectre(GetComplexes(input));
            //
            //    List<Complex> resultSpectre = new List<Complex>();
            //    for (int i = 0; i < inputSpectre.Count; i++)
            //    {
            //        resultSpectre.Add(inputSpectre[i] / kernSpectre[i]);
            //    }
            //
            //    List<Complex> reversedRow = GetReverseComplexSpectre(resultSpectre);
            //
            //    return GetRealsFromComplexes(reversedRow);
            //}


            public static void resizeImageFurie(IModel cur, double scaleX, double scaleY)
            {
                furieTransform(cur);

                int height = Convert.ToInt32(cur.param["height"]);
                int width = Convert.ToInt32(cur.param["width"]);
                int newHeight = (int)Math.Round(height * scaleY);
                int newWidth = (int)Math.Round(width * scaleX);
                int deltaHeight = newHeight - height;
                int deltaWidth = newWidth - width;

                double[] redChan = new double[newHeight * newWidth];
                double[] greChan = new double[newHeight * newWidth];
                double[] bluChan = new double[newHeight * newWidth];

                for(int i = 0; i < width; i++)
                {
                    int i1 = i + deltaWidth / 2;
                    for(int j = 0; j < height; j++)
                    {
                        int j1 = j + deltaHeight / 2;

                        redChan[j1 + i1 * newHeight] = cur.red[j + i * height];
                        greChan[j1 + i1 * newHeight] = cur.green[j + i * height];
                        bluChan[j1 + i1 * newHeight] = cur.blue[j + i * height];
                    }
                }

                cur.red = redChan;
                cur.green = greChan;
                cur.blue = bluChan;
                cur.param["height"] = newHeight;
                cur.param["width"] = newWidth;

                furieTransformBack(cur);
            }

            public static double[] bandStopFilter(double f, double S, int length, double step = 1.0)
            {
                double[] tmp1 = Process.lppotter(f - S / 2, Convert.ToInt32(length), step);
                double[] tmp2 = Process.lppotter(f + S / 2, Convert.ToInt32(length), step);

                double[] res = new double[tmp1.Length];

                for (int i = 0; i <= 2 * length; i++)
                {
                    if (i == length)
                        res[i] = 1.0 - tmp2[i] + tmp1[i];
                    else
                        res[i] = tmp1[i] - tmp2[i];
                }

                return res;
            }

            public static void deleteLines(IModel cur, int step)
            {
                double[][] img = new double[Convert.ToInt32(cur.param["width"])][];
                double[] oldDer, curDer = null;

                double maxFrequency = 0;
                int count = 0;

                img[0] = new double[Convert.ToInt32(cur.param["height"])];

                for (int j = 0; j < cur.param["height"]; j++)
                    img[0][j] = cur.red[j];
                oldDer = derivative(img[0]);

                for (int i = 1; i < cur.param["width"]; i ++)
                {
                    img[i] = new double[Convert.ToInt32(cur.param["height"])];

                    for (int j = 0; j < cur.param["height"]; j++)
                        img[i][j] = cur.red[j + i * Convert.ToInt32(cur.param["height"])];

                    if (i % step != 0)
                        continue;

                    curDer = derivative(img[i]);


                    curDer = Analysis.intercorr(oldDer, curDer);

                    int pOf2 = 2;
                    while (pOf2 * 2 <= oldDer.Length)
                        pOf2 *= 2;

                    complex[] spec = new complex[pOf2];
                    for (int j = 0; j < pOf2; j++)
                    {
                        spec[j] = new complex(curDer[j]);
                    }

                    spec = lineFurie(spec);

                    double indOfMax = spec.Length / 6;

                    for (int j = 10; j < spec.Length / 2; j++)
                    {
                        if (spec[Convert.ToInt32(indOfMax)] < spec[j])
                        {
                            indOfMax = j;
                        }
                    }

                    maxFrequency += indOfMax / spec.Length;
                    count++;
                }
                maxFrequency /= count;

                MessageBox.Show("Частота: " + maxFrequency + "\nКоличество обработанных линий: " + count, "Характеристики максимума", MessageBoxButtons.OK);

                double[] bandPassFilter = bandStopFilter(maxFrequency - maxFrequency / 10, maxFrequency + maxFrequency / 10, 80);
                
                double[] newRow;
                for (int i = 0; i < cur.param["width"]; i++)
                {
                    newRow = convoluteLines(img[i], bandPassFilter);

                    double[] cutRow = new double[Convert.ToInt32(cur.param["height"])];

                    int end = 50 + img[i].Length;
                    for (int j = 50; j < end; j++)
                        cutRow[j - 50] = newRow[j];

                    cutRow.CopyTo(cur.red, i * Convert.ToInt32(cur.param["width"]));
                    cutRow.CopyTo(cur.green, i * Convert.ToInt32(cur.param["width"]));
                    cutRow.CopyTo(cur.blue, i * Convert.ToInt32(cur.param["width"]));
                }
            }

            static bool isPowOf2(double num)
            {
                if (num == 1)
                    return true;
                else if (num < 1)
                    return false;
                else
                    return isPowOf2(num / 2);
            }

            public class complex
            {
                public complex()
                {
                    R = 0; I = 0;
                }

                public complex(double r, double i)
                {
                    R = r;
                    I = i;
                }

                public complex(double arg)
                {
                    R = arg;
                    I = 0;
                }
                public double R;
                public double I;

                public static bool operator <(complex a, complex b)
                {
                    return a.ToDouble() < b.ToDouble();
                }

                public static bool operator >(complex a, complex b)
                {
                    return a.ToDouble() > b.ToDouble();
                }

                public static complex operator -(complex a, complex b)
                {
                    return new complex(a.R - b.R, a.I - b.I);
                }

                public static complex operator +(complex a, complex b)
                {
                    return new complex(a.R + b.R, a.I + b.I);
                }

                public static complex operator *(complex a, complex b)
                {
                    return new complex(a.R * b.R - a.I * b.I, a.I * b.R + b.I * a.R);
                }

                public static complex operator *(complex a, double b)
                {
                    return new complex(a.R * b, a.I * b);
                }

                public static complex operator /(complex a, double b)
                {
                    return a * (1 / b);
                }

                public static complex operator /(double a, complex b)
                {
                    return new complex(a * b.R / (Math.Pow(b.R, 2) + Math.Pow(b.I, 2)), a * b.I / (Math.Pow(b.R, 2) + Math.Pow(b.I, 2)));
                }

                public static complex operator /(complex a, complex b)
                {
                    return new complex((a.R * b.R + a.I * b.I) / (b.R * b.R + b.I * b.I), (b.R * a.I - a.R * b.I) / (b.R * b.R + b.I * b.I));
                }

                public static complex exp(double arg)
                {
                    return new complex(Math.Cos(arg * 2.0 * Math.PI), Math.Sin(arg * 2.0 * Math.PI));
                }

                public double ToDouble()
                {
                    return Math.Sqrt(R * R + I * I);
                }
            }

            public static void filtreImageByFurie(IModel cur, IModel func, double alpha = 0)
            {
                double[][] image = new double[Convert.ToInt32(cur.param["width"])][];

                //Разбиваем изображение на столбцы
                for (int i = 0; i < image.Length; i++)
                {
                    image[i] = new double[Convert.ToInt32(cur.param["height"])];
                    for (int j = 0; j < image[i].Length; j++)
                        image[i][j] = cur.red[j + i * Convert.ToInt32(cur.param["height"])];
                    image[i] = IdealFilter(image[i], func.red);
                }

                image = rotateImg(image, false);

                for (int i = 0; i < image.Length; i++)
                    image[i] = IdealFilter(image[i], func.red);

                image = rotateImg(image, true);

                for (int i = 0; i < image.Length; i++)
                {
                    image[i] = new double[Convert.ToInt32(cur.param["height"])];
                    for (int j = 0; j < image[i].Length; j++)
                        cur.red[j + i * Convert.ToInt32(cur.param["height"])] = image[i][j];
                }
            }

            public static double[] IdealFilter(double[] input, double[] kern)
            {
                complex[] image = new complex[input.Length];
                complex[] filter = new complex[input.Length];
                for(int i = 0; i < image.Length; i++)
                {
                    image[i] = new complex(input[i], 0);
                    filter[i] = (i < kern.Length) ? new complex(kern[i], 0) : new complex();
                }

                complex[] filterFurie = lineFurie(filter);
                complex[] imageFurie = lineFurie(image);

                complex[] resultFurie = new complex[input.Length];
                for (int i = 0; i < imageFurie.Length; i++)
                {
                    resultFurie[i] = imageFurie[i] / filterFurie[i];
                }

                complex[] result = lineFurie(resultFurie, true);
                double[] res = new double[result.Length];

                for (int i = 0; i < result.Length; i++)
                    res[i] = result[i].R;

                return res;
            }

            static complex[] lineFurie(complex[] x, bool back = false)
            {
                if (x.Length % 2 != 0)
                {
                    complex[] tmp = longLineFurie(x, back);

                    return tmp;
                }

                complex[] X;
                int N = x.Length;
                if (N == 2)
                {
                    X = new complex[2];
                    X[0] = x[0] + x[1];
                    X[1] = x[0] - x[1];
                   
                    return X;
                }
                else
                {
                    complex[] x_even = new complex[N / 2];
                    complex[] x_odd = new complex[N / 2];
                    for (int i = 0; i < N / 2; i++)
                    {
                        x_even[i] = x[2 * i];
                        x_odd[i] = x[2 * i + 1];
                    }
                    complex[] X_even = lineFurie(x_even, back);
                    complex[] X_odd = lineFurie(x_odd, back);
                    X = new complex[N];
                    for (int i = 0; i < N / 2; i++)
                    {
                        if (!back)
                        {
                            X[i] = X_even[i] + ((i % N == 0) ? new complex(1, 0) : complex.exp(-1.0 * Convert.ToDouble(i) / N)) * X_odd[i];
                            X[i + N / 2] = X_even[i] - ((i % N == 0) ? new complex(1, 0) : complex.exp(-1.0 * Convert.ToDouble(i) / N)) * X_odd[i];
                        }
                        else
                        {
                            X[i] = X_even[i] + ((i % N == 0) ? new complex(1, 0) : complex.exp(1.0 * Convert.ToDouble(i) / N)) * X_odd[i];
                            X[i + N / 2] = X_even[i] - ((i % N == 0) ? new complex(1, 0) : complex.exp(1.0 * Convert.ToDouble(i) / N)) * X_odd[i];
                        }
                    }

                    return X;
                }
            }

            static complex[] longLineFurie(complex[] x, bool back = false)
            {
                complex[] spec = new complex[x.Length];

                for (int n = 0; n < x.Length; n++)
                {
                    spec[n] = new complex();
                    for (int k = 0; k < x.Length; k++)
                    {
                        if(!back)
                            spec[n] += x[k] * complex.exp(-1 * Convert.ToDouble(n * k) / Convert.ToDouble(x.Length));
                        else
                            spec[n] += x[k] * complex.exp(Convert.ToDouble(n * k) / Convert.ToDouble(x.Length));
                    }
                }

                return spec;
            }

            public static double[] derivative(double[] cur)
            {
                double[] tmp = new double[cur.Length - 1];

                for(int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = cur[i + 1] - cur[i];
                }

                return tmp;
            }

            static complex[][] rotateImg(complex[][] img, bool againSun)
            {
                complex[][] rotImg = new complex[img[0].Length][];
                for (int i = 0; i < rotImg.Length; i++)
                    rotImg[i] = new complex[img.Length];

                for(int i = 0; i < img.Length; i++)
                {
                    for(int j = 0; j < img[0].Length; j++)
                    {
                        if(againSun)
                            rotImg[j][img.Length - i - 1] = img[i][j];
                        else
                            rotImg[img[0].Length - j - 1][i] = img[i][j];
                    }
                }

                return rotImg;
            }

            static double[][] rotateImg(double[][] img, bool againSun)
            {
                double[][] rotImg = new double[img[0].Length][];
                for (int i = 0; i < rotImg.Length; i++)
                    rotImg[i] = new double[img.Length];

                for (int i = 0; i < img.Length; i++)
                {
                    for (int j = 0; j < img[0].Length; j++)
                    {
                        if (againSun)
                            rotImg[j][img.Length - i - 1] = img[i][j];
                        else
                            rotImg[img[0].Length - j - 1][i] = img[i][j];
                    }
                }

                return rotImg;
            }
            public static void furieTransform(IModel cur)
            {

                double[] newChannel = new double[cur.red.Length];
                double[] newChannelR = new double[cur.red.Length];
                double[] newChannelI = new double[cur.red.Length];

                complex[][] image = new complex[Convert.ToInt32(cur.param["width"])][];

                //Разбиваем изображение на столбцы, сразу проводим быстрое преобразование Фурье каждого столбца
                for (int i = 0; i < image.Length; i++)
                {
                    image[i] = new complex[Convert.ToInt32(cur.param["height"])];
                    for (int j = 0; j < image[i].Length; j++)
                    {
                        image[i][j] = new complex();
                        image[i][j].R = cur.red[j + i * Convert.ToInt32(cur.param["height"])];
                    }

                    image[i] = lineFurie(image[i]);
                }

                if (cur.param["width"] == 1)
                {
                    for (int j = 0; j < image[0].Length; j++)
                    {
                        newChannel[j] = image[0][j].ToDouble();
                        newChannelR[j] = image[0][j].R;
                        newChannelI[j] = image[0][j].I;
                    }
                    cur.red = new double[newChannel.Length];
                    cur.green = new double[newChannelR.Length];
                    cur.blue = new double[newChannelI.Length];

                    newChannel.CopyTo(cur.red, 0);
                    newChannelR.CopyTo(cur.green, 0);
                    newChannelI.CopyTo(cur.blue, 0);
                    cur.param.Add("maxAmplitude", cur.red.Max());
                    return;
                }

                //Поворачиваем изображение, проводим преобразование Фурье каждого  столбца
                complex[][] imageR = rotateImg(image, false);
                for (int i = 0; i < imageR.Length; i++)
                {
                    imageR[i] = lineFurie(imageR[i]);
                }
                
                //поворачиваем спектр, 
                image = rotateImg(imageR, true);

                //записывая в качестве яркости пикселя модуль комплексного числа, а так же смещаем низкие частоты в центр изображения.
                for (int i = 0; i < image.Length; i++)
                {
                    for (int j = 0; j < image[i].Length; j++)
                    {
                        int i1 = (i + Convert.ToInt32(cur.param["width"]) / 2) % Convert.ToInt32(cur.param["width"]);
                        int j1 = (j + Convert.ToInt32(cur.param["height"]) / 2) % Convert.ToInt32(cur.param["height"]);

                        newChannel[j1 + i1 * Convert.ToInt32(cur.param["height"])] = image[i][j].ToDouble();
                        newChannelR[j1 + i1 * Convert.ToInt32(cur.param["height"])] = image[i][j].R;
                        newChannelI[j1 + i1 * Convert.ToInt32(cur.param["height"])] = image[i][j].I;
                    }
                }

                cur.red = new double[newChannel.Length];
                cur.green = new double[newChannelR.Length];
                cur.blue = new double[newChannelI.Length];

                newChannel.CopyTo(cur.red, 0);
                newChannelR.CopyTo(cur.green, 0);
                newChannelI.CopyTo(cur.blue, 0);
                cur.param.Add("maxAmplitude", cur.red.Max());
            }

            public static void furieTransformBack(IModel cur)
            {
                double scale = cur.param["maxAmplitude"] / cur.red.Max();
                multNumToModel(ref cur, scale);


                complex[][] image = new complex[Convert.ToInt32(cur.param["width"])][];
                for (int i = 0; i < image.Length; i++)
                    image[i] = new complex[Convert.ToInt32(cur.param["height"])];

                //Разбиваем изображение на столбцы, а так же перемещаем низкие частоты в углы изображения
                for (int i1 = 0; i1 < image.Length; i1++)
                {
                    int i2;

                    if (cur.param["width"] == 1)
                        i2 = i1;
                    else
                        i2 = (i1 + Convert.ToInt32(cur.param["width"]) / 2) % Convert.ToInt32(cur.param["width"]);
                    for (int j1 = 0; j1 < image[i1].Length; j1++)
                    {
                        int j2;
                        if (cur.param["width"] == 1)
                            j2 = j1;
                        else
                            j2 = (j1 + Convert.ToInt32(cur.param["height"]) / 2) % Convert.ToInt32(cur.param["height"]);

                        image[i2][j2] = new complex();
                        image[i2][j2].R = cur.green[j1 + i1 * Convert.ToInt32(cur.param["height"])];
                        image[i2][j2].I = cur.blue[j1 + i1 * Convert.ToInt32(cur.param["height"])];
                    }

                }

                double[] newChannel = new double[cur.red.Length];
                if (cur.param["width"] == 1)
                {
                    for (int j = 0; j < image[0].Length; j++)
                        image[0][j].I *= -1;

                    image[0] = lineFurie(image[0], true);

                    for (int j = 0; j < image[0].Length; j++)
                    {
                        newChannel[j] = image[0][j].R / image[0].Length;
                    }
                    cur.red = new double[newChannel.Length];
                    cur.green = new double[newChannel.Length];
                    cur.blue = new double[newChannel.Length];
                
                    newChannel.CopyTo(cur.red, 0);
                    newChannel.CopyTo(cur.green, 0);
                    newChannel.CopyTo(cur.blue, 0);
                    return;
                }


                complex[][] imageR = rotateImg(image, false);
                
                for (int i = 0; i < imageR.Length; i++)
                {
                    imageR[i] = lineFurie(imageR[i], true);
                
                }
                
                //Поворачиваем изображение, проводим обратное преобразование Фурье каждого транспонированного столбца
                image = rotateImg(imageR, true);
                for (int i = 0; i < image.Length; i++)
                {
                    image[i] = lineFurie(image[i], true);
                }

                for (int i = 0; i < image.Length; i++)
                {
                    for (int j = 0; j < image[i].Length; j++)
                    {
                        newChannel[j + i * Convert.ToInt32(cur.param["height"])] = image[i][j].R;
                    }

                }

                newChannel.CopyTo(cur.red, 0);
                newChannel.CopyTo(cur.green, 0);
                newChannel.CopyTo(cur.blue, 0);
            }

            public static double[] greyScale(int bits)
            {
                double[] scale = new double[256];

                double lvls = Math.Pow(2, bits);
                int lvlSize = Convert.ToInt32(256 / lvls);
                double lvlUp = 255 / (lvls - 1);

                for(int i = 0; i < 256; i++)
                {
                    scale[i] = (i / lvlSize) * lvlUp;
                }

                return scale;
            }

            public static void averFilter(IModel cur, int window)
            {
                double[] redC = cur.red;
                double[] greC = cur.green;
                double[] bluC = cur.blue;

                double oldW = cur.param["width"],
                    oldH = cur.param["height"],
                    newW = Convert.ToDouble(Math.Floor(oldW - window)),
                    newH = Convert.ToDouble(Math.Floor(oldH - window));

                double[]
                    oldR = cur.red,
                    oldG = cur.green,
                    oldB = cur.blue,
                    newR = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newG = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newB = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)];

                double sumR;
                double sumG;
                double sumB;

                for (int i = 0; i < newW; i++)     //столбец
                {
                    for (int j = 0; j < newH; j++)     //строка
                    {
                        sumR = 0;
                        sumG = 0;
                        sumB = 0;

                        for (int m = i; m < i + window; m++)             //столбец для формирования матрицы 
                        {
                            for (int n = j; n < j + window; n++)         // строка для формирования матрицы
                            {
                                sumR += oldR[n + m * Convert.ToInt32(oldH)];
                                sumG += oldG[n + m * Convert.ToInt32(oldH)];
                                sumB += oldB[n + m * Convert.ToInt32(oldH)];
                            }
                        }

                        newR[j + i * Convert.ToInt32(newH)] = sumR / (window * window);
                        newG[j + i * Convert.ToInt32(newH)] = sumG / (window * window);
                        newB[j + i * Convert.ToInt32(newH)] = sumB / (window * window);
                    }
                }

                cur.red = newR;
                cur.green = newG;
                cur.blue = newB;
                cur.param["width"] = newW;
                cur.param["height"] = newH;

            }

            public static void medianFilter(IModel cur, int window)
            {

                double oldW = cur.param["width"],
                    oldH = cur.param["height"],
                    newW = Convert.ToDouble(Math.Floor(oldW - window)),
                    newH = Convert.ToDouble(Math.Floor(oldH - window));

                double[]
                    oldR = cur.red,
                    oldG = cur.green,
                    oldB = cur.blue,
                    newR = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newG = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newB = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)];

                List<double> matrixR = new List<double>();
                List<double> matrixG = new List<double>();
                List<double> matrixB = new List<double>();

                for (int i = 0; i < newW; i++)     //столбец
                {
                    for(int j = 0; j < newH; j++)     //строка
                    {
                        matrixR.Clear();
                        matrixG.Clear();
                        matrixB.Clear();

                        for (int m = i; m < i + window; m++)             //столбец для формирования матрицы 
                        {
                            for(int n = j; n < j + window; n++)         // строка для формирования матрицы
                            {
                                matrixR.Add(oldR[n + m * Convert.ToInt32(oldH)]);
                                matrixG.Add(oldR[n + m * Convert.ToInt32(oldH)]);
                                matrixB.Add(oldR[n + m * Convert.ToInt32(oldH)]);
                            }
                        }

                        matrixR.Sort();
                        matrixG.Sort();
                        matrixB.Sort();

                        newR[j + i * Convert.ToInt32(newH)] = matrixR[(window * window) / 2];
                        newG[j + i * Convert.ToInt32(newH)] = matrixG[(window * window) / 2];
                        newB[j + i * Convert.ToInt32(newH)] = matrixB[(window * window) / 2];
                    }
                }

                cur.red = newR;
                cur.green = newG;
                cur.blue = newB;
                cur.param["width"] = newW;
                cur.param["height"] = newH;

            }

            public static void subImages(IModel fromModel, IModel whatModel)
            {
                double[] fromR = fromModel.red,
                    fromG = fromModel.green,
                    fromB = fromModel.blue;

                double[] whatR = whatModel.red,
                         whatG = whatModel.green,
                         whatB = whatModel.blue;

                for(int i = 0; i < whatR.Length; i++)
                {
                    fromR[i] -= whatR[i];
                    fromR[i] /= 2;
                    fromR[i] += 127;
                    fromG[i] -= whatG[i];
                    fromG[i] /= 2;
                    fromG[i] += 127;
                    fromB[i] -= whatB[i];
                    fromB[i] /= 2;
                    fromB[i] += 127;
                }

                fromModel.red = fromR;
                fromModel.green = fromG;
                fromModel.blue = fromB;

                normalizePicture(ref fromModel);
            }

            public static void rotate(IModel cur)
            {
                double oldW = cur.param["width"],
                    oldH = cur.param["height"],
                    newW = oldH,
                    newH = oldW;
                
                double[] oldR = cur.red,
                    oldG = cur.green,
                    oldB = cur.blue,
                    newR = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newG = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newB = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)];
                
                for(int i = 0; i < oldW; i++)
                {
                    for(int j = 0; j < oldH; j++)
                    {
                        newR[i + j * Convert.ToInt32(newH)] = oldR[j + i * Convert.ToInt32(oldH)];
                        newG[i + j * Convert.ToInt32(newH)] = oldG[j + i * Convert.ToInt32(oldH)];
                        newB[i + j * Convert.ToInt32(newH)] = oldB[j + i * Convert.ToInt32(oldH)];
                    }
                }
                
                cur.red = newR;
                cur.green = newG;
                cur.blue = newB;
                cur.param["width"] = newW;
                cur.param["height"] = newH;

            }

            public static void saltPepperNoize(IModel cur, double intense)
            {
                Random rnd = new Random();

                double prob = intense / 100;

                double[] redC = cur.red;
                double[] greC = cur.green;
                double[] bluC = cur.blue;

                for (int i = 0; i < cur.param["step_ctr"]; i++)
                {
                    if (rnd.NextDouble() < prob)
                        redC[i] = (rnd.NextDouble() > 0.5) ? 255 : 0;

                    greC[i] = redC[i];
                    bluC[i] = redC[i];
                }

                cur.red = redC;
                cur.green = greC;
                cur.blue = bluC;
            }

            public static void randomNoize(IModel cur, double intense)
            {
                Random rnd = new Random();

                double maxNoize = 255.0 / 100.0 * intense;

                double[] redC = cur.red;
                double[] greC = cur.green;
                double[] bluC = cur.blue;

                for(int i = 0; i < cur.param["step_ctr"]; i++)
                {
                    redC[i] += (rnd.NextDouble() - 0.5) * 2 * maxNoize;
                    if (redC[i] < 0) redC[i] = 0;
                    else if (redC[i] > 255) redC[i] = 255;

                    greC[i] = redC[i];
                    bluC[i] = redC[i];
                }

                cur.red = redC;
                cur.green = greC;
                cur.blue = bluC;
            }

            public static double[] lppotter(double fc, int m, double dt)
            {
                double fact = 2.0 * fc * dt;

                double[] w = new double[Convert.ToInt32(m) + 1];

                w[0] = fact;
                fact *= Math.PI;

                for (int i = 1; i <= m; i++)
                    w[i] = Math.Sin(fact * i) / (Math.PI * i);

                w[Convert.ToInt32(m)] /= 2;

                double[] d = { 0.35577019, 0.2436983, 0.07211497, 0.00630164 };
                double sumg = w[0];

                for (int i = 1; i <= m; i++)
                {
                    double sum = d[0];
                    fact = Math.PI * Convert.ToDouble(i) / Convert.ToDouble(m);

                    for (int k = 1; k < 4; k++)
                        sum += 2.0 * d[k] * Math.Cos(fact * Convert.ToDouble(k));

                    w[i] *= sum;
                    sumg += 2.0 * w[i];
                }

                for (int i = 0; i <= m; i++)
                    w[i] /= sumg;

                double[] tmp = new double[w.Length * 2 - 1];
                w.CopyTo(tmp, Convert.ToInt32(m));

                for (int i = 0; i < w.Length; i++)
                    tmp[i] = tmp[tmp.Length - 1 - i];

                return tmp;
            }

            static public IModel suppressWindow(IModel model, int window)
            {
                double[] rTmp = Process.aver_smooth(model.red, window);
                double[] gTmp = Process.aver_smooth(model.green, window);
                double[] bTmp = Process.aver_smooth(model.blue, window);


                for (int i = 0; i < rTmp.Length; i++)
                {
                    rTmp[i] = model.red[i] - rTmp[i];
                    gTmp[i] = model.red[i] - gTmp[i];
                    bTmp[i] = model.red[i] - bTmp[i];

                }

                model.red = rTmp;
                model.green = gTmp;
                model.blue = bTmp;
                return model;

            }

            public static double[] aver_smooth(double[] realisation, int window)
            {
                double[] res = new double[realisation.Length - window];
                
                double[] weights = new double[window];

                for (int i = 0; i < window; i++)
                {
                    double x = Convert.ToDouble(i) / window;
                    weights[i] = (-2.0) * (x * x) + (2.0) * x + 0.5;
                }

                for (int i = 0; i < realisation.Length - window; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < window; j++) sum += realisation[i + j] * weights[j];
                    res[i] = sum / (weights.Average() * weights.Length);
                }
                return res;
            }

            public static void normalizePicture(ref IModel tmp)
            {
                double max = tmp.red.Max();

                for(int i = 0; i < tmp.red.Length; i++)
                {
                    if (tmp.red[i] < 0)
                        tmp.red[i] = 0;
                    if (tmp.green[i] < 0)
                        tmp.green[i] = 0;
                    if (tmp.blue[i] < 0)
                        tmp.blue[i] = 0;
                }
                if (max > 255)
                    Process.multNumToModel(ref tmp, 255 / max);
            }

            public static double[] correct(double[] realisation)
            {
                double min = realisation.Min();
                double[] correct = new double[realisation.Length];

                for (int i = 0; i < realisation.Length; i++)
                    correct[i] = realisation[i] - min;
                return correct;
            }

            public static double disp(double[] realisation)
            {
                double sum = 0, mid = Analysis.average(realisation);
                for (int i = 0; i < realisation.Length; i++) sum += (realisation[i] - mid) * (realisation[i] - mid);
                return sum / realisation.Length;
            }

            public static double[] lightLevelMask(double[] channel, double[] mask)
            {
                if (mask == null || channel == null)
                    return null;

                long levelSize = 4294967295 / mask.Length;
                
                double[] res = channel;

                for (int i = 0; i < res.Length; i++)
                    res[i] = mask[Convert.ToInt32(res[i]) / levelSize];

                return res;
            }

            public static void sumNumToModel(ref IModel model, double num)
            {

                double[] rTmp =new double[model.red.Length];
                double[] gTmp =new double[model.green.Length];
                double[] bTmp = new double[model.blue.Length];

                model.red.CopyTo(rTmp, 0);
                model.green.CopyTo(gTmp, 0);
                model.blue.CopyTo(bTmp, 0);

                for (int i = 0; i < rTmp.Length; i++)
                {
                    rTmp[i] += num;
                    gTmp[i] += num;
                    bTmp[i] += num;

                }

                model.red = rTmp;
                model.green = gTmp;
                model.blue = bTmp;
            }

            public static void intensTransform(ref IModel picture, IModel trans, bool red, bool gre, bool blu)
            {
                normalizePicture(ref picture);

                double[] transCurve = trans.red;
                if (transCurve.Length < 256)
                    return;

                if (red)
                {
                    double[] chan = picture.red;

                    for (int i = 0; i < chan.Length; i++)
                        chan[i] = transCurve[Convert.ToInt32(chan[i])];

                    picture.red = chan;
                }
                if(gre)
                {
                    double[] chan = picture.green;

                    for (int i = 0; i < chan.Length; i++)
                        chan[i] = transCurve[Convert.ToInt32(chan[i])];

                    picture.green = chan;
                }
                if(blu)
                {
                    double[] chan = picture.blue;

                    for (int i = 0; i < chan.Length; i++)
                        chan[i] = transCurve[Convert.ToInt32(chan[i])];

                    picture.blue = chan;
                }
            }

            public static IModel sumModelToModel(IModel a, IModel b)
            {
                if (a.param["step_ctr"] != b.param["step_ctr"])
                    return null;

                double[] rTmp = new double[a.red.Length];
                double[] gTmp = new double[a.green.Length];
                double[] bTmp = new double[a.blue.Length];

                a.red.CopyTo(rTmp, 0);
                a.green.CopyTo(gTmp, 0);
                a.blue.CopyTo(bTmp, 0);


                for (int i = 0; i < rTmp.Length; i++)
                {
                    rTmp[i] += b.red[i];
                    gTmp[i] += b.blue[i];
                    bTmp[i] += b.green[i];

                }

                return Model.get_model(a.param, rTmp, gTmp, bTmp);
            }

            public static void multNumToModel(ref IModel model, double num)
            {
                double[] rTmp = new double[model.red.Length];
                double[] gTmp = new double[model.green.Length];
                double[] bTmp = new double[model.blue.Length];

                model.red.CopyTo(rTmp, 0);
                model.green.CopyTo(gTmp, 0);
                model.blue.CopyTo(bTmp, 0);

                multNumToRealisation(rTmp, num);
                multNumToRealisation(gTmp, num);
                multNumToRealisation(bTmp, num);

                model.red = rTmp;
                model.green = gTmp;
                model.blue = bTmp;
            }

            public static void multNumToRealisation(double[] real, double num)
            {
                for (int i = 0; i < real.Length; i++)
                {
                    real[i] *= num;
                }
            }

            public static void resizeImage(ref IModel cur, double scaleX, double scaleY, bool isBilinear)
            {
                if (scaleX <= 1 || scaleY <= 1)
                    smoothImage(ref cur);

                if (scaleX == 1 || scaleY == 1)
                    return;

                double oldW = cur.param["width"],
                    oldH = cur.param["height"],
                    newW = Convert.ToDouble(Math.Floor(oldW * scaleX)), // - ((isBilinear) ? 1.0 : 0.0),
                    newH = Convert.ToDouble(Math.Floor(oldH * scaleY)); // - ((isBilinear) ? 1.0 : 0.0);

                double[] oldR = cur.red,
                    oldG = cur.green,
                    oldB = cur.blue,
                    newR = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newG = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)],
                    newB = new double[Convert.ToInt32(newW) * Convert.ToInt32(newH)];

                if (isBilinear)
                {
                    for (int i = 0; i < newW; i++)
                    {
                        int x0 = Convert.ToInt32(Math.Floor(i / scaleX));
                        if (x0 == oldW - 1)
                            x0--;
                        int x1 = Convert.ToInt32(x0 + 1);

                        double x = (i / scaleX) % 1;
                        for (int j = 0; j < newH; j++)
                        {
                            int y0 = Convert.ToInt32(Math.Floor(j / scaleY));
                            if (y0 == oldH - 1)
                                y0--;
                            int y1 = Convert.ToInt32(y0 + 1);
                            double y = (j / scaleY) % 1;

                            newR[j + i * Convert.ToInt32(newH)] = bilinear(
                                oldR[y0 + x0 * Convert.ToInt32(oldH)],
                                oldR[y0 + x1 * Convert.ToInt32(oldH)],
                                oldR[y1 + x0 * Convert.ToInt32(oldH)],
                                oldR[y1 + x1 * Convert.ToInt32(oldH)],
                                x, y);
                            newG[j + i * Convert.ToInt32(newH)] = bilinear(
                                oldG[y0 + x0 * Convert.ToInt32(oldH)],
                                oldG[y0 + x1 * Convert.ToInt32(oldH)],
                                oldG[y1 + x0 * Convert.ToInt32(oldH)],
                                oldG[y1 + x1 * Convert.ToInt32(oldH)],
                                x, y);
                            newB[j + i * Convert.ToInt32(newH)] = bilinear(
                                oldB[y0 + x0 * Convert.ToInt32(oldH)],
                                oldB[y0 + x1 * Convert.ToInt32(oldH)],
                                oldB[y1 + x0 * Convert.ToInt32(oldH)],
                                oldB[y1 + x1 * Convert.ToInt32(oldH)],
                                x, y);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < newW; i++)
                    {
                        int colNeigth = Convert.ToInt32(Math.Floor(i / scaleX));
                        for (int j = 0; j < newH; j++)
                        {
                            int rowNeigth = Convert.ToInt32(Math.Floor(j / scaleY));
                            newR[j + i * Convert.ToInt32(newH)] = oldR[rowNeigth + colNeigth * Convert.ToInt32(oldH)];
                            newG[j + i * Convert.ToInt32(newH)] = oldG[rowNeigth + colNeigth * Convert.ToInt32(oldH)];
                            newB[j + i * Convert.ToInt32(newH)] = oldB[rowNeigth + colNeigth * Convert.ToInt32(oldH)];
                        }
                    }
                }

                cur.red = newR;
                cur.green = newG;
                cur.blue = newB;
                cur.param["width"] = newW;
                cur.param["height"] = newH;
            }

            public static double bilinear(double p00, double p10, double p01, double p11, double x, double y)
            {
                return p00 * (1 - x) * (1 - y) + p10 * x * (1 - y) + p01 * (1 - x) * y + p11 * x * y;
            }

            public static void smoothImage(ref IModel cur)
            {///////////
                int matrixSize = 2;

                if (matrixSize == 0)
                    return;
                double width = cur.param["width"],
                    height = cur.param["height"];

                double[] oldR = cur.red,
                    oldG = cur.green,
                    oldB = cur.blue,
                    newR = new double[(Convert.ToInt32(width)) * (Convert.ToInt32(height))],
                    newG = new double[(Convert.ToInt32(width)) * (Convert.ToInt32(height))],
                    newB = new double[(Convert.ToInt32(width)) * (Convert.ToInt32(height))];

                cur.red.CopyTo(newR, 0);
                cur.green.CopyTo(newG, 0);
                cur.blue.CopyTo(newB, 0);

                double[] weights = new double[matrixSize + 1];

                for (int i = 0; i < weights.Length; i++)
                {
                    double x = Convert.ToDouble(i) / weights.Length;
                    weights[i] = (-1.0) * (x * x) + 1;
                }


                for (int i = matrixSize; i < width - matrixSize; i++)
                {
                    for(int j = matrixSize; j < height - matrixSize; j++)
                    {
                        double Rsum = oldR[j + (i) * Convert.ToInt32(height)];
                        double Gsum = oldG[j + (i) * Convert.ToInt32(height)];
                        double Bsum = oldB[j + (i) * Convert.ToInt32(height)];

                        for(int k = 1; k <= matrixSize; k++)
                        {
                            Rsum +=
                                (
                                     oldR[j + (i + k) * Convert.ToInt32(height)] +               //6
                                     oldR[j + k + (i) * Convert.ToInt32(height)] +               //2
                                     oldR[j + (i - k) * Convert.ToInt32(height)] +               //4
                                     oldR[j - k + (i) * Convert.ToInt32(height)]                 //8
                                 ) / weights[k] +
                                 (
                                     oldR[j - k + (i - k) * Convert.ToInt32(height)] +            //7
                                     oldR[j - k + (i + k) * Convert.ToInt32(height)] +            //9
                                     oldR[j + k + (i - k) * Convert.ToInt32(height)] +            //1
                                     oldR[j + k + (i + k) * Convert.ToInt32(height)]              //3
                                ) / (weights[k] * 1.44);
                            Gsum +=
                                (
                                     oldG[j + (i + k) * Convert.ToInt32(height)] +               //6
                                     oldG[j + k + (i) * Convert.ToInt32(height)] +               //2
                                     oldG[j + (i - k) * Convert.ToInt32(height)] +               //4
                                     oldG[j - k + (i) * Convert.ToInt32(height)]                 //8
                                 ) / weights[k] +
                                 (
                                     oldG[j - k + (i - k) * Convert.ToInt32(height)] +            //7
                                     oldG[j - k + (i + k) * Convert.ToInt32(height)] +            //9
                                     oldG[j + k + (i - k) * Convert.ToInt32(height)] +            //1
                                     oldG[j + k + (i + k) * Convert.ToInt32(height)]              //3
                                ) / (weights[k] * 1.44);
                            Bsum +=
                                (
                                     oldB[j + (i + k) * Convert.ToInt32(height)] +               //6
                                     oldB[j + k + (i) * Convert.ToInt32(height)] +               //2
                                     oldB[j + (i - k) * Convert.ToInt32(height)] +               //4
                                     oldB[j - k + (i) * Convert.ToInt32(height)]                 //8
                                 ) / weights[k] +
                                 (
                                     oldB[j - k + (i - k) * Convert.ToInt32(height)] +            //7
                                     oldB[j - k + (i + k) * Convert.ToInt32(height)] +            //9
                                     oldB[j + k + (i - k) * Convert.ToInt32(height)] +            //1
                                     oldB[j + k + (i + k) * Convert.ToInt32(height)]              //3
                                ) / (weights[k] * 1.44);
                        }
                        
                        newR[j + i * Convert.ToInt32(height)] = Rsum / 17;
                        newG[j + i * Convert.ToInt32(height)] = Gsum / 17;
                        newB[j + i * Convert.ToInt32(height)] = Bsum / 17;
                    }
                }


                cur.red = newR;
                cur.green = newG;
                cur.blue = newB;
                //cur.param["width"] = width - 2 * matrixSize;
                //cur.param["height"] = height - 2 * matrixSize;

                normalizePicture(ref cur);
            }

            public static void convoluteImage(IModel cur, IModel matrix)
            {
                int matrixWidth = Convert.ToInt32(matrix.param["width"]),
                    matrixHeight = Convert.ToInt32(matrix.param["height"]),
                    oldWidth = Convert.ToInt32(cur.param["width"]),
                    oldHeight = Convert.ToInt32(cur.param["height"]),
                    width = Convert.ToInt32(cur.param["width"]) - matrixWidth + 1,
                    height = Convert.ToInt32(cur.param["height"]) - matrixHeight + 1;

                //Получаем байты изображения
                double[] inputR = cur.red;
                double[] inputG = cur.green;
                double[] inputB = cur.blue;
                double[] outputR = new double[width * height];
                double[] outputG = new double[width * height];
                double[] outputB = new double[width * height];

                //Производим вычисления
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        double rSum = 0, gSum = 0, bSum = 0;

                        for (int i = 0; i < matrixWidth; i++)
                        {
                            for (int j = 0; j < matrixHeight; j++)
                            {
                                int pixelPosX = x + i;
                                int pixelPosY = y + j;
                                if (pixelPosX >= oldWidth || pixelPosY >= oldHeight)
                                    continue;

                                double r = inputR[oldHeight * pixelPosX + pixelPosY]; 
                                double g = inputG[oldHeight * pixelPosX + pixelPosY];
                                double b = inputB[oldHeight * pixelPosX + pixelPosY];

                                double kernelVal = matrix.red[i * matrixHeight + j];

                                rSum += r * kernelVal;
                                gSum += g * kernelVal;
                                bSum += b * kernelVal;
                            }
                        }

                        //Записываем значения в результирующее изображение
                        double mSum = matrix.red.Sum();
                        outputR[height * x + y] = rSum / mSum;
                        outputG[height * x + y] = gSum / mSum;
                        outputB[height * x + y] = bSum / mSum;
                    }
                }
                //Записываем в cur отфильтрованное изображение

                cur.red = outputR;
                cur.green = outputG;
                cur.blue = outputB;
                cur.param["width"] = Convert.ToDouble(width);
                cur.param["height"] = Convert.ToDouble(height);
            }

            public static double[] convoluteLines(double[] first, double[] second)
            {
                double[] tmp = new double[first.Length + second.Length];

                for (int k = 0; k < first.Length + second.Length; k++)
                {
                    for (int j = 0; j < first.Length; j++)
                    {
                        if (j > k) continue;
                        if (k - j >= second.Length) continue;
                        tmp[k] += first[j] * second[k - j];
                    }

                }

                return tmp;
            }
        }

        class Model
        {
            class picture : IModel
            {
                private double[] red_;
                private double[] green_;
                private double[] blue_;
                public double[] red { set { red_ = value; d_param["step_ctr"] = value.Length; } get { return red_; } }
                public double[] green { set { green_ = value; d_param["step_ctr"] = value.Length; } get { return green_; } }
                public double[] blue { set { blue_ = value; d_param["step_ctr"] = value.Length; } get { return blue_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }
                
            }

            public static IModel fill_model(double[] red, double[] gre, double[] blu, Dictionary<string, double> param)
            {
                IModel tmp = new picture();
                tmp.param = param;
                //tmp.param = tmp.param.Concat(a.param);

                tmp.red = red;
                tmp.green = gre;
                tmp.blue = blu;

                return tmp;
            }

            public static IModel copy_model(IModel a)
            {
                IModel tmp = new picture();
                tmp.param = new Dictionary<string, double>(a.param);
                //tmp.param = tmp.param.Concat(a.param);

                tmp.red = new double[a.red.Length];
                a.red.CopyTo(tmp.red, 0);
                tmp.blue = new double[a.blue.Length];
                a.blue.CopyTo(tmp.blue, 0);
                tmp.green = new double[a.green.Length];
                a.green.CopyTo(tmp.green, 0);

                return tmp;
            }

            public static IModel get_model(Dictionary<string, double> param, double[] red, double[] gre, double[] blu)
            {
                picture cur = new picture();
                cur.param = param;
                cur.red = red;
                cur.green = gre;
                cur.blue = blu;
                return cur;
            }
        }

        private bool is_picture(IModel model)
        {
            if (!model.param.ContainsKey("width") || !model.param.ContainsKey("height"))
                return false;

            return true;
        }

        public string new_model(Dictionary<string, double> new_params, string name, double[] red, double[] gre, double[] blu)
        {
            int i = 0;
            string name_ = name;
            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            add_model(Model.get_model(new_params, red, gre, blu), name_);

            return name_;
        }

        public Dictionary<string, double> get_params(string name)
        {
            int index = models_names.IndexOf(name);
            if (index == -1)
                return null;
            return models[index].param;
        }

        public double[] get_red(string name)
        {
            return models[models_names.IndexOf(name)].red;
        }

        public double[] get_green(string name)
        {
            return models[models_names.IndexOf(name)].green;
        }

        public double[] get_blue(string name)
        {
            return models[models_names.IndexOf(name)].blue;
        }

        public void del_model(string name)
        {
            models.Remove(models[models_names.IndexOf(name)]);
            models_names.Remove(models_names[models_names.IndexOf(name)]);
            return;
        }

        IModel get_model(string name)
        {
            int index = models_names.IndexOf(name);
            return models[index];
        }

        public List<string> get_names()
        {
            return models_names;
        }

        public void sum_models(string first_name, string second_name, string new_name)
        {
            int i = 0;
            string name_ = new_name;
            while (models_names.Contains(name_))
            {
                name_ = new_name + i;
                i++;
            }

            IModel first = get_model(first_name), second = get_model(second_name);

            if (new_name != "")
            {
                models.Add(Process.sumModelToModel(first, second));
                models_names.Add(name_);
            }
            else
                first = Process.sumModelToModel(first, second);
        }

        public void aver_smooth(string name, int param, string new_name)
        {
            string name_;
            if (new_name == "")
            {
                int i = 0;
                name_ = name + "SMTHD";

                while (models_names.Contains(name_))
                {
                    name_ = name + "SMTHD" + i;
                    i++;
                }
            }
            else
            {
                int i = 0;
                name_ = new_name + "SMTHD";

                while (models_names.Contains(name_))
                {
                    name_ = new_name + "SMTHD" + i;
                    i++;
                }
            }

            add_model(Analysis.smoothing(Model.copy_model(get_model(name)), 0, param), name_);
        }

        void add_model(IModel model, string name)
        {
            int i = 0;
            string name_ = name;
            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }
            models_names.Add(name_);
            models.Add(model);
        }

        public void load_model(double[] data, string name, double step, Dictionary<string, double> _param = null)
        {
            int i = 0;
            string name_ = name;

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            Dictionary<string, double> param;
            if (_param == null) 
                param = new Dictionary<string, double>();
            else
                param = new Dictionary<string, double>(_param);

            param.Add("step_ctr", Convert.ToDouble(data.Length));
            param.Add("step", step);
            param.Add("height", Convert.ToDouble(data.Length));
            param.Add("width", 1);
            param.Add("start", 0.0);

            add_model(Model.fill_model(data, data, data, param), name_);

            return;
        }

        public void load_picture(double[] red, double[] gre, double[] blu, string name, double step, Dictionary<string, double> _param = null)
        {
            if (red.Length != gre.Length || gre.Length != blu.Length)
                return;

            int i = 0;
            string name_ = name;

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            Dictionary<string, double> param;
            if (_param == null)
                param = new Dictionary<string, double>();
            else
                param = new Dictionary<string, double>(_param);

            param.Add("step_ctr", Convert.ToDouble(red.Length));
            param.Add("step", step);
            param.Add("start", 0.0);

            add_model(Model.fill_model(red, gre, blu, param), name_);

            return;
        }

        public void suppressSlideWindow(string name, int window)
        {
            int i = 0;
            string name_ = name + "SUPWND";

            while (models_names.Contains(name_))
            {
                name_ = name + "SUPWND" + i;
                i++;
            }

            add_model(Process.suppressWindow(Model.copy_model(get_model(name)), window), name_);
        }

        public void renameModel(string oldName, string newName)
        {
            Predicate<string> is_equals = delegate (string val) { return val == oldName; };

            int i = models_names.FindIndex(is_equals);
            models_names[i] = newName;
            return;
        }

        public void lightLevelMask(string modelName, double[] mask, string newName = "")
        {
            if (mask == null)
                return;

            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            double[] rTmp = Process.lightLevelMask(cur.red, mask);
            double[] gTmp = Process.lightLevelMask(cur.green, mask);
            double[] bTmp = Process.lightLevelMask(cur.blue, mask);

            cur.red = rTmp;
            cur.green = gTmp;
            cur.blue = bTmp;

            if (newName != "")
                add_model(cur, newName);
        }

        public void sumNumToModel(string modelName, double number, string newName = "")
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            Process.sumNumToModel(ref cur, number);

            //double[] tmp = cur.result;
            //
            //for (int i = 0; i < tmp.Length; i++)
            //    tmp[i] += number;
            //
            //cur.result = tmp

            if (newName != "")
                add_model(cur, newName);
        }

        public void multModelToNum(string modelName, double number, string newName)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            Process.multNumToModel(ref cur, number);

            //double[] tmp = cur.result;
            //
            //for (int i = 0; i < tmp.Length; i++)
            //    tmp[i] *= number;
            //
            //cur.result = tmp;

            if (newName != "")
                add_model(cur, newName);
        }

        public void inversePicture(string modelName, string newName)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            Process.sumNumToModel(ref cur, -255.0);

            double[] rTmp = cur.red;
            double[] gTmp = cur.green;
            double[] bTmp = cur.blue;

            for (int i = 0; i < rTmp.Length; i++)
            {
                rTmp[i] = Math.Abs(rTmp[i]);
                gTmp[i] = Math.Abs(gTmp[i]);
                bTmp[i] = Math.Abs(bTmp[i]);
            }

            if (newName != "")
                add_model(cur, newName);
        }

        public void setParams(string name, Dictionary<string, double> param)
        {
            IModel cur = get_model(name);
            cur.param = param;
        }

        public void normalizePicture(string modelName)
        {
            IModel tmp = get_model(modelName);

            Process.normalizePicture(ref tmp);
        }

        public void intensTransform(string picName, string modelName, string newName, bool changeRed, bool changeGreen, bool changeBlue)
        {
            IModel picModel = (newName == "") ? get_model(picName) : Model.copy_model(get_model(picName));

            if (!is_picture(picModel))
                return;

            IModel transModel = get_model(modelName);

            Process.intensTransform(ref picModel, transModel, changeRed, changeGreen, changeBlue);

            Process.normalizePicture(ref picModel);

            if (newName != "")
                add_model(picModel, newName);
        }

        public void resizeImage(string modelName, string newName, double scaleX, double scaleY, bool isBilinear)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.resizeImage(ref cur, scaleX, scaleY, isBilinear);

            if (newName != "")
                add_model(cur, newName);
        }

        public void makeHysto(string modelName)
        {
            string newName = modelName + "HYSTO";

            IModel cur = Model.copy_model(get_model(modelName));

            Analysis.hystohramm(cur);

            add_model(cur, newName);
        }

        public void makeDistrib(string modelName)
        {
            string newName = modelName + "DISTRIB";

            IModel cur = Model.copy_model(get_model(modelName));

            Analysis.hystohramm(cur);

            Analysis.distribution(cur);

            Process.normalizePicture(ref cur);

            add_model(cur, newName);
        }

        public void noizeSnP(string modelName, string newName, double intense)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.saltPepperNoize(cur, intense);

            if (newName != "")
                add_model(cur, newName);
        }

        public void noizeRnd(string modelName, string newName, double intense)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.randomNoize(cur, intense);

            if (newName != "")
                add_model(cur, newName);
        }

        public void rotateImage(string modelName)
        {
            IModel cur = get_model(modelName);

            if (!is_picture(cur))
                return;

            Process.rotate(cur);

        }

        public void substractImages(string subFromName, string subWhatName, string newName)
        {
            if(newName == "") newName = subWhatName + "_SUBED_" + subFromName;

            IModel fromModel = Model.copy_model(get_model(subFromName));
            IModel whatModel = get_model(subWhatName);

            if (!is_picture(fromModel) || !is_picture(whatModel))
                return;

            Process.subImages(fromModel, whatModel);

            add_model(fromModel, newName);
        }

        public void medianFilterImage(string modelName, string newName, int window)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.medianFilter(cur, window);

            if (newName != "")
                add_model(cur, newName);
        }

        public void averageFilterImage(string modelName, string newName, int window)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.averFilter(cur, window);

            if (newName != "")
                add_model(cur, newName);
        }

        public void greyScale(string modelName, string newName, int bits)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            double[] scale = Process.greyScale(bits);
            Dictionary<string, double> param = new Dictionary<string, double>();

            IModel tmp = Model.get_model(param, scale, scale, scale);

            Process.intensTransform(ref cur, tmp, true, true, true);

            if (newName != "")
                add_model(cur, newName);

        }

        public void furieTransform(string modelName, string newName, bool isBack = false)
        {
            if(newName == "")
                newName = modelName + ((isBack) ? "BACK" : "FURIE");

            IModel cur = Model.copy_model(get_model(modelName));

            if(!isBack)
                Process.furieTransform(cur);
            else
                Process.furieTransformBack(cur);

            add_model(cur, newName);
        }

        public void linesDelete(string modelName, string newName, int step)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.deleteLines(cur, step);

            if (newName != "")
                add_model(cur, newName);
        }

        public void convolute(string modelName, string maskName, string newName)
        {
            if (newName == "") newName = modelName + "_CONVED_" + maskName;

            IModel model = Model.copy_model(get_model(modelName));
            IModel mask = get_model(maskName);

            if (!is_picture(model) || !is_picture(mask))
                return;

            Process.convoluteImage(model, mask);

            add_model(model, newName);
        }

        public void resizeImageRediscret(string modelName, string newName, double scaleX, double scaleY)
        {
            IModel cur = Model.copy_model(get_model(modelName));

            if (!is_picture(cur))
                return;

            Process.resizeImageFurie(cur, scaleX, scaleY);

            add_model(cur, (newName == "") ? modelName + "REDISCRED" : newName);
        }

        public void filtreFurie(string modelName, string filterName, string newName, double alpha)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));
            IModel filter = get_model(filterName);

            if (!is_picture(cur))
                return;

            Process.filtreImageByFurie(cur, filter, alpha);

            if (newName != "")
                add_model(cur, newName);
        }
    }
}
