using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using System.IO;
using System.Data.OleDb;

using Graph;
using StochasticValues;
using EquationSolve;
using Simplex;

namespace RTS
{
    /// <summary>
    /// Модель подразделения железнодорожного транспорта (ПЖДТ)
    /// </summary>
    class TSModel : Graph.Graph
    {
        public Dictionary<Node, int> LocoNum, GearNum;
        public Dictionary<Link, double> LinksLoad;

        public Dictionary<Node, double> EQ, ET;
        public Dictionary<Node, double> TotalTW, TotalTL, TotalTG;
        
        public Stochastic sI = new Stochastic(dl: 2, scl: 0.3);
        public Stochastic sQ = new Stochastic(dl: 1, loc: 3, scl: 0.6);

        /// <summary>
        /// мощность локомотива, ваг.
        /// </summary>
        private int qw = 10;
        /// <summary>
        /// матожидание продолжительности обслуживания одного вагона на грузовом фронте, ч
        /// </summary>
        private double mt = 0.3;
        /// <summary>
        /// матожидание продолжительности обслуживания одного вагона при доставке на грузовой фронт, ч
        /// </summary>
        private double mw = 0.1;
        /// <summary>
        /// удельные затраты на простой вагона в загруженном и порожнем состоянии соответственно, у.е./ч
        /// </summary>
        private double cwl = 50, cwu = 10;
        /// <summary>
        /// удельная стоимость работы ПРМ, у.е./ч 
        /// </summary>
        double cmc = 80;
        /// <summary>
        /// средневзвешенные удельные затраты на работу локомотивов
        /// при ожидании прибытия вагонов и перемещение подач вагонов соответственно, у.е./ч 
        /// </summary>
        double clc = 60, clv = 120;
        /// <summary>
        /// балансовая стоимость локомотивов и ПРМ, у.е.
        /// </summary>
        double BL = 10000, BG = 2000;

        /// <summary>
        /// Конструктор класса ПЖДТ
        /// </summary>
        /// <param name="dbFile">имя accdb-файла с информацией о ПЖДТ</param>
        public TSModel(string dbFile)
        {
            LoadFromDB(dbFile);

            this.LocoNum = new Dictionary<Node, int>();
            this.GearNum = new Dictionary<Node, int>();
            this.LinksLoad = new Dictionary<Link, double>();

            loadFacilitiesNum(dbFile);

            this.EQ = new Dictionary<Node, double>();
            this.ET = new Dictionary<Node, double>();
            this.TotalTW = new Dictionary<Node, double>();
            this.TotalTL = new Dictionary<Node, double>();
            this.TotalTG = new Dictionary<Node, double>();

            DefineLinksWeight();
            this.SetFlows(this.sI);
            this.DefineLinksLoad();
        }

        private void loadFacilitiesNum(string dbFile)
        {
            OleDbConnection сonnection = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + dbFile);
            OleDbCommand command = сonnection.CreateCommand();

            сonnection.Open();

            command.CommandText = "SELECT Code, LN, GN FROM Nodes";
            OleDbDataReader nodesReader = command.ExecuteReader();
            while (nodesReader.Read())
            {
                this.LocoNum[Nodes[Convert.ToInt32(nodesReader["Code"])-1]] = Convert.ToInt32(nodesReader["LN"]);
                this.GearNum[Nodes[Convert.ToInt32(nodesReader["Code"])-1]] = Convert.ToInt32(nodesReader["GN"]);
            }
            nodesReader.Close();
                        
            сonnection.Close();
        }

        /// <summary>
        /// Расчет оптимального количества локомотивов станции
        /// </summary>
        /// <param name="node">ссылка на вершину графа</param>
        /// <returns>Оптимальное количество станционных локомотивов</returns>
        private int defLocoNum(Node node)
        {
            double eq = this.EQ[node], et = this.ET[node];
            
            // стартовое количество ПРМ
            int gn = 5;
                 
            // коэффициенты уравнения для определения оптимального количества локомотивов
            double k0, k1, k2, k3, k4;
            k0 = 24 * clc;
            k1 = 60 * (clv - clc) * Math.Pow(eq, 0.884) * Math.Pow(et, -0.885);
            k2 = -932 * cwl * Math.Pow(eq, 2.527) * Math.Pow(et, -1.979);
            k3 = -9.5 * cwu * Math.Pow(eq, 0.978) * Math.Pow(et, -0.865);
            k4 = 14855 * cwl * Math.Pow(mt, 1.129) * Math.Pow(eq, 2.982) * Math.Pow(et, -2.698) * Math.Pow(gn, -2.368);

            Solver solver = new Solver();
            solver.LowBound = 1;
            solver.HighBound = 100;
            solver.Accuracy = 0.001;
            solver.FuncCoefs = new double[5] {k0, k1, k2, k3, k4 };
            solver.VarPows = new double[5] {0, -0.336, -1.429, -1.054, -0.695 };

            // количество локомотивов в первом приближении
            int ln = (int)solver.Solve();
            // уточнение оптимального количества ПРМ
            gn = defGearNum(eq, et, ln);
            // пересчет коэффициента уравнения
            solver.FuncCoefs[4] = 14885 * cwl * Math.Pow(mt, 1.129) *
                                    Math.Pow(eq, 2.982) * Math.Pow(et, -2.698) * Math.Pow(gn, -2.368);
            ln = (int)solver.Solve();
            
            return (ln <= 1) ? 1 : ln;
        }

        /// <summary>
        /// Расчет оптимального количества ПРМ
        /// </summary>
        /// <param name="eq">отношение количества вагонов поезда к мощности локомотива</param>
        /// <param name="et">отношение интервала поступления поезда к продолжительности операции локомотива</param>
        /// <param name="ln">количество станционных локомотивов</param>
        /// <returns>Оптимальное количество ПРМ</returns>
        private int defGearNum(double eq, double et, int ln)
        {
            int gn = (int)(12.36 * Math.Pow(ln, 0.091) * Math.Pow(this.mt, 0.335) * Math.Pow(eq, 0.885) *
                    Math.Pow(this.cwl / this.cmc, 0.297) / Math.Pow(et, 0.801));
            return (gn <= 1) ? 1 : gn;
        }

        /// <summary>
        /// Определение суммарного времени обслуживания вагонов на станции
        /// </summary>
        /// <param name="node">ссылка на вершину графа</param>
        /// <returns>суммарное времени обслуживания вагонов для станции node</returns>
        private double defTotalTW(Node node)
        {
            return 2172 * Math.Pow(EQ[node], 2.527) * Math.Pow(LocoNum[node], -0.429) *
                    Math.Pow(ET[node], -1.979) + 104 * Math.Pow(EQ[node], 1.054) * Math.Pow(ET[node], -0.961) + 
                    48704 * Math.Pow(LocoNum[node], 0.305) * Math.Pow(EQ[node], 2.982) * Math.Pow(this.mt, 1.129) *
                    Math.Pow(ET[node], -2.698) * Math.Pow(GearNum[node], -2.368) + 123 * this.mt;
        }

        /// <summary>
        /// Определение суммарного времени работы станционных локомотивов
        /// </summary>
        /// <param name="node">ссылка на вершину графа</param>
        /// <returns>суммарное время работы локомотивов для станции node</returns>
        private double defTotalTL(Node node)
        {
            double tl = 90 * Math.Pow(this.LocoNum[node], 0.664) * Math.Pow(this.ET[node], -0.885);
            return (tl < 24 * this.LocoNum[node]) ? 24 * this.LocoNum[node] : tl;
        }

        /// <summary>
        /// Определение суммарного времени работы ПРМ
        /// </summary>
        /// <param name="node">ссылка на вершину графа</param>
        /// <returns>суммарное время работы ПРМ для станции node</returns>
        private double defTotalTG(Node node)
        {
            return (123 * this.mt < 24 * this.GearNum[node]) ? 24 * this.GearNum[node] : 123 * this.mt;
        }

        /// <summary>
        /// Имитация процесса функционирования транспортной сети
        /// </summary>
        public void SimulateNet()
        {
            this.SetFlows(this.sI);
            this.DefineLinksLoad();

            foreach (Node node in this.Nodes)
            {
                EQ[node] = this.sQ.GetValue() / this.qw;
                ET[node] = 1 / node.InFlowsTotalIntencity / this.mw;
                LocoNum[node] = defLocoNum(node);
                GearNum[node] = defGearNum(EQ[node], ET[node], LocoNum[node]);
                TotalTW[node] = defTotalTW(node);
                TotalTL[node] = defTotalTL(node);
                TotalTG[node] = defTotalTG(node);
            }

            foreach (Link link in this.Links) this.LinksLoad[link] = link.Load;
        }

        /// <summary>
        /// Определение коэффициентов целевой функции для решения задачи устойчивого развития ПЖДТ
        /// на основании симплекс-метода
        /// </summary>
        /// <returns>массив, содержащий коэффициенты целевой функции</returns>
        private double[] simplexCoeffs()
        {
            double[] sc = new double[4];

            Dictionary<Node, int> bLN = new Dictionary<Node, int>();
            Dictionary<Node, int> bGN = new Dictionary<Node, int>();
            Dictionary<Link, double> bL = new Dictionary<Link, double>();
            Dictionary<Node, double> bTW = new Dictionary<Node, double>();
            Dictionary<Node, double> bTL = new Dictionary<Node, double>();
            Dictionary<Node, double> bTG = new Dictionary<Node, double>();

            int dLN = 0, dGN = 0;
            double dL = 0;
            double dTW = 0, dTL = 0, dTG = 0;

            SimulateNet();

            foreach (Node node in this.Nodes)
            {
                bLN[node] = this.LocoNum[node];
                bGN[node] = this.GearNum[node];
                bTW[node] = this.TotalTW[node];
                bTL[node] = this.TotalTL[node];
                bTG[node] = this.TotalTG[node];
            }
            foreach (Link link in this.Links) bL[link] = this.LinksLoad[link];

            SimulateNet();

            // определение прироста основных характеристик функционирования ПЖДТ
            foreach (Node node in this.Nodes)
            {
                dLN += bLN[node] - this.LocoNum[node];
                dGN += bGN[node] - this.GearNum[node];
                dTW += bTW[node] - this.TotalTW[node];
                dTL += bTL[node] - this.TotalTL[node];
                dTG += bTG[node] - this.TotalTG[node];
            }
            foreach (Link link in this.Links) 
                dL += (bL[link] - this.LinksLoad[link]) * link.Weight;

            // расчет величины капиталовложений
            double Kres = 0;
            if (dLN > 0) Kres += dLN * this.BL;
            if (dGN > 0) Kres += dGN * this.BG;

            sc[0] = (dTL * this.clv + dTG * this.cmc) / Kres;
            sc[1] = 100 * 95 * dL / Kres;
            sc[2] = 1;
            sc[3] = (dTW * (this.cwl + this.cwu) / 2 + 100 * 24 * this.qw * dL / 50) / Kres;
                        
            return sc;
        }
        
        /// <summary>
        /// Имитация процесса функционирования ПЖДТ
        /// </summary>
        /// <returns>значение целевой функции, характеризующей устойчивое развитие ПЖДТ</returns>
        public double SimulateTS()
        {
            double[] sc = new double[4];
            while (sc[0] <= 0 || double.IsPositiveInfinity(sc[0])) sc = simplexCoeffs();

            ObjectiveFunction objF = new ObjectiveFunction(sc);
            Constraint c1 = new Constraint(new double[4] { 1, 1, 1, 1 }, 1);
            Constraint c2 = new Constraint(new double[4] { -1, -1, -1, -1 }, -1);
            Constraint c3 = new Constraint(new double[4] { -1, 0, 0, 0 }, -0.1);
            Constraint c4 = new Constraint(new double[4] { 0, -1, 0, 0 }, -0.1);
            Constraint c5 = new Constraint(new double[4] { 0, 0, -1, 0 }, -0.1);
            Constraint c6 = new Constraint(new double[4] { 0, 0, 0, -1 }, -0.1);
            LPP lpp = new LPP(objF, new Constraint[6] { c1, c2, c3, c4, c5, c6 });

            lpp.Solve();
            //MessageBox.Show(lpp.ObjFunc.Value(lpp.Variables).ToString(), "Response Function");

            return lpp.ObjFunc.Value(lpp.Variables);
        }

        /// <summary>
        /// Реализация эксперимента
        /// </summary>
        public void CalcExper()
        {
            double minI = 0.2, maxI = 1.2;
            double minQ = 3, maxQ = 10;
            int step = 3, nExp = 100;

            TextWriter tw = new StreamWriter("res.txt");

            for (double i = minI; i <= maxI; i += (maxI - minI) / step)
                for (double q = minQ; q <= maxQ; q += (maxQ - minQ) / step)
                    for (int j = 0; j < nExp; j++)
                    {
                        this.sI = new Stochastic(dl: 2, scl: i);
                        this.sQ = new Stochastic(dl: 1, loc: q, scl: q / 5);

                        double[] sc = new double[4];
                        while (sc[0] <= 0 || double.IsPositiveInfinity(sc[0])) sc = simplexCoeffs();

                        tw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", i, q, sc[0], sc[1], sc[3]);
                        //tw.WriteLine("{0}\t{1}\t{2}", i, q, SimulateTS());
                    }
            tw.Close();
        }
    }
}
