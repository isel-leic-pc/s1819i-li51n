using Aula_2018_11_21;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThreadUtils;

namespace PrimesChecker {
    public partial class Form1 : Form {
        private const int MAX_VALS = 20000000;
        private long[] vals;
        Random r;

       
        public Form1() {
            InitializeComponent();
            vals = new long[MAX_VALS];
            CheckForIllegalCrossThreadCalls = true;
            r = new Random();
        }

        private void InitVals(long[] vals, int size) {
            for (int i = 0; i < size; ++i)
                vals[i] = r.Next();
        }

        private void startBut_Click(object sender, EventArgs e) {
            int colSize = int.Parse(numSizeText.Text);
            countText.Clear();
          
            InitVals(vals, colSize);

            var task = Task.Run(() => {
                int size = PrimeUtils.CountPrimes(vals, colSize);
                return size;

            });
            task.ContinueWith((ant)=> {
                int res = ant.Result;
                this.countText.Text = res.ToString();
                startBut.Enabled = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());
            startBut.Enabled = false;
            

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            WorkerThreadReport.ShutdownWorkerThreadReport();
        }
    }
}
