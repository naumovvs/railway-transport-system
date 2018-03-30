using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

//using System.Data;
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
        public Stochastic sQ = new Stochastic(dl: 1, loc: 5, scl: 1);

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
        private double cwl = 100, cwu = 10;
        /// <summary>
        /// удельная стоимость работы ПРМ, у.е./ч 
        /// </summary>
        double cmc = 100;
        /// <summary>
        /// средневзвешенные удельные затраты на работу локомотивов
        /// при ожидании прибытия вагонов и перемещение подач вагонов соответственно, у.е./ч 
        /// </summary>
        double clc = 80, clv = 120;
        /// <summary>
        /// балансовая стоимость локомотивов и ПРМ, у.е.
        /// </summary>
        double BL = 1000, BG = 500;

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
        /// <param name="eq">отношение количества вагонов поезда к мощности локомотива</param>
        /// <param name="et">отношение интервала поступления поезда к продолжительности операции локомотива</param>
        /// <returns>Оптимальное количество станционных локомотивов</returns>
        private int defLocoNum(Node node)
        {
            double eq = this.EQ[node], et = this.ET[node];
            
            // стартовое количество ПРМ
            int gn = 5;
                 
            // коэффициенты уравнения для определения оптимального количества локомотивов
            double c1, c2, c3, c4, c5;
            c1 = 24 * clc;
            c2 = 60 * (clv - clc) * Math.Pow(eq, 0.884) * Math.Pow(et, -0.885);
            c3 = -932 * cwl * Math.Pow(eq, 2.527) * Math.Pow(et, -1.979);
            c4 = -9.5 * cwu * Math.Pow(eq, 0.978) * Math.Pow(et, -0.865);
            c5 = 14855 * cwl * Math.Pow(mt, 1.129) * Math.Pow(eq, 2.982) * Math.Pow(et, -2.698) * Math.Pow(gn, -2.368);

            Solver solver = new Solver();
            solver.LowBound = 1;
            solver.HighBound = 100;
            solver.Accuracy = 0.001;
            solver.FuncCoefs = new double[5] { c1, c2, c3, c4, c5 };
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

        private double defTotalTW(Node node)
        {
            return 2172 * Math.Pow(EQ[node], 2.527) * Math.Pow(LocoNum[node], -0.429) *
                    Math.Pow(ET[node], -1.979) + 104 * Math.Pow(EQ[node], 1.054) * Math.Pow(ET[node], -0.961) + 
                    48704 * Math.Pow(LocoNum[node], 0305) * Math.Pow(EQ[node], 2.982) * Math.Pow(this.mt, 1.129) *
                    Math.Pow(ET[node], -2.698) * Math.Pow(GearNum[node], -2.368) + 123 * this.mt;
        }

        private double defTotalTL(Node node)
        {
            double tl = 90 * Math.Pow(this.LocoNum[node], 0.664) * Math.Pow(this.ET[node], -0.885);
            return (tl < 24 * this.LocoNum[node]) ? 24 * this.LocoNum[node] : tl;
        }

        private double defTotalTG(Node node)
        {
            return (123 * this.mt < 24 * this.GearNum[node]) ? 24 * this.GearNum[node] : 123 * this.mt;
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
            int gn = (int)(12.36 * Math.Pow(ln, 0.091) * Math.Pow(mt, 0.335) * Math.Pow(eq, 0.885) *
                    Math.Pow(cwl / cmc, 0.297) / Math.Pow(et, 0.801));
            return (gn <= 1) ? 1 : gn;
        }

        public void SimulateNet()
        {
            this.SetFlows(this.sI);
            this.DefineLinksLoad();

            foreach (Node node in this.Nodes)
            {
                EQ[node] = this.sQ.getValue() / this.qw;
                ET[node] = 1 / node.InFlowsTotalIntencity / this.mw;
                LocoNum[node] = defLocoNum(node);
                GearNum[node] = defGearNum(EQ[node], ET[node], LocoNum[node]);
                TotalTW[node] = defTotalTW(node);
                TotalTL[node] = defTotalTL(node);
                TotalTG[node] = defTotalTG(node);
            }

            foreach (Link link in this.Links) this.LinksLoad[link] = link.Load;
        }

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

            double Kres = 0;
            if (dLN > 0) Kres += dLN * this.BL;
            if (dGN > 0) Kres += dGN * this.BG;

            sc[0] = (dTL * this.clv + dTG * this.cmc) / Kres;
            sc[1] = 100*95 * dL / Kres;
            sc[2] = 1;
            sc[3] = 100 * 24 * this.qw * dL / 50 / Kres; // (dTW * (this.cwl + this.cwu) / 2) + 24 * this.qw * dL / 50) / Kres;

            return sc;
        }

        public void SimulateTS()
        {
            double[] sc = new double[4];
            while (sc[0] <= 0 || double.IsPositiveInfinity(sc[0])) sc = simplexCoeffs();
            MessageBox.Show(sc[0].ToString() + " : " + sc[1].ToString() + " : " + sc[3].ToString());

            ObjectiveFunction objF = new ObjectiveFunction(sc);
            Constraint c1 = new Constraint(new double[4] { 1, 1, 1, 1 }, 1);
            Constraint c2 = new Constraint(new double[4] { -1, -1, -1, -1 }, -1);
            Constraint c3 = new Constraint(new double[4] { -1, 0, 0, 0 }, -0.1);
            Constraint c4 = new Constraint(new double[4] { 0, -1, 0, 0 }, -0.1);
            Constraint c5 = new Constraint(new double[4] { 0, 0, -1, 0 }, -0.1);
            Constraint c6 = new Constraint(new double[4] { 0, 0, 0, -1 }, -0.1);
            LPP lpp = new LPP(objF, new Constraint[6] { c1, c2, c3, c4, c5, c6 });

            lpp.Solve();

            //MessageBox.Show(lpp.Variables[0].ToString() + " : " + lpp.Variables[1].ToString() + " : " +
            //                lpp.Variables[2].ToString() + " : " + lpp.Variables[3].ToString());

        }

    }
}
