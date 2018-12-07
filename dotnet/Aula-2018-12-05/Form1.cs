
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncUtils;

namespace ShowImages {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = true;
        }

        private void ShowErrors(AggregateException e) {
            StringBuilder sb = new StringBuilder(e.Message);
            sb.Append(": ");
            e.Flatten().Handle((exc) => {
                sb.Append(exc.Message);
                sb.Append("; ");
                return true;
            });
            status.Text = sb.ToString();
        }
      
        /// <summary>
        /// Version of load images using WhenAll. Not a good idea,
        /// since we can show something only when we have all results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, EventArgs e) {
       
            var t1 = Model.DownloadImageFromUrlAsync(url1.Text);
            var t2 = Model.DownloadImageFromUrlAsync(url2.Text);
            var t3 = Model.DownloadImageFromUrlAsync(url3.Text);

            Task.WhenAll(t1,t2,t3).
                ContinueWith(t => {
                    if (t.Status == TaskStatus.Faulted)
                        ShowErrors(t.Exception);
                    else {
                        pictureBox1.Image = t.Result[0];
                        pictureBox2.Image = t.Result[1];
                        pictureBox2.Image = t.Result[2];
                    }
             
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// In this version we put a continuation in all tasks so we
        /// can process the results as soon as they are available.
        /// All continuations run in the user interface thraed, so  
        /// thre are no problems acessing the "index" variable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e) {
         
            string[] sites = {
                url1.Text, url2.Text, url3.Text
            };

            PictureBox[] viewers = { pictureBox1, pictureBox2, pictureBox3 };
            int index = 0;
            foreach (string site in sites) {
                Model.DownloadImageFromUrlAsync(site)
                    .ContinueWith(ant => {
                        if (ant.Status == TaskStatus.Faulted)
                            ShowErrors(ant.Exception);
                        else
                            viewers[index].Image = ant.Result;
                        index++;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

    }
}
