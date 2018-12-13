
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncUtils;
using System.Diagnostics;
using System.Threading;

namespace ShowImages {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = true;
        }

        /// <summary>
        /// An auxliary method to show the error(s) ocurred in the download tasks
        /// </summary>
        /// <param name="e"></param>
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

            Task.WhenAll(t1, t2, t3).
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
        /// there are no problems acessing the "index" variable.
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
                Model1.DownloadImageFromUrlAsync4(site)
                    .ContinueWith(ant => {
                        if (ant.Status == TaskStatus.Faulted)
                            ShowErrors(ant.Exception);
                        else
                            viewers[index].Image = ant.Result;
                        index++;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        /// <summary>
        /// A version of handler made async to avoid explicit continuations
        /// that use the launch downloads in sequence. Not good!
        /// Note that the ConfigureAwait call results in an error since the 
        /// continuation will not run on the UI thread!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button3_Click(object sender, EventArgs e) {
            string[] sites = {
                url1.Text, url2.Text, url3.Text
            };
            Debug.WriteLine("Start in UI thread {0}",
                Thread.CurrentThread.ManagedThreadId);
            PictureBox[] viewers = { pictureBox1, pictureBox2, pictureBox3 };
            int index = 0;
            foreach (String url in sites) {
                Image img = await Model.DownloadImageFromUrlAsync(url).
                    ConfigureAwait(false); 
                Debug.WriteLine("Continuation on thread {0}",
                    Thread.CurrentThread.ManagedThreadId);
                viewers[index].Image = img;
                index++;
                status.Text = "Done with Success";
            }
        }

        /// <summary>
        /// A version of handler made async to avoid explicit continuations
        /// that use the WhenAny combinator to process task results by completion order
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button4_Click(object sender, EventArgs e) {

            List<Task<Image>> tasks = new List<Task<Image>> {
                Model.DownloadImageFromUrlAsync(url1.Text),
                Model.DownloadImageFromUrlAsync(url2.Text),
                Model.DownloadImageFromUrlAsync(url3.Text)
            };
            PictureBox[] viewers = { pictureBox1, pictureBox2, pictureBox3 };
            int index = 0;

            while (tasks.Count > 0) {
                var task = await Task.WhenAny(tasks);
                viewers[index].Image = task.Result;
                index++;
                status.Text = "Done with Success";
                // note that we must remove the completed task from tasks collection!
                tasks.Remove(task);
            }
        }

        /// <summary>
        /// Finally a version of handler made async to avoid explicit continuations
        /// that use the OrderByCompletion combinator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button5_Click(object sender, EventArgs e) {

            List<Task<Image>> tasks = new List<Task<Image>> {
                Model.DownloadImageFromUrlAsync(url1.Text),
                Model.DownloadImageFromUrlAsync(url2.Text),
                Model.DownloadImageFromUrlAsync(url3.Text)
            };
            PictureBox[] viewers = { pictureBox1, pictureBox2, pictureBox3 };
            int index = 0;

            foreach(var t in Model.OrderByCompletion(tasks)) {
                await t;
                viewers[index].Image = t.Result;
                index++;
                status.Text = "Done with Success";
            }
        }
    }
}
