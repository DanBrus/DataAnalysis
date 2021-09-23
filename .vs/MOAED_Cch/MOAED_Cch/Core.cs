using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOAED_Cch
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
            double[] result { get; set; }
            Dictionary<string, double> param { get; set; }

            void calculate();
        }

        class IOModule
        {

        }

        class Analysis
        {
            public static IModel smoothing(IModel cur_mod, int type, int param = 0)
            {
                switch (type)
                {
                    case (0):   //Скользящее среднее
                        {
                            cur_mod.result = Process.aver_smooth(cur_mod.result, param);
                            break;
                        }
                }
                return cur_mod;
            }
            
        }

        class Process
        {
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
                double[] tmp = Process.aver_smooth(model.result, window);


                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = model.result[i] - tmp[i];

                model.result = tmp;
                return model;
            }

            static public IModel convolute(IModel first, IModel second)
            {
                IModel result = Model.copy_model(first);

                double[] tmp = new double[Convert.ToInt32(first.param["step_ctr"] + second.param["step_ctr"])];

                for(int k = 0; k < first.param["step_ctr"] + second.param["step_ctr"]; k++)
                {
                    for(int j = 0; j < first.param["step_ctr"]; j++)
                    {
                        if (j > k) continue;
                        if (k - j >= second.param["step_ctr"]) continue;
                        tmp[k] += first.result[j] * second.result[k - j];
                    }

                }

                result.result = tmp;

                return result;
            }

            static public Dictionary<string, double> statioanarity(IModel model)
            {
                double[] x_correct = correct(model.result);
                Dictionary<string, double> param = model.param;

                double[][] realisations = new double[10][];

                for (int i = 0; i < 10; i++)
                {
                    realisations[i] = new double[x_correct.Length / 10];
                    for (int j = 0; j < x_correct.Length / 10; j++)
                        realisations[i][j] = x_correct[i * x_correct.Length / 10 + j];

                }

                double[] sigma = new double[10];
                double[] average = new double[10];
                for (int i = 0; i < 10; i++)
                {
                    sigma[i] = Math.Sqrt(disp(realisations[i]));
                    average[i] = Process.average(realisations[i]);
                }


                double S_average = average.Max() - average.Min(), S_sigma = sigma.Max() - sigma.Min();
                double S_x = x_correct.Max() - x_correct.Min();

                double d_average = S_average / S_x * 100, d_sigma = S_sigma / S_x * 100;


                Dictionary<string, double> ret = new Dictionary<string, double>();
                ret.Add("d_average", d_average);
                ret.Add("d_D", d_sigma);

                return ret;
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
                double sum = 0, mid = average(realisation);
                for (int i = 0; i < realisation.Length; i++) sum += (realisation[i] - mid) * (realisation[i] - mid);
                return sum / realisation.Length;
            }

            public static double average(double[] realisation)
            {
                double sum = 0;
                for (int i = 0; i < realisation.Length; i++)
                    sum += realisation[i];
                return sum / realisation.Length;
            }

            public static void make_blowout(ref IModel model)
            {
                Random rand = new Random();
                double[] tmp = model.result;

                byte[] mask = new byte[Convert.ToInt32(model.param["step_ctr"])];
                rand.NextBytes(mask);

                int ind = 0;
                for(int i = 0; i < model.param["step_ctr"]; i++)
                    if (mask[ind] < mask[i])
                        ind = i;

                tmp[ind] = ++tmp[ind] * 2000 * rand.NextDouble();

                model.result = tmp;

                return;
            }

            public static void del_blowout(ref IModel model)
            {
                Random rand = new Random();
                double[] tmp = model.result;
                
                double aver = Process.average(tmp), lim = 3 * Math.Sqrt(Process.disp(tmp));

                for (int i = 0; i < model.param["step_ctr"]; i++)
                    if (Math.Abs(tmp[i] - aver) > lim)
                    {
                        double prev, next;

                        if (i == 0)
                            prev = tmp[2];
                        else
                            prev = tmp[i - 1];

                        if (i == model.param["step_ctr"] - 1)
                            next = tmp[Convert.ToInt32(model.param["step_ctr"] - 3)];
                        else
                            next = tmp[i + 1];

                        tmp[i] = (prev + next) / 2;
                    }
                
                return;
            }

            public static void unite_models(ref IModel first, IModel second)
            {
                double f_ctr = first.param["step_ctr"];
                double s_ctr = second.param["step_ctr"];

                double[] tmp = new double[Convert.ToInt32(f_ctr + s_ctr)];

                first.result.CopyTo(tmp, 0);
                second.result.CopyTo(tmp, Convert.ToInt32(f_ctr));

                first.result = tmp;
            }

            public static void calculate(IModel model)
            {
                model.calculate();
            }

            public static double[][] reproductionAndMutate(double[][] prevGenerations, int generations)
            {
                double[][] nextGenerations = new double[generations][];

                for (int i = 0; i < generations; i++)
                    nextGenerations[i] = new double[prevGenerations[0].Length];

                Random rnd = new Random();

                if (prevGenerations.Length == 1)
                {
                    for (int i = 0; i < prevGenerations[0].Length; i++)
                    {
                        for (int j = 0; j < generations; j++)
                        {
                            //nextGenerations[j][i] = (prevGenerations[rnd.Next(0, prevGenerations.Length)][i] + prevGenerations[rnd.Next(0, prevGenerations.Length)][i]) / 2.0;       //Генерируем ген нового поколения
                            nextGenerations[j][i] = (rnd.NextDouble() - 0.5) * (prevGenerations[0].Max() - prevGenerations[0].Min()) * 2;                                                                         //Мутируем ген нового поколения
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < prevGenerations[0].Length; i++)
                    {
                        for (int j = 0; j < generations; j++)
                        {
                            nextGenerations[j][i] = (prevGenerations[rnd.Next(0, prevGenerations.Length)][i] + prevGenerations[rnd.Next(0, prevGenerations.Length)][i]) / 2.0;       //Генерируем ген нового поколения
                            nextGenerations[j][i] += (rnd.NextDouble() > 0.8) ? (rnd.NextDouble() - 0.5) * nextGenerations[j][i] / 2 : 0;                                                                         //Мутируем ген нового поколения
                        }
                    }
                }

                return nextGenerations;
            }

            public static double[][] selection(double[][] allGenerations, double[] etalon, int generations)
            {
                double[][] luckyGenerations = new double[generations][];

                Random rnd = new Random();

                double[] fitness = new double[allGenerations.Length];

                for (int i = 0; i < allGenerations.Length; i++)
                    fitness[i] = Process.fitness(allGenerations[i], etalon);

                double[] lottery = new double[allGenerations.Length];

                lottery[0] = Math.Pow(fitness[0], 2);
                for(int i = 1; i < lottery.Length; i++)
                    lottery[i] = lottery[i - 1] + Math.Pow(fitness[i], 2);

                for (int i = 0; i < generations; i++)
                {
                    luckyGenerations[i] = new double[allGenerations[0].Length];
                    double lotteryTicket = rnd.NextDouble() * (lottery.Max() - lottery.Min()) + lottery.Min();
                    for(int luckyGye = 0; luckyGye < lottery.Length; luckyGye++)
                        if(lotteryTicket <= lottery[luckyGye])
                        {
                            allGenerations[luckyGye].CopyTo(luckyGenerations[i], 0);
                            break;
                        }
                }

                return luckyGenerations;
            }

            public static double fitness(double[] cur, double[] etalon)
            {
                double[] tmp = new double[cur.Length];

                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = cur[i] - etalon[i];

                double dis = disp(tmp), ma = Math.Pow(tmp.Max(), 2);
                return 1 / (disp(tmp) * Math.Pow(tmp.Max(), 2));
            }
        }

        class Model
        {
            class potterfilter : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; if (value != null) d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate()
                {
                    switch (Convert.ToInt32(param["filterType"]))
                    {
                        case 0:
                            {
                                result = Process.lppotter(param["f"], Convert.ToInt32(param["step_ctr"]), param["step"]);
                                return;
                            }
                        case 1:
                            {
                                double[] tmp1 = Process.lppotter(param["f"], Convert.ToInt32(param["step_ctr"]), param["step"]);

                                double[] res = new double[tmp1.Length];
                                for (int i = 0; i <= 2 * param["step_ctr"]; i++)
                                {
                                    if (i == param["step_ctr"])
                                        res[i] = 1.0 - tmp1[i];
                                    else
                                        res[i] = -tmp1[i];
                                }

                                result = res;
                                return;
                            }
                        case 2:
                            {
                                double[] tmp1 = Process.lppotter(param["f"] - param["S"] / 2, Convert.ToInt32(param["step_ctr"]), param["step"]);
                                double[] tmp2 = Process.lppotter(param["f"] + param["S"] / 2, Convert.ToInt32(param["step_ctr"]), param["step"]);

                                double[] res = new double[tmp1.Length];

                                for (int i = 0; i <= 2 * param["step_ctr"]; i++)
                                {
                                    res[i] = tmp2[i] - tmp1[i];
                                }

                                result = res;
                                return;
                            }
                        case 3:
                            {
                                double[] tmp1 = Process.lppotter(param["f"] - param["S"] / 2, Convert.ToInt32(param["step_ctr"]), param["step"]);
                                double[] tmp2 = Process.lppotter(param["f"] + param["S"] / 2, Convert.ToInt32(param["step_ctr"]), param["step"]);

                                double[] res = new double[tmp1.Length];

                                for (int i = 0; i <= 2 * param["step_ctr"]; i++)
                                {
                                    if (i == param["step_ctr"])
                                        res[i] = 1.0 - tmp2[i] + tmp1[i];
                                    else
                                        res[i] = tmp1[i] - tmp2[i];
                                }

                                result = res;
                                return;
                            }
                        default:
                            { break; }
                            return;
                    }
                }
            }

            class pulse : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; if(value != null) d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate()
                {
                    double[] tmp = new double[Convert.ToInt32(param["step_ctr"])];

                    for(int i = 0; i < tmp.Length; i++)
                    {
                        if((i * param["step"]) % param["T"] <= param["len"])
                            tmp[i] = param["max"];
                        else
                            tmp[i] = param["min"];
                    }
                    result = tmp;
                }
            }

            class furie : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate() {
                    double[] spec = new double[Convert.ToInt32(param["step_ctr"])];

                    for (int n = 0; n < param["step_ctr"]; n++)
                    {
                        double Re = 0, Im = 0;
                        for (int k = 0; k < param["step_ctr"]; k++)
                        {
                            Re += result[k] * Math.Cos(2 * Math.PI * n * k / param["step_ctr"]);
                            Im += result[k] * Math.Sin(2 * Math.PI * n * k / param["step_ctr"]);
                        }
                        Re /= param["step_ctr"];
                        Im /= param["step_ctr"];

                        spec[n] = Math.Sqrt(Re * Re + Im * Im);
                    }

                    double[] new_res = new double[Convert.ToInt32(param["step_ctr"] / 2)];

                    
                    for (int i = 0; i < Convert.ToInt32(param["step_ctr"] / 2); i++)
                        new_res[i] = spec[i];
                    result = new_res;

                    param["step"] = 1 / (param["step"] * param["step_ctr"] * 2);
                    param["step_ctr"] = result.Length;

                    return;
                }
            }

            class points : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate() { }
            }

            class harmony : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate()
                {
                    result_ = new double[Convert.ToInt32(param["step_ctr"])];

                    double k = param["step"], f = param["f"] * 2 * Math.PI, phase = param["phase"], A = param["A"];

                    for(int i = 0; i < Convert.ToInt32(param["step_ctr"]); i++)
                    {
                        result[i] = A * Math.Sin(i * k * f + phase);
                    }

                    return;
                }
            }

            class autocorrelation : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate()
                {
                    double[] new_result = new double[result.Length * 7 / 8];

                    //param["step"] = 1;
                    param["step_ctr"] = new_result.Length;
                    param["start"] = 0;

                    for (int i = 0; i < new_result.Length; i++)
                    {
                        for (int j = 0; j + i < result.Length; j++)
                            new_result[i] += result[j] * result[j + i];

                        new_result[i] /= result.Length;
                    }

                    result = new_result;
                }
            }

            class distribution : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                //result_ должны быть заполнены соответствующими изначальной модели.
                //step_ctr обозначает сглаживание графика (кол-во промежутков)
                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }

                public void calculate()
                {
                    double[] new_result = new double[Convert.ToInt32(param["step_ctr"])];

                    double min = result.Min(), max = result.Max(), step = (max - min) / param["step_ctr"];

                    param["start"] = min;
                    param["step"] = step;
                    

                    foreach(double x in result)
                    {
                        double control = (x - min) / step;
                        new_result[Convert.ToInt32(Math.Truncate((x - step / 300 - min) / step))]++;
                    }

                    for (int i = 0; i < new_result.Length; i++)
                        new_result[i] /= result.Length;
                    /*
                    for (int i = 0; i < param["step_ctr"]; i++)
                    {
                        new_result[i] = Math.Exp(Math.Pow(x - aver, 2.0) * (-1.0) / 2.0 / Math.Pow(sigm, 2.0)) / sigm / Math.Sqrt(2.0 * Math.PI);
                        x += step;
                    }
                    */
                    result = new_result;
                }
            }

            class random : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }


                public void calculate()
                {
                    result_ = new double[Convert.ToUInt32(d_param["step_ctr"])];
                    switch (Convert.ToInt32(d_param["rand_type"]))
                    {
                        case 0:
                            Random rnd0 = new Random();
                            int i0 = 0;
                            while (i0 < d_param["step_ctr"])
                            {
                                result_[i0] = d_param["min"] + (d_param["max"] - d_param["min"]) * rnd0.NextDouble();
                                i0++;
                            }
                            break;

                        case 1:
                            {
                                Random rnd2 = new Random();
                                int i1 = 2;
                                TimeSpan time = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
                                result[0] = time.TotalMilliseconds % (d_param["max"] - d_param["min"]);
                                result[1] = (Math.Exp(result[0]) * result[0]) % (d_param["max"] - d_param["min"]);
                                while (i1 < d_param["step_ctr"])
                                {
                                    result[i1] = (result[i1 - 1] + result[i1 - 2]) % (d_param["max"] - d_param["min"]);
                                    i1++;
                                }
                                i1 = 0;
                                while (i1 < d_param["step_ctr"])
                                {
                                    result[i1] = d_param["min"] + result[i1];
                                    i1++;
                                }
                                break;
                            }
                        case 2:
                            {
                                Random rnd2 = new Random();
                                int i1 = 2;
                                TimeSpan time = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
                                result[0] = time.TotalMilliseconds % (d_param["max"] - d_param["min"]);
                                result[1] = Math.Exp(result[0]) % (d_param["max"] - d_param["min"]);
                                while (i1 < d_param["step_ctr"])
                                {
                                    result[i1] = (result[i1 - 1] + result[i1 - 2]) % (d_param["max"] - d_param["min"]);
                                    i1++;
                                }
                                i1 = 0;
                                while (i1 < d_param["step_ctr"])
                                {
                                    result[i1] = d_param["min"] + result[i1];
                                    i1++;
                                }
                                break;
                            }
                    }
                }
            }

            class linear : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }


                public void calculate()
                {
                    result = new double[Convert.ToUInt32(d_param["step_ctr"])];
                    for (int i = 0; i < d_param["step_ctr"]; i++)
                        result[i] = (d_param["start"] + d_param["step"] * i) * d_param["c"] + d_param["d"];

                }
            }

            class exponent : IModel
            {
                private double[] result_;
                public double[] result { set { result_ = value; d_param["step_ctr"] = value.Length; } get { return result_; } }

                Dictionary<string, double> d_param;
                public Dictionary<string, double> param { get { return d_param; } set { d_param = value; } }


                public void calculate()
                {
                    result_ = new double[Convert.ToUInt32(d_param["step_ctr"])];
                    for (int i = 0; i < d_param["step_ctr"]; i++)
                        result_[i] = d_param["b"] * Math.Exp(d_param["start"] + d_param["step"] * i * d_param["a"]);
                }

            }

            public static IModel fill_model(double[] data, Dictionary<string, double> param)
            {
                IModel tmp = new points();
                tmp.param = param;
                //tmp.param = tmp.param.Concat(a.param);

                tmp.result = data;

                return tmp;
            }

            public static IModel copy_model(IModel a)
            {
                IModel tmp = new points();
                tmp.param = new Dictionary<string, double>(a.param);
                //tmp.param = tmp.param.Concat(a.param);

                tmp.result = new double[a.result.Length];
                a.result.CopyTo(tmp.result, 0);

                return tmp;
            }

            public static IModel get_model(int type, Dictionary<string, double> param, double[] result = null)
            {
                switch (type)
                {
                    case 0:
                        linear cur_lin = new linear();
                        cur_lin.param = param;
                        return cur_lin;

                    case 1:
                        exponent cur_exp = new exponent();
                        cur_exp.param = param;
                        return cur_exp;

                    case 2:
                        random cur_rand = new random();
                        cur_rand.param = param;
                        return cur_rand;
                    case 3:
                        distribution cur_distrib = new distribution();
                        cur_distrib.param = param;
                        cur_distrib.result = result;
                        return cur_distrib;
                    case 4:
                        autocorrelation cur_autocorr = new autocorrelation();
                        cur_autocorr.param = param;
                        cur_autocorr.result = result;
                        return cur_autocorr;
                    case 5:
                        harmony cur_harm = new harmony();
                        cur_harm.param = param;
                        return cur_harm;
                    case 6:
                        furie cur_furie = new furie();
                        cur_furie.param = param;
                        cur_furie.result = result;
                        return cur_furie;
                    case 7:
                        pulse cur_pulse = new pulse();
                        cur_pulse.param = param;
                        cur_pulse.result = result;
                        return cur_pulse;
                    case 8:
                        potterfilter cur_filter = new potterfilter();
                        cur_filter.param = param;
                        cur_filter.result = result;
                        return cur_filter;
                    default:
                        return null;
                }
            }
        }

        public string distrb(string name, int steps = 0){
            int i = 0;
            string name_ = name + "Distrib";

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            double[] res = get_result(name);
            Dictionary<string, double> param = new Dictionary<string, double>(get_params(name));

            if (steps != 0)
                param["step_ctr"] = steps;

            models_names.Add(name_);
            models.Add(Model.get_model(3, param, res));

            Process.calculate(models.Last());
            
            return name_;
        }

        public string autocorr(string name)
        {
            int i = 0;
            string name_ = name + "Autocorr";

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            double[] res = get_result(name);
            Dictionary<string, double> param = new Dictionary<string, double>(get_params(name));

            models_names.Add(name_);
            models.Add(Model.get_model(4, param, res));

            Process.calculate(models.Last());

            return name_;
        }

        public void intercorr(string first_name, string second_name, string name)
        {
            int i = 0;
            string name_ = name;

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            IModel first, second;
            if (get_model(first_name).param["step_ctr"] < get_model(first_name).param["step_ctr"])
            {
                first = Model.copy_model(get_model(first_name));
                second = get_model(second_name);
            }
            else
            {
                first = Model.copy_model(get_model(second_name));
                second = get_model(first_name);
            }

            double[] tmp = new double[Convert.ToInt32(first.param["step_ctr"])];
            
            for (i = 0; i < first.param["step_ctr"]; i++)
            {
                int j = 0;
                for (j = 0; (j < first.param["step_ctr"]) && (j + i < second.param["step_ctr"]); j++)
                    tmp[i] += first.result[j] * second.result[j + i];

                tmp[i] /= j;
            }

            first.result = tmp;

            models_names.Add(name_);
            models.Add(first);
        }

        public string new_model(Dictionary<string, double> new_params, string name)
        {
            int i = 0;
            string name_ = name;
            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            models_names.Add(name_);
            models.Add(Model.get_model(Convert.ToInt32(new_params["type"]), new_params));

            Process.calculate(models.Last());
            return name_;
        }

        public Dictionary<string, double> get_params(string name)
        {
            int index = models_names.IndexOf(name);
            return models[index].param;
        }

        public double[] get_result(string name)
        {
            return models[models_names.IndexOf(name)].result;
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

            IModel first, second;
            if (get_model(first_name).param["step_ctr"] > get_model(first_name).param["step_ctr"])
            {
                first = Model.copy_model(get_model(first_name));
                second = get_model(second_name);
            }
            else
            {
                first = Model.copy_model(get_model(second_name));
                second = get_model(first_name);
            }

            double[] tmp = new double[Convert.ToInt32(first.param["step_ctr"])];
            for (int j = 0; j < first.param["step_ctr"]; j++)
                tmp[j] = first.result[j] + second.result[j % Convert.ToInt32(second.param["step_ctr"])];

            first.result = tmp;

            models_names.Add(name_);
            models.Add(first);
        }

        public void mult_models(string first_name, string second_name, string new_name)
        {
            int i = 0;
            string name_ = new_name;
            while (models_names.Contains(name_))
            {
                name_ = new_name + i;
                i++;
            }

            IModel first, second;
            if (get_model(first_name).param["step_ctr"] > get_model(first_name).param["step_ctr"])
            {
                first = Model.copy_model(get_model(first_name));
                second = get_model(second_name);
            }
            else
            {
                first = Model.copy_model(get_model(second_name));
                second = get_model(first_name);
            }

            double[] tmp = new double[Convert.ToInt32(first.param["step_ctr"])];
            for (int j = 0; j < first.param["step_ctr"]; j++)
                tmp[j] = first.result[j] * second.result[j % Convert.ToInt32(second.param["step_ctr"])];

            first.result = tmp;

            models_names.Add(name_);
            models.Add(first);
        }

        public Dictionary<string, double> statioanarity(string name)
        {
            int index = models_names.IndexOf(name);
            return Process.statioanarity(models[index]);
        }

        public void make_blowout(string name)
        {
            IModel tmp = get_model(name);
            Process.make_blowout(ref tmp);
            return;
        }

        public void del_blowout(string name)
        {
            IModel tmp = get_model(name);
            Process.del_blowout(ref tmp);
            return;
        }

        public void get_furie(string name)
        {
            int i = 0;
            string name_ = name + "Furie";

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            double[] res = get_result(name);
            Dictionary<string, double> param = new Dictionary<string, double>(get_params(name));

            models_names.Add(name_);
            models.Add(Model.get_model(6, param, res));
            Process.calculate(models.Last());
            
        }

        public void to_null(string name)
        {
            IModel tmp = get_model(name);
            double aver = Process.average(tmp.result);
            for (int i = 0; i < tmp.param["step_ctr"]; i++)
                tmp.result[i] -= aver;
            return;
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

        public void unite_models(string first_name, string second_name, string new_name)
        {
            int i = 0;
            string name_ = new_name;

            while (models_names.Contains(name_))
            {
                name_ = new_name + i;
                i++;
            }

            IModel first = Model.copy_model(get_model(first_name)),
                second = get_model(second_name);

            Process.unite_models(ref first, second);
            add_model(first, name_);
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

        public void convolute(string first, string second, string new_name)
        {
            int i = 0;
            string name_ = new_name;

            while (models_names.Contains(name_))
            {
                name_ = new_name + i;
                i++;
            }

            add_model(Process.convolute(get_model(first), get_model(second)), name_);
        }

        public void load_model(double[] data, string name, double step)
        {
            int i = 0;
            string name_ = name;

            while (models_names.Contains(name_))
            {
                name_ = name + i;
                i++;
            }

            Dictionary<string, double> param = new Dictionary<string, double>();
            param.Add("step_ctr", Convert.ToDouble(data.Length));
            param.Add("step", step);
            param.Add("start", 0.0);

            add_model(Model.fill_model(data, param), name_);

            return;
        }

        public double[] snr_calculate(string name, int steps)
        {
            double[] res = new double[steps];

            IModel model = Model.copy_model(get_model(name));
            double[] harm = model.result;
            double[][] rp = new double[Convert.ToInt32(model.param["step_ctr"])][];

            double A = (harm.Max() - harm.Min()) * 100;

            Random rand = new Random();
            
            for (int i = 0; i < steps; i++)
            {
                double[] tmp = new double[Convert.ToInt32(model.param["step_ctr"])];
                rp[i] = new double[Convert.ToInt32(model.param["step_ctr"])];

                for (int j = 0; j < rp.Length; j++)
                    rp[i][j] = A * (rand.NextDouble() - 0.5);

                for (int k = 0; k <= i; k++) {
                    for (int j = 0; j < Convert.ToInt32(model.param["step_ctr"]); j++)
                    {
                        tmp[j] += rp[k][j] / i;
                    }
                }

                for (int j = 0; j < Convert.ToInt32(model.param["step_ctr"]); j++)
                {
                    tmp[j] += harm[j];
                }

                if (i == 1)
                    add_model(Model.fill_model(tmp, model.param), "tmp1");
                if (i == 9)
                    add_model(Model.fill_model(tmp, model.param), "tmp10");
                if (i == 49)
                    add_model(Model.fill_model(tmp, model.param), "tmp50");
                if (i == 99)
                    add_model(Model.fill_model(tmp, model.param), "tmp100");

                res[i] = Math.Sqrt(Process.disp(tmp));
            }

           

            Dictionary<string, double> param = new Dictionary<string, double>();
            param.Add("step_ctr", Convert.ToDouble(res.Length));
            param.Add("step", 1);
            param.Add("start", 0.0);

            add_model(Model.fill_model(res, param), name+"SNR");

            return res;
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

        public void sumNumToModel(string modelName, double number, string newName)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            double[] tmp = cur.result;

            for (int i = 0; i < tmp.Length; i++)
                tmp[i] += number;

            cur.result = tmp;

            if (newName != "")
                add_model(cur, newName);
        }

        public void multModelToNum(string modelName, double number, string newName)
        {
            IModel cur = (newName == "") ? get_model(modelName) : Model.copy_model(get_model(modelName));

            double[] tmp = cur.result;

            for (int i = 0; i < tmp.Length; i++)
                tmp[i] *= number;

            cur.result = tmp;

            if (newName != "")
                add_model(cur, newName);
        }

        public void setParams(string name, Dictionary<string, double> param)
        {
            IModel cur = get_model(name);
            cur.param = param;
        }

        public void modulation(string originalName, string modulationName, string resultName, double coefficient)
        {
            int i = 0;
            string name_ = resultName;

            while (models_names.Contains(name_))
            {
                name_ = resultName + i;
                i++;
            }

            IModel modulationModel = get_model(modulationName), resultModel = Model.copy_model(get_model(originalName));

            double origShift = - resultModel.result.Min();

            double[] tmp = resultModel.result;

            for (int j = 0; j < tmp.Length; j++)
            {
                tmp[j] *= 0.01 * coefficient;
                tmp[j] += origShift;
            }

            for (int j = 0; j < resultModel.param["step_ctr"]; j++)
                tmp[j] *= modulationModel.result[j % Convert.ToInt32(modulationModel.param["step_ctr"])];

            resultModel.result = tmp;

            add_model(resultModel, name_);
        }

        public void demodulation(string originalName, string modulationName, string resultName, double coefficient)
        {
            int i = 0;
            string name_ = resultName;

            while (models_names.Contains(name_))
            {
                name_ = resultName + i;
                i++;
            }

            IModel modulationModel = get_model(modulationName), resultModel = Model.copy_model(get_model(originalName));
            
            double moduleShift = -modulationModel.result.Min();

            double[] tmp = resultModel.result;

            for (int j = 0; j < resultModel.param["step_ctr"]; j++)
            {
                
                tmp[j] /= modulationModel.result[j % Convert.ToInt32(modulationModel.param["step_ctr"])] + moduleShift + 1;
            }


            resultModel.result = tmp;

            add_model(resultModel, name_);

            to_null(name_);

            multModelToNum(name_, 1 / 0.01 * coefficient, "");
            
        }

        public void absModel(string name, string new_name) {
            IModel cur = (new_name != "") ? Model.copy_model(get_model(name)) : get_model(name);

            double[] tmp = cur.result;
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = Math.Abs(tmp[i]);

            cur.result = tmp;

            if (new_name != "")
                add_model(cur, new_name);

            return;
        }

        public void cutModel(string name, string new_name, double from, double to)
        {
            if (to < from)
                return;
            IModel cur = (new_name != "") ? Model.copy_model(get_model(name)) : get_model(name);

            int start = (from < cur.param["start"]) ? 0 : Convert.ToInt32((from - cur.param["start"]) / cur.param["step"]);
            int end = (to > cur.param["start"] + cur.param["step"] * cur.result.Length) ? cur.result.Length : Convert.ToInt32((to - cur.param["start"]) / cur.param["step"]);

            double[] tmp = new double[end - start];

            for (int i = start; i < end; i++)
                tmp[i - start] = cur.result[i];

            cur.result = tmp;

            if (new_name != "")
                add_model(cur, new_name);

            return;
        }

        public void snrModulation(string originalName, string modulationName)
        {
            IModel original = Model.copy_model(get_model(originalName));

            IModel modulationModel = get_model(modulationName), modulatedModel = Model.copy_model(get_model(originalName));

            double origShift = -modulatedModel.result.Min();

            double[] tmp = modulatedModel.result;

            for (int j = 0; j < tmp.Length; j++)
            {
                tmp[j] += origShift;
            }

            for (int j = 0; j < modulatedModel.param["step_ctr"]; j++)
                tmp[j] *= modulationModel.result[j % Convert.ToInt32(modulationModel.param["step_ctr"])];

            modulatedModel.result = tmp;

            double[] rand = new double[Convert.ToInt32(modulatedModel.param["step_ctr"])];
            Random rnd0 = new Random();

            double dispMod = Process.disp(modulatedModel.result);

            IModel etalonModel = null;
            double step_i = (modulatedModel.result.Max() - modulatedModel.result.Min()) * 2;
            double[] outputOsrMod = new double[100];
            double[] outputOsrRes = new double[100];
            for (int i = 0; i <= 100; i++)
            {
                IModel cur = Model.copy_model(modulatedModel);
                tmp = cur.result;

                for (int j = 0; j < rand.Length; j++)
                {
                    rand[j] = (rnd0.NextDouble() - 0.5) * Convert.ToDouble(i) / 100.0 * step_i;
                }

                for (int j = 0; j < tmp.Length; j++)
                {
                    tmp[j] = Math.Abs(tmp[j] + rand[j]);
                }

                double osrMod = 20.0 * Math.Log(Math.Sqrt(dispMod) / Math.Sqrt(Process.disp(rand)));
                outputOsrMod[i] = osrMod;
                double[] smoothedTmp = Process.aver_smooth(tmp, 200);

                double aver = Process.average(smoothedTmp);
                for (int j = 0; j < smoothedTmp.Length; j++)
                    smoothedTmp[j] -= aver;

                cur.result = tmp;

                if (i == 0)
                {
                    etalonModel = Model.copy_model(cur);
                    continue;
                }
                else
                {
                    double[] res_noize = new double[cur.result.Length];
                    cur.result.CopyTo(res_noize, 0);

                    for (int j = 0; j < res_noize.Length; j++)
                        res_noize[j] -= etalonModel.result[j];

                    double osrRes = 20.0 * Math.Log(Math.Sqrt(dispMod) / Math.Sqrt(Process.disp(res_noize)));
                    outputOsrRes[i] = osrRes;
                }

                if (i % 10 == 0)
                {
                    add_model(Model.get_model(6, cur.param, cur.result), "Furie" + i * step_i);
                    Process.calculate(models.Last());
                }
            }
            Dictionary<string, double> header = new Dictionary<string, double>();
            header.Add("step", step_i);
            header.Add("step_ctr", outputOsrRes.Length);
            header.Add("start", 0);
            add_model(Model.fill_model(outputOsrMod, header), "SNR of modulated signal");
            add_model(Model.fill_model(outputOsrRes, header), "SNR of result signal");
        }

        public void evolution(string originalName, string etalonName, string evoName, int evoSteps)
        {
            double[] fitnessOfTime = new double[evoSteps];
            double[] averFitnessOfTime = new double[evoSteps];
            IModel original = get_model(originalName), etalon = get_model(etalonName);
            fitnessOfTime[0] = Process.fitness(original.result, etalon.result);
            averFitnessOfTime[0] = Process.fitness(original.result, etalon.result);

            Dictionary<string, double> tmpHeader = new Dictionary<string, double>();
            tmpHeader.Add("step_ctr", 15.0);
            tmpHeader.Add("step", 1.0);
            tmpHeader.Add("start", 0.0);

            Dictionary<string, double> breakHeader = new Dictionary<string, double>(original.param);

            int nextBreakpointNum = 1;
            double[][] curGeneration = new double[1][];
            curGeneration[0] = new double[original.result.Length];
            original.result.CopyTo(curGeneration[0], 0);

            for (int i = 1; i < evoSteps; i++)
            {
                curGeneration = Process.reproductionAndMutate(curGeneration, 200);
                curGeneration = Process.selection(curGeneration, etalon.result, 50);

                double[] curFitness = new double[curGeneration.Length];
                for (int j = 0; j < curGeneration.Length; j++)
                    curFitness[j] = Process.fitness(curGeneration[j], etalon.result);

                fitnessOfTime[i] = curFitness.Max();
                averFitnessOfTime[i] = curFitness.Average();

                if(i == nextBreakpointNum * (evoSteps / 5))
                {
                    int maxIndex = 0;
                    foreach(double val in curFitness)
                    {
                        if (val == curFitness.Max())
                            break;
                        else
                            maxIndex++;
                    }

                    add_model(Model.fill_model(curGeneration[maxIndex], breakHeader), evoName + "STEP" + i);
                    add_model(Model.fill_model(curFitness, tmpHeader), evoName + "AverFitnessSTEP" + i);
                    nextBreakpointNum++;

                }
            }

            Dictionary<string, double> resHeader = new Dictionary<string, double>();
            resHeader.Add("step_ctr", Convert.ToDouble(evoSteps));
            resHeader.Add("step", 1.0);
            resHeader.Add("start", 0.0);

            add_model(Model.fill_model(fitnessOfTime, resHeader), evoName + "BestFitness");
            add_model(Model.fill_model(averFitnessOfTime, resHeader), evoName + "AverFitness");
        }
    }
}
